using Old8Lang.AST;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LangParser;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 符号表构建器 - 遍历 AST 构建带词法作用域链的符号表。
/// 顶层 <see cref="Build"/> 仅返回全局作用域符号，局部符号全部挂载在 <see cref="RootScope"/> 的子作用域树中。
/// </summary>
public class SymbolTableBuilder(string uri, List<LangToken>? tokens = null, string? sourceCode = null)
{
    private readonly List<LangToken>? _tokens = tokens;
    private readonly string? _sourceCode = sourceCode;

    // 当前正在处理的类的 token 范围（用于在类内查找字段文档注释）
    private int _currentClassStartTokenIndex = -1;
    private int _currentClassEndTokenIndex = -1;

    /// <summary>构建完成后的全局词法作用域根结点</summary>
    public SymbolScope? RootScope { get; private set; }

    /// <summary>顶层脚本作用域中的直接调用点列表（供 Call Hierarchy 使用）</summary>
    public List<CallSite> TopLevelCalls { get; } = [];

    /// <summary>
    /// 构建符号表与词法作用域树，返回顶层全局符号字典（保持向后兼容）。
    /// </summary>
    public Dictionary<string, SymbolInfo> Build(BlockStatement ast) => BuildWithScope(ast).Symbols;

    /// <summary>
    /// 构建并返回词法作用域树根结点（包含全局符号表与所有嵌套子作用域）。
    /// </summary>
    public SymbolScope BuildWithScope(BlockStatement ast)
    {
        var globalRange = new SourceLocation
        {
            Uri = uri,
            Line = 0,
            Column = 0,
            EndLine = int.MaxValue,
            EndColumn = int.MaxValue
        };

        RootScope = new SymbolScope
        {
            Kind = ScopeKind.Global,
            Parent = null,
            Range = globalRange
        };

        VisitBlockStatement(ast, RootScope);
        return RootScope;
    }

    /// <summary>
    /// 访问块语句（在指定作用域中处理声明与语句）
    /// </summary>
    private void VisitBlockStatement(BlockStatement block, SymbolScope scope)
    {
        // 先注册函数、类、导入等声明，确保同作用域内支持前向引用与类型解析
        foreach (var statement in block.ImportStatements)
        {
            VisitStatement(statement, scope);
        }

        foreach (var statement in block.OtherStatements)
        {
            VisitStatement(statement, scope);
        }
    }

    /// <summary>
    /// 访问单个语句
    /// </summary>
    private void VisitStatement(IOldLangTree statement, SymbolScope scope)
    {
        switch (statement)
        {
            case FuncInit funcInit:
                VisitFunction(funcInit, scope);
                break;
            case AsyncFuncInit asyncFuncInit:
                VisitAsyncFunction(asyncFuncInit, scope);
                break;
            case ClassInit classInit:
                VisitClass(classInit, scope);
                break;
            case EnumInit enumInit:
                VisitEnum(enumInit, scope);
                break;
            case SetStatement setStatement:
                VisitVariable(setStatement, scope);
                break;
            case NativeStatement nativeStatement:
                VisitNativeStatement(nativeStatement, scope);
                break;
            case ExternStatement externStatement:
                VisitExternStatement(externStatement, scope);
                break;
            case ImportStatement importStatement:
                VisitImportStatement(importStatement, scope);
                break;
            case IfStatement ifStatement:
                VisitIfStatement(ifStatement, scope);
                break;
            case WhileStatement whileStatement:
                VisitWhileStatement(whileStatement, scope);
                break;
            case ForStatement forStatement:
                VisitForStatement(forStatement, scope);
                break;
            case ForInStatement forInStatement:
                VisitForInStatement(forInStatement, scope);
                break;
            case AsyncForInStatement asyncForInStatement:
                VisitAsyncForInStatement(asyncForInStatement, scope);
                break;
            case TryStatement tryStatement:
                VisitTryStatement(tryStatement, scope);
                break;
            case UsingStatement usingStatement:
                VisitUsingStatement(usingStatement, scope);
                break;
            case SwitchStatement switchStatement:
                VisitSwitchStatement(switchStatement, scope);
                break;
            case SelectStatement selectStatement:
                VisitSelectStatement(selectStatement, scope);
                break;
            case DeferStatement deferStatement:
                VisitStatement(deferStatement.Statement, scope);
                break;
            case BlockStatement nestedBlock:
            {
                var childScope = CreateChildScope(ScopeKind.Block, scope, CreateBlockRange(nestedBlock, nestedBlock.Position));
                VisitBlockStatement(nestedBlock, childScope);
                break;
            }
            case ReturnStatement returnStatement:
                VisitExpressionForNestedScopes(returnStatement.Expression, scope);
                break;
            case FuncRunStatement funcRunStatement:
                VisitExpressionForNestedScopes(funcRunStatement.Expression, scope);
                break;
        }
    }

    /// <summary>
    /// 访问函数声明
    /// </summary>
    private void VisitFunction(FuncInit funcInit, SymbolScope parentScope)
    {
        var syntax = funcInit.Syntax;
        if (syntax.Id == null) return; // 跳过 Lambda

        var funcName = syntax.Id.IdName;
        var location = CreateLocation(syntax.Id.Position, funcName.Length, funcInit.Position);

        var paramList = syntax.Parameters.Count > 0
            ? string.Join(", ",
                syntax.Parameters.Select(p =>
                    $"{p.IdName}{(string.IsNullOrEmpty(p.AssumptionType) ? "" : ":" + p.AssumptionType)}"))
            : "";
        var returnType = string.IsNullOrEmpty(syntax.Id.AssumptionType) ? "void" : syntax.Id.AssumptionType;
        var funcSignature = $"func {funcName}({paramList}) -> {returnType}";

        var documentation = syntax.DocComment != null ? FormatDocComment(syntax.DocComment) : null;

        var funcSymbol = new SymbolInfo
        {
            Name = funcName,
            Kind = SymbolKind.Function,
            Type = funcSignature,
            Location = location,
            Documentation = documentation,
            HasExplicitTypeAnnotation = !string.IsNullOrEmpty(syntax.Id.AssumptionType),
            GenericParameters = syntax.GenericParameters?.Select(g => g.Name).ToList() ?? []
        };

        parentScope.Declare(funcSymbol);

        // 为函数体创建独立词法作用域（涵盖声明行到右花括号，保证参数和函数体均落入该作用域）
        var funcScope = CreateChildScope(
            ScopeKind.Function,
            parentScope,
            CreateDeclarationBlockRange(funcInit.Position, syntax.Body),
            ownerSymbol: funcSymbol);

        foreach (var param in syntax.Parameters)
        {
            var paramLocation = CreateLocation(param.Position, param.IdName.Length, funcInit.Position);
            var paramSymbol = new SymbolInfo
            {
                Name = param.IdName,
                Kind = SymbolKind.Parameter,
                Type = string.IsNullOrEmpty(param.AssumptionType) ? "var" : param.AssumptionType,
                Location = paramLocation,
                Parent = funcSymbol,
                HasExplicitTypeAnnotation = !string.IsNullOrEmpty(param.AssumptionType)
            };

            funcSymbol.Parameters.Add(paramSymbol);
            funcScope.Declare(paramSymbol);
        }

        VisitBlockStatement(syntax.Body, funcScope);
    }

    /// <summary>
    /// 访问异步函数声明
    /// </summary>
    private void VisitAsyncFunction(AsyncFuncInit asyncFuncInit, SymbolScope parentScope)
    {
        var syntax = asyncFuncInit.Syntax;
        if (syntax.Id == null) return;

        var funcName = syntax.Id.IdName;
        var location = CreateLocation(syntax.Id.Position, funcName.Length, asyncFuncInit.Position);

        var paramList = syntax.Parameters.Count > 0
            ? string.Join(", ",
                syntax.Parameters.Select(p =>
                    $"{p.IdName}{(string.IsNullOrEmpty(p.AssumptionType) ? "" : ":" + p.AssumptionType)}"))
            : "";
        var returnType = string.IsNullOrEmpty(syntax.Id.AssumptionType) ? "void" : syntax.Id.AssumptionType;
        var funcSignature = $"async func {funcName}({paramList}) -> {returnType}";

        var documentation = syntax.DocComment != null ? FormatDocComment(syntax.DocComment) : null;

        var asyncFuncSymbol = new SymbolInfo
        {
            Name = funcName,
            Kind = SymbolKind.Function,
            Type = funcSignature,
            Location = location,
            Documentation = documentation,
            HasExplicitTypeAnnotation = !string.IsNullOrEmpty(syntax.Id.AssumptionType),
            GenericParameters = syntax.GenericParameters?.Select(g => g.Name).ToList() ?? []
        };

        parentScope.Declare(asyncFuncSymbol);

        // 异步函数同样拥有独立的词法作用域，局部变量不污染顶层符号表
        var funcScope = CreateChildScope(
            ScopeKind.Function,
            parentScope,
            CreateDeclarationBlockRange(asyncFuncInit.Position, syntax.Body),
            ownerSymbol: asyncFuncSymbol);

        foreach (var param in syntax.Parameters)
        {
            var paramLocation = CreateLocation(param.Position, param.IdName.Length, asyncFuncInit.Position);
            var paramSymbol = new SymbolInfo
            {
                Name = param.IdName,
                Kind = SymbolKind.Parameter,
                Type = string.IsNullOrEmpty(param.AssumptionType) ? "var" : param.AssumptionType,
                Location = paramLocation,
                Parent = asyncFuncSymbol,
                HasExplicitTypeAnnotation = !string.IsNullOrEmpty(param.AssumptionType)
            };

            asyncFuncSymbol.Parameters.Add(paramSymbol);
            funcScope.Declare(paramSymbol);
        }

        VisitBlockStatement(syntax.Body, funcScope);
    }

    /// <summary>
    /// 访问类 / 接口 / Mixin 声明
    /// </summary>
    private void VisitClass(ClassInit classInit, SymbolScope parentScope)
    {
        var syntax = classInit.Syntax;
        var typeTemplate = classInit.GetTypeTemplate();
        var className = syntax.ClassName;
        var location = CreateLocation(syntax.NamePosition, className.Length, classInit.Position);

        FindClassTokenRange(className);

        var documentation = syntax.DocComment != null ? FormatDocComment(syntax.DocComment) : null;
        var prefix = syntax.IsInterface ? "interface" : syntax.IsMixin ? "mixin" : "class";

        var classSymbol = new SymbolInfo
        {
            Name = className,
            Kind = syntax.IsInterface ? SymbolKind.Interface : SymbolKind.Class,
            Type = $"{prefix} {className}",
            Location = location,
            Documentation = documentation,
            BaseClassName = syntax.ParentClassName,
            InterfaceNames = [.. syntax.ImplementsNames],
            MixinNames = [.. syntax.MixinNames],
            IsInterface = syntax.IsInterface,
            IsAbstract = syntax.IsAbstract,
            IsMixin = syntax.IsMixin,
            GenericParameters = syntax.GenericParameters?.Select(g => g.Name).ToList() ?? []
        };

        parentScope.Declare(classSymbol);

        var classScope = CreateChildScope(
            ScopeKind.Class,
            parentScope,
            CreateDeclarationBlockRange(classInit.Position, syntax.Body),
            ownerSymbol: classSymbol);

        VisitClassMembers(classSymbol, typeTemplate, classScope);

        _currentClassStartTokenIndex = -1;
        _currentClassEndTokenIndex = -1;
    }

    /// <summary>
    /// 访问类的成员（方法和属性）并为其方法建立独立方法作用域
    /// </summary>
    private void VisitClassMembers(SymbolInfo classSymbol, TypeTemplate typeTemplate, SymbolScope classScope)
    {
        // 第一遍：先注册所有字段和属性，以便方法体内部引用字段时即可获取准确类型
        foreach (var (memberId, memberExpr) in typeTemplate.Variates)
        {
            if (memberExpr is not FuncLangValue && memberExpr is not MethodOverloadList)
            {
                var prop = CreatePropertySymbol(memberId, memberExpr, isStatic: false, classScope);
                prop.Parent = classSymbol;
                classSymbol.Members[memberId.IdName] = prop;
                classScope.Declare(prop);
            }
        }

        foreach (var (memberId, memberExpr) in typeTemplate.StaticVariates)
        {
            if (memberExpr is not FuncLangValue && memberExpr is not MethodOverloadList)
            {
                var prop = CreatePropertySymbol(memberId, memberExpr, isStatic: true, classScope);
                prop.Parent = classSymbol;
                classSymbol.Members[memberId.IdName] = prop;
                classScope.Declare(prop);
            }
        }

        // 第二遍：注册实例方法与静态方法并遍历方法体作用域
        foreach (var (memberId, memberExpr) in typeTemplate.Variates)
        {
            if (memberExpr is FuncLangValue funcValue)
            {
                RegisterMethod(classSymbol, classScope, memberId, funcValue, isStatic: false);
            }
            else if (memberExpr is MethodOverloadList overloads && overloads.Overloads.Count > 0)
            {
                foreach (var overload in overloads.Overloads)
                {
                    RegisterMethod(classSymbol, classScope, memberId, overload, isStatic: false);
                }
            }
        }

        foreach (var (memberId, memberExpr) in typeTemplate.StaticVariates)
        {
            if (memberExpr is FuncLangValue funcValue)
            {
                RegisterMethod(classSymbol, classScope, memberId, funcValue, isStatic: true);
            }
            else if (memberExpr is MethodOverloadList overloads && overloads.Overloads.Count > 0)
            {
                foreach (var overload in overloads.Overloads)
                {
                    RegisterMethod(classSymbol, classScope, memberId, overload, isStatic: true);
                }
            }
        }
    }

    private void RegisterMethod(
        SymbolInfo classSymbol,
        SymbolScope classScope,
        ClassMemberId memberId,
        FuncLangValue funcValue,
        bool isStatic)
    {
        var methodSymbol = CreateMethodSymbol(memberId, funcValue, isStatic);
        methodSymbol.Parent = classSymbol;
        classSymbol.Members[memberId.IdName] = methodSymbol;
        classScope.Declare(methodSymbol);

        if (funcValue.BlockStatement == null)
        {
            return;
        }

        var methodScope = CreateChildScope(
            ScopeKind.Method,
            classScope,
            CreateDeclarationBlockRange(funcValue.Position, funcValue.BlockStatement),
            ownerSymbol: methodSymbol);

        foreach (var paramSymbol in methodSymbol.Parameters)
        {
            methodScope.Declare(paramSymbol);
        }

        VisitBlockStatement(funcValue.BlockStatement, methodScope);
    }

    /// <summary>
    /// 创建方法符号信息
    /// </summary>
    private SymbolInfo CreateMethodSymbol(ClassMemberId memberId, FuncLangValue funcValue, bool isStatic)
    {
        var methodName = memberId.IdName;
        var methodNamePos = funcValue.Id?.Position.Line > 0 ? funcValue.Id.Position : memberId.Position;
        var location = CreateLocation(methodNamePos, methodName.Length, funcValue.Position);

        var paramList = funcValue.Ids != null
            ? string.Join(", ",
                funcValue.Ids.Select(p =>
                    $"{p.IdName}{(string.IsNullOrEmpty(p.AssumptionType) ? "" : ":" + p.AssumptionType)}"))
            : "";
        var returnType = string.IsNullOrEmpty(funcValue.Id?.AssumptionType) ? "void" : funcValue.Id!.AssumptionType;
        var staticKeyword = isStatic ? "static " : "";
        var methodSignature = $"{staticKeyword}func {methodName}({paramList}) -> {returnType}";

        string? documentation = null;
        if (funcValue.DocComment != null)
        {
            documentation = FormatDocComment(funcValue.DocComment);
        }
        else
        {
            var docComment = FindDocCommentForMember(methodName);
            if (docComment != null)
            {
                documentation = FormatDocComment(docComment);
            }
        }

        var accessModifier = AccessModifier.Public;
        if (memberId.HasModifier(AccessModifierType.Private))
            accessModifier = AccessModifier.Private;
        else if (memberId.HasModifier(AccessModifierType.Protected))
            accessModifier = AccessModifier.Protected;

        var methodSymbol = new SymbolInfo
        {
            Name = methodName,
            Kind = SymbolKind.Method,
            Type = methodSignature,
            Location = location,
            Documentation = documentation,
            AccessModifier = accessModifier,
            IsStatic = isStatic,
            HasExplicitTypeAnnotation = !string.IsNullOrEmpty(funcValue.Id?.AssumptionType),
            GenericParameters = funcValue.GenericParameters?.Select(g => g.Name).ToList() ?? []
        };

        if (funcValue.Ids != null)
        {
            foreach (var param in funcValue.Ids)
            {
                var paramLocation = CreateLocation(param.Position, param.IdName.Length, funcValue.Position);

                var paramSymbol = new SymbolInfo
                {
                    Name = param.IdName,
                    Kind = SymbolKind.Parameter,
                    Type = string.IsNullOrEmpty(param.AssumptionType) ? "var" : param.AssumptionType,
                    Location = paramLocation,
                    Parent = methodSymbol,
                    HasExplicitTypeAnnotation = !string.IsNullOrEmpty(param.AssumptionType)
                };

                methodSymbol.Parameters.Add(paramSymbol);
            }
        }

        return methodSymbol;
    }

    /// <summary>
    /// 创建属性符号信息（支持无显式注解时从初始化表达式推断类型）
    /// </summary>
    private SymbolInfo CreatePropertySymbol(
        ClassMemberId memberId,
        LangExpression memberExpr,
        bool isStatic,
        SymbolScope classScope)
    {
        var propertyName = memberId.IdName;
        var location = CreateLocation(memberId.Position, propertyName.Length, memberExpr.Position);

        var hasExplicit = !string.IsNullOrEmpty(memberId.AssumptionType);
        var propertyType = hasExplicit ? memberId.AssumptionType : null;

        if (string.IsNullOrEmpty(propertyType))
        {
            var inferred = TypeResolver.InferExpressionType(memberExpr, classScope, RootScope?.Symbols);
            if (!string.IsNullOrEmpty(inferred) && inferred != "null")
            {
                propertyType = inferred;
            }
        }

        propertyType ??= "var";
        var staticKeyword = isStatic ? "static " : "";

        var accessModifier = AccessModifier.Public;
        if (memberId.HasModifier(AccessModifierType.Private))
            accessModifier = AccessModifier.Private;
        else if (memberId.HasModifier(AccessModifierType.Protected))
            accessModifier = AccessModifier.Protected;

        string? documentation = null;
        var docComment = FindDocCommentForMember(propertyName);
        if (docComment != null)
        {
            documentation = FormatDocComment(docComment);
        }

        return new SymbolInfo
        {
            Name = propertyName,
            Kind = SymbolKind.Property,
            Type = $"{staticKeyword}{propertyType}",
            Location = location,
            Documentation = documentation,
            AccessModifier = accessModifier,
            IsStatic = isStatic,
            HasExplicitTypeAnnotation = hasExplicit
        };
    }

    /// <summary>
    /// 访问枚举声明
    /// </summary>
    private void VisitEnum(EnumInit enumInit, SymbolScope scope)
    {
        var enumName = enumInit.EnumName;
        var location = CreateLocation(enumInit.NamePosition, enumName.Length, enumInit.Position);

        var enumSymbol = new SymbolInfo
        {
            Name = enumName,
            Kind = SymbolKind.Enum,
            Type = $"enum {enumName}",
            Location = location
        };

        foreach (var (memberName, _) in enumInit.Members)
        {
            var memberSymbol = new SymbolInfo
            {
                Name = memberName,
                Kind = SymbolKind.EnumMember,
                Type = enumName,
                Location = location,
                Parent = enumSymbol,
                IsStatic = true,
                AccessModifier = AccessModifier.Public
            };
            enumSymbol.Members[memberName] = memberSymbol;
        }

        scope.Declare(enumSymbol);
    }

    /// <summary>
    /// 访问变量赋值/声明语句
    /// </summary>
    private void VisitVariable(SetStatement setStatement, SymbolScope scope)
    {
        VisitExpressionForNestedScopes(setStatement.Value, scope);

        if (setStatement.Id == null || string.IsNullOrEmpty(setStatement.Id.IdName))
        {
            return;
        }

        var varName = setStatement.Id.IdName;
        var hasExplicitType = !string.IsNullOrEmpty(setStatement.Id.AssumptionType);

        // 若未带新类型注解且当前函数作用域链内已存在该变量/参数，视为重新赋值而非重新声明
        if (!hasExplicitType)
        {
            var existing = scope.ResolveInCurrentFunction(varName);
            if (existing != null)
            {
                // 如果原变量类型尚未定型（any/var），可借此次赋值补充推断类型
                if (TypeResolver.IsDynamicType(existing.Type) && setStatement.Value != null)
                {
                    var refined = TypeResolver.InferExpressionType(setStatement.Value, scope, RootScope?.Symbols);
                    if (!string.IsNullOrEmpty(refined) && refined != "null")
                    {
                        existing.Type = refined;
                    }
                }
                return;
            }
        }

        var location = CreateLocation(setStatement.Id.Position, varName.Length, setStatement.Position);

        var varType = hasExplicitType ? setStatement.Id.AssumptionType : null;
        if (string.IsNullOrEmpty(varType) && setStatement.Value != null)
        {
            varType = TypeResolver.InferExpressionType(setStatement.Value, scope, RootScope?.Symbols);
        }

        varType ??= "any";

        var varSymbol = new SymbolInfo
        {
            Name = varName,
            Kind = SymbolKind.Variable,
            Type = varType,
            Location = location,
            HasExplicitTypeAnnotation = hasExplicitType
        };

        // 显式类型注解在同一作用域重复声明时记入重复检查；无注解的顶层重赋上面已过滤
        scope.Declare(varSymbol, recordDuplicateCheck: hasExplicitType);
    }

    private void VisitIfStatement(IfStatement ifStatement, SymbolScope scope)
    {
        foreach (var child in ifStatement.Children)
        {
            VisitExpressionForNestedScopes(child.Condition, scope);
            if (child.Block is BlockStatement childBlock)
            {
                var blockScope = CreateChildScope(ScopeKind.Block, scope, CreateBlockRange(childBlock, child.Position));
                VisitBlockStatement(childBlock, blockScope);
            }
            else
            {
                VisitStatement(child.Block, scope);
            }
        }

        if (ifStatement.ElseBlock != null)
        {
            var elseScope = CreateChildScope(ScopeKind.Block, scope, CreateBlockRange(ifStatement.ElseBlock, ifStatement.Position));
            VisitBlockStatement(ifStatement.ElseBlock, elseScope);
        }
    }

    private void VisitWhileStatement(WhileStatement whileStatement, SymbolScope scope)
    {
        VisitExpressionForNestedScopes(whileStatement.Condition, scope);
        if (whileStatement.Block is BlockStatement whileBlock)
        {
            var blockScope = CreateChildScope(ScopeKind.Block, scope, CreateBlockRange(whileBlock, whileStatement.Position));
            VisitBlockStatement(whileBlock, blockScope);
        }
        else
        {
            VisitStatement(whileStatement.Block, scope);
        }
    }

    private void VisitForStatement(ForStatement forStatement, SymbolScope scope)
    {
        var loopScope = CreateChildScope(ScopeKind.Loop, scope, CreateDeclarationBlockRange(forStatement.Position, forStatement.Block));

        // C 风格 for 循环初始化变量始终在循环作用域内独立声明（遮蔽外层同名变量）
        if (forStatement.Init?.Id != null && !string.IsNullOrEmpty(forStatement.Init.Id.IdName))
        {
            var id = forStatement.Init.Id;
            var hasExplicit = !string.IsNullOrEmpty(id.AssumptionType);
            var varType = hasExplicit
                ? id.AssumptionType
                : TypeResolver.InferExpressionType(forStatement.Init.Value, scope, RootScope?.Symbols) ?? "any";

            loopScope.Declare(new SymbolInfo
            {
                Name = id.IdName,
                Kind = SymbolKind.Variable,
                Type = varType,
                Location = CreateLocation(id.Position, id.IdName.Length, forStatement.Init.Position),
                HasExplicitTypeAnnotation = hasExplicit
            });
        }

        VisitExpressionForNestedScopes(forStatement.Condition, loopScope);
        VisitBlockStatement(forStatement.Block, loopScope);
    }

    private void VisitForInStatement(ForInStatement forInStatement, SymbolScope scope)
    {
        VisitExpressionForNestedScopes(forInStatement.Expression, scope);

        var bodyBlock = forInStatement.Body as BlockStatement;
        var range = bodyBlock != null
            ? CreateDeclarationBlockRange(forInStatement.Position, bodyBlock)
            : CreateLocation(forInStatement.Position, 3);
        var loopScope = CreateChildScope(ScopeKind.Loop, scope, range);

        foreach (var id in forInStatement.Ids)
        {
            if (string.IsNullOrEmpty(id.IdName)) continue;
            loopScope.Declare(new SymbolInfo
            {
                Name = id.IdName,
                Kind = SymbolKind.Variable,
                Type = string.IsNullOrEmpty(id.AssumptionType) ? "any" : id.AssumptionType,
                Location = CreateLocation(id.Position, id.IdName.Length, forInStatement.Position),
                HasExplicitTypeAnnotation = !string.IsNullOrEmpty(id.AssumptionType)
            });
        }

        if (bodyBlock != null)
        {
            VisitBlockStatement(bodyBlock, loopScope);
        }
        else
        {
            VisitStatement(forInStatement.Body, loopScope);
        }
    }

    private void VisitAsyncForInStatement(AsyncForInStatement asyncForInStatement, SymbolScope scope)
    {
        VisitExpressionForNestedScopes(asyncForInStatement.Expression, scope);

        var bodyBlock = asyncForInStatement.Body as BlockStatement;
        var range = bodyBlock != null
            ? CreateDeclarationBlockRange(asyncForInStatement.Position, bodyBlock)
            : CreateLocation(asyncForInStatement.Position, 5);
        var loopScope = CreateChildScope(ScopeKind.Loop, scope, range);

        foreach (var id in asyncForInStatement.Ids)
        {
            if (string.IsNullOrEmpty(id.IdName)) continue;
            loopScope.Declare(new SymbolInfo
            {
                Name = id.IdName,
                Kind = SymbolKind.Variable,
                Type = string.IsNullOrEmpty(id.AssumptionType) ? "any" : id.AssumptionType,
                Location = CreateLocation(id.Position, id.IdName.Length, asyncForInStatement.Position),
                HasExplicitTypeAnnotation = !string.IsNullOrEmpty(id.AssumptionType)
            });
        }

        if (bodyBlock != null)
        {
            VisitBlockStatement(bodyBlock, loopScope);
        }
        else
        {
            VisitStatement(asyncForInStatement.Body, loopScope);
        }
    }

    private void VisitTryStatement(TryStatement tryStatement, SymbolScope scope)
    {
        var tryScope = CreateChildScope(ScopeKind.Block, scope, CreateBlockRange(tryStatement.TryBlock, tryStatement.Position));
        VisitBlockStatement(tryStatement.TryBlock, tryScope);

        foreach (var (exceptionType, exceptionVar, filter, catchBlock) in tryStatement.CatchBlocks)
        {
            var startPos = exceptionVar is { Position.Line: > 0 } ? exceptionVar.Position : catchBlock.Position;
            var catchScope = CreateChildScope(ScopeKind.Catch, scope, CreateDeclarationBlockRange(startPos, catchBlock));

            if (exceptionVar != null && !string.IsNullOrEmpty(exceptionVar.IdName))
            {
                catchScope.Declare(new SymbolInfo
                {
                    Name = exceptionVar.IdName,
                    Kind = SymbolKind.Variable,
                    Type = string.IsNullOrEmpty(exceptionType) ? "Exception" : exceptionType,
                    Location = CreateLocation(exceptionVar.Position, exceptionVar.IdName.Length, catchBlock.Position),
                    HasExplicitTypeAnnotation = !string.IsNullOrEmpty(exceptionType)
                });
            }

            VisitExpressionForNestedScopes(filter, catchScope);
            VisitBlockStatement(catchBlock, catchScope);
        }

        if (tryStatement.FinallyBlock != null)
        {
            var finallyScope = CreateChildScope(ScopeKind.Block, scope, CreateBlockRange(tryStatement.FinallyBlock, tryStatement.Position));
            VisitBlockStatement(tryStatement.FinallyBlock, finallyScope);
        }
    }

    private void VisitUsingStatement(UsingStatement usingStatement, SymbolScope scope)
    {
        VisitExpressionForNestedScopes(usingStatement.ResourceExpression, scope);
        var usingScope = CreateChildScope(
            ScopeKind.Using,
            scope,
            CreateDeclarationBlockRange(usingStatement.Position, usingStatement.BlockStatement));

        if (!string.IsNullOrEmpty(usingStatement.VariableName))
        {
            var resourceType = TypeResolver.InferExpressionType(usingStatement.ResourceExpression, scope, RootScope?.Symbols) ?? "any";
            usingScope.Declare(new SymbolInfo
            {
                Name = usingStatement.VariableName,
                Kind = SymbolKind.Variable,
                Type = resourceType,
                Location = CreateLocation(usingStatement.VariablePosition, usingStatement.VariableName.Length, usingStatement.Position)
            });
        }

        VisitBlockStatement(usingStatement.BlockStatement, usingScope);
    }

    private void VisitSwitchStatement(SwitchStatement switchStatement, SymbolScope scope)
    {
        VisitExpressionForNestedScopes(switchStatement.SwitchExpression, scope);

        foreach (var caseStmt in switchStatement.SwitchCaseList)
        {
            VisitExpressionForNestedScopes(caseStmt.Expression, scope);
            var caseScope = CreateChildScope(ScopeKind.Block, scope, CreateBlockRange(caseStmt.BlockStatement, caseStmt.Position));
            VisitBlockStatement(caseStmt.BlockStatement, caseScope);
        }

        if (switchStatement.DefaultBlockStatement != null)
        {
            var defaultScope = CreateChildScope(ScopeKind.Block, scope, CreateBlockRange(switchStatement.DefaultBlockStatement, switchStatement.Position));
            VisitBlockStatement(switchStatement.DefaultBlockStatement, defaultScope);
        }
    }

    private void VisitSelectStatement(SelectStatement selectStatement, SymbolScope scope)
    {
        foreach (var selectCase in selectStatement.Cases)
        {
            VisitExpressionForNestedScopes(selectCase.ChannelExpression, scope);
            VisitExpressionForNestedScopes(selectCase.SendValueExpression, scope);

            var caseScope = CreateChildScope(ScopeKind.Block, scope, CreateDeclarationBlockRange(selectCase.Position, selectCase.BlockStatement));
            if (!string.IsNullOrEmpty(selectCase.VariableName))
            {
                caseScope.Declare(new SymbolInfo
                {
                    Name = selectCase.VariableName,
                    Kind = SymbolKind.Variable,
                    Type = "any",
                    Location = CreateLocation(selectCase.Position, selectCase.VariableName.Length)
                });
            }

            VisitBlockStatement(selectCase.BlockStatement, caseScope);
        }

        if (selectStatement.DefaultCase != null)
        {
            var defaultScope = CreateChildScope(ScopeKind.Block, scope, CreateBlockRange(selectStatement.DefaultCase, selectStatement.Position));
            VisitBlockStatement(selectStatement.DefaultCase, defaultScope);
        }
    }

    /// <summary>
    /// 在表达式中递归发现 Lambda 表达式并建立对应的词法作用域。
    /// </summary>
    private void VisitExpressionForNestedScopes(LangExpression? expr, SymbolScope scope)
    {
        if (expr == null) return;

        switch (expr)
        {
            case LambdaExpression lambda:
            {
                var lambdaScope = CreateChildScope(
                    ScopeKind.Lambda,
                    scope,
                    CreateDeclarationBlockRange(lambda.Position, lambda.Body));

                foreach (var param in lambda.Parameters)
                {
                    lambdaScope.Declare(new SymbolInfo
                    {
                        Name = param.IdName,
                        Kind = SymbolKind.Parameter,
                        Type = string.IsNullOrEmpty(param.AssumptionType) ? "var" : param.AssumptionType,
                        Location = CreateLocation(param.Position, param.IdName.Length, lambda.Position),
                        HasExplicitTypeAnnotation = !string.IsNullOrEmpty(param.AssumptionType)
                    });
                }

                VisitBlockStatement(lambda.Body, lambdaScope);
                break;
            }
            case Operation op:
                if (op.Opera == LangTokenType.Dot)
                {
                    VisitExpressionForNestedScopes(op.Left, scope);

                    var receiverName = ExtractReceiverName(op.Left);
                    if (op.Right is CallExpression memberCall)
                    {
                        RecordCallSite(scope, memberCall.Id.IdName, memberCall.Id.Position, memberCall.Position, receiverName, isMemberCall: true);
                        foreach (var arg in memberCall.Arguments) VisitExpressionForNestedScopes(arg, scope);
                        foreach (var named in memberCall.NamedArguments) VisitExpressionForNestedScopes(named.Value, scope);
                    }
                    else if (op.Right is Instance memberInst)
                    {
                        RecordCallSite(scope, memberInst.Id.IdName, memberInst.Id.Position, memberInst.Position, receiverName, isMemberCall: true);
                        foreach (var arg in memberInst.Ids) VisitExpressionForNestedScopes(arg, scope);
                        foreach (var named in memberInst.NamedArgs) VisitExpressionForNestedScopes(named.Value, scope);
                    }
                    else
                    {
                        VisitExpressionForNestedScopes(op.Right, scope);
                    }
                }
                else
                {
                    VisitExpressionForNestedScopes(op.Left, scope);
                    VisitExpressionForNestedScopes(op.Right, scope);
                }
                break;
            case CallExpression call:
                RecordCallSite(scope, call.Id.IdName, call.Id.Position, call.Position, receiverName: null, isMemberCall: false);
                foreach (var arg in call.Arguments) VisitExpressionForNestedScopes(arg, scope);
                foreach (var named in call.NamedArguments) VisitExpressionForNestedScopes(named.Value, scope);
                break;
            case Instance inst:
                RecordCallSite(scope, inst.Id.IdName, inst.Id.Position, inst.Position, receiverName: null, isMemberCall: false);
                foreach (var arg in inst.Ids) VisitExpressionForNestedScopes(arg, scope);
                foreach (var named in inst.NamedArgs) VisitExpressionForNestedScopes(named.Value, scope);
                break;
            case FunctionCallExpression funcCall:
                if (funcCall.FunctionExpression is LangId fnId)
                {
                    RecordCallSite(scope, fnId.IdName, fnId.Position, funcCall.Position, receiverName: null, isMemberCall: false);
                }
                else
                {
                    VisitExpressionForNestedScopes(funcCall.FunctionExpression, scope);
                }
                foreach (var arg in funcCall.Arguments) VisitExpressionForNestedScopes(arg, scope);
                break;
            case TernaryExpression ternary:
                VisitExpressionForNestedScopes(ternary.Condition, scope);
                VisitExpressionForNestedScopes(ternary.TrueExpression, scope);
                VisitExpressionForNestedScopes(ternary.FalseExpression, scope);
                break;
            case AwaitExpression awaitExpr:
                VisitExpressionForNestedScopes(awaitExpr.Expression, scope);
                break;
            case ListLiteralExpression listLit:
                foreach (var el in listLit.Elements) VisitExpressionForNestedScopes(el, scope);
                break;
            case ArrayLiteralExpression arrLit:
                foreach (var el in arrLit.Elements) VisitExpressionForNestedScopes(el, scope);
                break;
            case TupleLiteralExpression tupleLit:
                foreach (var el in tupleLit.Elements) VisitExpressionForNestedScopes(el, scope);
                break;
        }
    }

    private void RecordCallSite(
        SymbolScope scope,
        string symbolName,
        SourcePosition idPosition,
        SourcePosition fallbackPosition,
        string? receiverName,
        bool isMemberCall)
    {
        if (string.IsNullOrEmpty(symbolName))
        {
            return;
        }

        var callSite = new CallSite
        {
            SymbolName = symbolName,
            Location = CreateLocation(idPosition, symbolName.Length, fallbackPosition),
            ReceiverName = receiverName,
            IsMemberCall = isMemberCall
        };

        var owner = FindEnclosingCallableOwner(scope);
        if (owner != null)
        {
            owner.Calls.Add(callSite);
        }
        else
        {
            TopLevelCalls.Add(callSite);
        }
    }

    private static SymbolInfo? FindEnclosingCallableOwner(SymbolScope scope)
    {
        var current = scope;
        while (current != null)
        {
            if (current.Kind is ScopeKind.Function or ScopeKind.Method && current.OwnerSymbol != null)
            {
                return current.OwnerSymbol;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string? ExtractReceiverName(LangExpression? receiver)
    {
        return receiver switch
        {
            LangId id => id.IdName,
            ThisExpression => "this",
            SuperExpression => "super",
            _ => null
        };
    }

    /// <summary>
    /// 格式化文档注释为 Markdown
    /// </summary>
    private static string FormatDocComment(DocCommentInfo docComment)
    {
        var lines = new List<string>();

        if (!string.IsNullOrEmpty(docComment.Summary))
        {
            lines.Add(docComment.Summary);
            lines.Add("");
        }

        if (docComment.Parameters.Count > 0)
        {
            lines.Add("**参数:**");
            foreach (var param in docComment.Parameters)
            {
                var paramLine = $"- `{param.Name}`";
                if (!string.IsNullOrEmpty(param.Type))
                {
                    paramLine += $" *({param.Type})*";
                }

                if (!string.IsNullOrEmpty(param.Description))
                {
                    paramLine += $": {param.Description}";
                }

                lines.Add(paramLine);
            }

            lines.Add("");
        }

        if (docComment.Returns != null)
        {
            var returnLine = "**返回:**";
            if (!string.IsNullOrEmpty(docComment.Returns.Type))
            {
                returnLine += $" *{docComment.Returns.Type}*";
            }

            if (!string.IsNullOrEmpty(docComment.Returns.Description))
            {
                returnLine += $" - {docComment.Returns.Description}";
            }

            lines.Add(returnLine);
            lines.Add("");
        }

        if (docComment.Throws.Count > 0)
        {
            lines.Add("**异常:**");
            foreach (var throwInfo in docComment.Throws)
            {
                var throwLine = $"- `{throwInfo.Type}`";
                if (!string.IsNullOrEmpty(throwInfo.Description))
                {
                    throwLine += $": {throwInfo.Description}";
                }

                lines.Add(throwLine);
            }

            lines.Add("");
        }

        if (docComment.Examples.Count > 0)
        {
            lines.Add("**示例:**");
            foreach (var example in docComment.Examples)
            {
                lines.Add("```old8lang");
                lines.Add(example);
                lines.Add("```");
            }
        }

        return string.Join("\n", lines);
    }

    private SymbolScope CreateChildScope(
        ScopeKind kind,
        SymbolScope parent,
        SourceLocation range,
        SymbolInfo? ownerSymbol = null)
    {
        var child = new SymbolScope
        {
            Kind = kind,
            Parent = parent,
            Range = range,
            OwnerSymbol = ownerSymbol
        };
        parent.Children.Add(child);
        return child;
    }

    private SourceLocation CreateBlockRange(BlockStatement block, SourcePosition fallbackStart)
    {
        var start = block.Position.Line > 0 ? block.Position : fallbackStart;
        var end = block.EndPosition.Line > 0 ? block.EndPosition : ComputeFallbackEndPosition(block, start);

        var startLine = Math.Max(0, start.Line - 1);
        var startCol = Math.Max(0, start.Column);
        var endLine = Math.Max(startLine, end.Line - 1);
        var endCol = end.Line > 0 ? Math.Max(0, end.Column) + 1 : int.MaxValue;

        if (endLine == startLine && endCol < startCol)
        {
            endCol = int.MaxValue;
        }

        return new SourceLocation
        {
            Uri = uri,
            Line = startLine,
            Column = startCol,
            EndLine = endLine,
            EndColumn = endCol
        };
    }

    private SourceLocation CreateDeclarationBlockRange(SourcePosition declStart, BlockStatement block)
    {
        var start = declStart.Line > 0 ? declStart : block.Position;
        var end = block.EndPosition.Line > 0 ? block.EndPosition : ComputeFallbackEndPosition(block, start);

        var startLine = Math.Max(0, start.Line - 1);
        var startCol = Math.Max(0, start.Column);
        var endLine = Math.Max(startLine, end.Line - 1);
        var endCol = end.Line > 0 ? Math.Max(0, end.Column) + 1 : int.MaxValue;

        if (endLine == startLine && endCol < startCol)
        {
            endCol = int.MaxValue;
        }

        return new SourceLocation
        {
            Uri = uri,
            Line = startLine,
            Column = startCol,
            EndLine = endLine,
            EndColumn = endCol
        };
    }

    private static SourcePosition ComputeFallbackEndPosition(BlockStatement block, SourcePosition fallbackStart)
    {
        var maxPos = fallbackStart;
        foreach (var stmt in block.ImportStatements.Concat(block.OtherStatements))
        {
            if (stmt.Position.Line > maxPos.Line ||
                (stmt.Position.Line == maxPos.Line && stmt.Position.Column > maxPos.Column))
            {
                maxPos = stmt.Position;
            }
        }

        return maxPos;
    }

    /// <summary>
    /// 根据 AST 的 SourcePosition 构建 0-based LSP SourceLocation。
    /// </summary>
    private SourceLocation CreateLocation(SourcePosition primary, int length, SourcePosition fallback = default)
    {
        var pos = primary.Line > 0 ? primary : fallback;
        var line = Math.Max(0, pos.Line - 1);
        var column = Math.Max(0, pos.Column);

        return new SourceLocation
        {
            Uri = uri,
            Line = line,
            Column = column,
            EndLine = line,
            EndColumn = column + Math.Max(0, length)
        };
    }

    /// <summary>
    /// 从 tokens 中查找成员的文档注释（类字段 AST 暂未挂 DocCommentInfo）
    /// </summary>
    private DocCommentInfo? FindDocCommentForMember(string memberName)
    {
        if (_tokens == null) return null;

        int startIndex = _currentClassStartTokenIndex >= 0 ? _currentClassStartTokenIndex : 0;
        int endIndex = _currentClassEndTokenIndex >= 0 ? _currentClassEndTokenIndex : _tokens.Count;

        for (int i = startIndex; i < endIndex; i++)
        {
            var token = _tokens[i];
            if (token.Type == LangTokenType.Identifier && token.Value == memberName)
            {
                bool isMemberDefinition = false;
                if (i + 1 < endIndex)
                {
                    var nextToken = _tokens[i + 1];
                    if (nextToken.Type is LangTokenType.Colon or LangTokenType.Assignment or LangTokenType.LeftParen)
                    {
                        isMemberDefinition = true;
                    }
                }

                if (!isMemberDefinition) continue;

                var docCommentTokens = new List<LangToken>();
                var searchIndex = i - 1;
                while (searchIndex >= startIndex)
                {
                    var prevToken = _tokens[searchIndex];
                    if (prevToken.Type is LangTokenType.Public or LangTokenType.Private or LangTokenType.Protected
                        or LangTokenType.Static or LangTokenType.Func)
                    {
                        searchIndex--;
                        continue;
                    }

                    if (prevToken.Type == LangTokenType.DocComment)
                    {
                        docCommentTokens.Insert(0, prevToken);
                        searchIndex--;
                        continue;
                    }

                    break;
                }

                if (docCommentTokens.Count > 0)
                {
                    var rawComment = string.Join("\n", docCommentTokens.Select(t => t.Value));
                    return DocCommentParser.Parse(rawComment);
                }

                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// 查找类的 token 范围（仅供成员文档注释收集使用）
    /// </summary>
    private void FindClassTokenRange(string className)
    {
        if (_tokens == null)
        {
            _currentClassStartTokenIndex = -1;
            _currentClassEndTokenIndex = -1;
            return;
        }

        for (int i = 0; i < _tokens.Count - 1; i++)
        {
            if (_tokens[i].Type is LangTokenType.Class or LangTokenType.Interface or LangTokenType.Mixin &&
                _tokens[i + 1].Type == LangTokenType.Identifier &&
                _tokens[i + 1].Value == className)
            {
                _currentClassStartTokenIndex = i;

                int braceCount = 0;
                bool foundLeftBrace = false;
                for (int j = i + 2; j < _tokens.Count; j++)
                {
                    if (_tokens[j].Type == LangTokenType.LeftBrace)
                    {
                        braceCount++;
                        foundLeftBrace = true;
                    }
                    else if (_tokens[j].Type == LangTokenType.RightBrace)
                    {
                        braceCount--;
                        if (foundLeftBrace && braceCount == 0)
                        {
                            _currentClassEndTokenIndex = j + 1;
                            return;
                        }
                    }
                }

                _currentClassEndTokenIndex = _tokens.Count;
                return;
            }
        }

        _currentClassStartTokenIndex = -1;
        _currentClassEndTokenIndex = -1;
    }

    /// <summary>
    /// 访问 native 函数声明
    /// </summary>
    private void VisitNativeStatement(NativeStatement nativeStatement, SymbolScope scope)
    {
        var methodName = nativeStatement.MethodName;
        var nativeName = nativeStatement.NativeName;
        var funcSyntax = nativeStatement.Function;
        var className = nativeStatement.ClassName;

        if (!string.IsNullOrEmpty(methodName))
        {
            var functionName = !string.IsNullOrEmpty(nativeName) ? nativeName : methodName;

            string funcSignature;
            if (funcSyntax?.Id != null)
            {
                var paramList = funcSyntax.Parameters.Count > 0
                    ? string.Join(", ",
                        funcSyntax.Parameters.Select(p =>
                            $"{p.IdName}{(string.IsNullOrEmpty(p.AssumptionType) ? "" : ":" + p.AssumptionType)}"))
                    : "";
                var returnType = string.IsNullOrEmpty(funcSyntax.Id.AssumptionType) ? "void" : funcSyntax.Id.AssumptionType;
                funcSignature = $"native func {functionName}({paramList}) -> {returnType}";
            }
            else
            {
                funcSignature = $"native func {functionName}(...)";
            }

            var location = CreateLocation(nativeStatement.NamePosition, functionName.Length, nativeStatement.Position);

            scope.Declare(new SymbolInfo
            {
                Name = functionName,
                Kind = SymbolKind.Function,
                Type = funcSignature,
                Location = location,
                Documentation = $"Native function from {className}"
            });
        }
        else if (!string.IsNullOrEmpty(nativeStatement.ClassAlias) || !string.IsNullOrEmpty(nativeStatement.ClassName))
        {
            var name = !string.IsNullOrEmpty(nativeStatement.ClassAlias)
                ? nativeStatement.ClassAlias
                : nativeStatement.ClassName;
            var location = CreateLocation(nativeStatement.NamePosition, name.Length, nativeStatement.Position);
            scope.Declare(new SymbolInfo
            {
                Name = name,
                Kind = SymbolKind.Class,
                Type = $"native class {nativeStatement.ClassName}",
                Location = location,
                Documentation = $"Native class from {nativeStatement.DllName}"
            });
        }
    }

    /// <summary>
    /// 访问 extern 语句
    /// </summary>
    private void VisitExternStatement(ExternStatement externStatement, SymbolScope scope)
    {
        foreach (var func in externStatement.Functions)
        {
            var functionName = func.Alias ?? func.FunctionName;

            string funcSignature;
            var paramSymbols = new List<SymbolInfo>();
            if (func.FunctionSignature != null)
            {
                var funcSyntax = func.FunctionSignature.Syntax;
                var paramList = funcSyntax.Parameters.Count > 0
                    ? string.Join(", ",
                        funcSyntax.Parameters.Select(p =>
                            $"{p.IdName}{(string.IsNullOrEmpty(p.AssumptionType) ? "" : ":" + p.AssumptionType)}"))
                    : "";
                var returnType = string.IsNullOrEmpty(funcSyntax.Id?.AssumptionType) ? "void" : funcSyntax.Id!.AssumptionType;
                funcSignature = $"extern func {functionName}({paramList}) -> {returnType}";

                foreach (var p in funcSyntax.Parameters)
                {
                    paramSymbols.Add(new SymbolInfo
                    {
                        Name = p.IdName,
                        Kind = SymbolKind.Parameter,
                        Type = string.IsNullOrEmpty(p.AssumptionType) ? "var" : p.AssumptionType,
                        Location = CreateLocation(p.Position, p.IdName.Length, externStatement.Position),
                        HasExplicitTypeAnnotation = !string.IsNullOrEmpty(p.AssumptionType)
                    });
                }
            }
            else
            {
                funcSignature = $"extern func {functionName}(...)";
            }

            var location = CreateLocation(func.NamePosition, functionName.Length, externStatement.Position);

            var symbol = new SymbolInfo
            {
                Name = functionName,
                Kind = SymbolKind.Function,
                Type = funcSignature,
                Location = location,
                Documentation = "External function",
                Parameters = paramSymbols
            };
            foreach (var p in paramSymbols) p.Parent = symbol;

            scope.Declare(symbol);
        }
    }

    /// <summary>
    /// 访问 import 语句
    /// </summary>
    private void VisitImportStatement(ImportStatement importStatement, SymbolScope scope)
    {
        var moduleAlias = importStatement.GetModuleAlias();
        var importString = importStatement.GetImportString();

        if (!string.IsNullOrEmpty(moduleAlias))
        {
            var location = CreateLocation(importStatement.ModuleAliasPosition, moduleAlias.Length, importStatement.Position);

            scope.Declare(new SymbolInfo
            {
                Name = moduleAlias,
                Kind = SymbolKind.Variable,
                Type = "module",
                Location = location,
                Documentation = $"Imported module: {importString}"
            });
        }
    }
}
