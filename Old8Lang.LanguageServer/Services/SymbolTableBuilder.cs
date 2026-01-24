using Old8Lang.AST;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LangParser;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 符号表构建器 - 遍历AST构建符号表
/// </summary>
public class SymbolTableBuilder(string uri, List<LangToken>? tokens = null, string? sourceCode = null)
{
    private readonly Dictionary<string, SymbolInfo> _symbolTable = new();
    private readonly List<LangToken>? _tokens = tokens;
    private readonly string? _sourceCode = sourceCode;

    // 当前正在处理的类的 token 范围（用于在类内查找成员文档注释）
    private int _currentClassStartTokenIndex = -1;
    private int _currentClassEndTokenIndex = -1;

    /// <summary>
    /// 构建符号表
    /// </summary>
    public Dictionary<string, SymbolInfo> Build(BlockStatement ast)
    {
        _symbolTable.Clear();
        VisitBlockStatement(ast);
        return _symbolTable;
    }

    /// <summary>
    /// 访问块语句
    /// </summary>
    private void VisitBlockStatement(BlockStatement block)
    {
        // 访问 import 语句（包括函数、类声明）
        foreach (var statement in block.ImportStatements)
        {
            VisitStatement(statement);
        }

        // 访问其他语句（包括变量声明）
        foreach (var statement in block.OtherStatements)
        {
            VisitStatement(statement);
        }
    }

    /// <summary>
    /// 访问单个语句
    /// </summary>
    private void VisitStatement(IOldLangTree statement)
    {
        switch (statement)
        {
            case FuncInit funcInit:
                VisitFunction(funcInit);
                break;
            case AsyncFuncInit asyncFuncInit:
                VisitAsyncFunction(asyncFuncInit);
                break;
            case ClassInit classInit:
                VisitClass(classInit);
                break;
            case SetStatement setStatement:
                VisitVariable(setStatement);
                break;
            case NativeStatement nativeStatement:
                VisitNativeStatement(nativeStatement);
                break;
            case ExternStatement externStatement:
                VisitExternStatement(externStatement);
                break;
            case ImportStatement importStatement:
                VisitImportStatement(importStatement);
                break;
        }
    }

    /// <summary>
    /// 访问函数声明
    /// </summary>
    private void VisitFunction(FuncInit funcInit)
    {
        var funcValue = funcInit.FuncValue;
        if (funcValue.Id == null) return; // 跳过Lambda

        var funcName = funcValue.Id.IdName;

        // 尝试从 token 列表中查找位置
        var tokenLocation = FindSymbolLocationFromTokens(funcName, LangTokenType.Func);
        var location = tokenLocation ?? new SourceLocation
        {
            Uri = uri,
            Line = funcInit.Position.Line,
            Column = funcInit.Position.Column,
            EndLine = funcInit.Position.Line,
            EndColumn = funcInit.Position.Column + funcName.Length
        };

        // 构建函数签名
        var paramList = funcValue.Ids != null
            ? string.Join(", ",
                funcValue.Ids.Select(p =>
                    $"{p.IdName}{(string.IsNullOrEmpty(p.AssumptionType) ? "" : ":" + p.AssumptionType)}"))
            : "";
        var returnType = funcValue.Id.AssumptionType ?? "void";
        var funcSignature = $"func {funcName}({paramList}) -> {returnType}";

        // 提取文档注释
        string? documentation = null;
        if (funcValue.DocComment != null)
        {
            documentation = FormatDocComment(funcValue.DocComment);
        }

        var funcSymbol = new SymbolInfo
        {
            Name = funcName,
            Kind = SymbolKind.Function,
            Type = funcSignature,
            Location = location,
            Documentation = documentation
        };

        // 添加函数参数到符号的参数列表
        if (funcValue.Ids != null)
        {
            foreach (var param in funcValue.Ids)
            {
                var paramLocation = FindSymbolLocationFromTokens(param.IdName, LangTokenType.Identifier);
                if (paramLocation == null)
                {
                    paramLocation = new SourceLocation
                    {
                        Uri = uri,
                        Line = param.Position.Line,
                        Column = param.Position.Column,
                        EndLine = param.Position.Line,
                        EndColumn = param.Position.Column + param.IdName.Length
                    };
                }

                var paramSymbol = new SymbolInfo
                {
                    Name = param.IdName,
                    Kind = SymbolKind.Parameter,
                    Type = param.AssumptionType ?? "var",
                    Location = paramLocation,
                    Parent = funcSymbol
                };

                funcSymbol.Parameters.Add(paramSymbol);
            }
        }

        _symbolTable[funcName] = funcSymbol;

        // 访问函数体中的局部变量
        VisitBlockStatement(funcValue.BlockStatement);
    }

    /// <summary>
    /// 访问异步函数声明
    /// </summary>
    private void VisitAsyncFunction(AsyncFuncInit asyncFuncInit)
    {
        var funcValue = asyncFuncInit.AsyncFuncValue;
        if (funcValue.Id == null) return;

        var funcName = funcValue.Id.IdName;

        // 尝试从 token 列表中查找位置
        var tokenLocation = FindSymbolLocationFromTokens(funcName, LangTokenType.Func);
        var location = tokenLocation ?? new SourceLocation
        {
            Uri = uri,
            Line = asyncFuncInit.Position.Line,
            Column = asyncFuncInit.Position.Column,
            EndLine = asyncFuncInit.Position.Line,
            EndColumn = asyncFuncInit.Position.Column + funcName.Length
        };

        var paramList = funcValue.Ids != null
            ? string.Join(", ",
                funcValue.Ids.Select(p =>
                    $"{p.IdName}{(string.IsNullOrEmpty(p.AssumptionType) ? "" : ":" + p.AssumptionType)}"))
            : "";
        var returnType = funcValue.Id.AssumptionType ?? "void";
        var funcSignature = $"async func {funcName}({paramList}) -> {returnType}";

        // 提取文档注释
        string? documentation = null;
        if (funcValue.DocComment != null)
        {
            documentation = FormatDocComment(funcValue.DocComment);
        }

        var asyncFuncSymbol = new SymbolInfo
        {
            Name = funcName,
            Kind = SymbolKind.Function,
            Type = funcSignature,
            Location = location,
            Documentation = documentation
        };

        // 添加函数参数到符号的参数列表
        if (funcValue.Ids != null)
        {
            foreach (var param in funcValue.Ids)
            {
                var paramLocation = FindSymbolLocationFromTokens(param.IdName, LangTokenType.Identifier);
                if (paramLocation == null)
                {
                    paramLocation = new SourceLocation
                    {
                        Uri = uri,
                        Line = param.Position.Line,
                        Column = param.Position.Column,
                        EndLine = param.Position.Line,
                        EndColumn = param.Position.Column + param.IdName.Length
                    };
                }

                var paramSymbol = new SymbolInfo
                {
                    Name = param.IdName,
                    Kind = SymbolKind.Parameter,
                    Type = param.AssumptionType ?? "var",
                    Location = paramLocation,
                    Parent = asyncFuncSymbol
                };

                asyncFuncSymbol.Parameters.Add(paramSymbol);
            }
        }

        _symbolTable[funcName] = asyncFuncSymbol;

        // 注意：异步函数的 BlockStatement 是 internal 的，且函数体内的局部变量不应该被添加到全局符号表
    }

    /// <summary>
    /// 访问类声明
    /// </summary>
    private void VisitClass(ClassInit classInit)
    {
        var typeTemplate = classInit.AnyValue;
        var className = typeTemplate.ClassName;

        // 尝试从 token 列表中查找位置和类的 token 范围
        var tokenLocation = FindSymbolLocationFromTokens(className, LangTokenType.Class);
        var location = tokenLocation ?? new SourceLocation
        {
            Uri = uri,
            Line = classInit.Position.Line,
            Column = classInit.Position.Column,
            EndLine = classInit.Position.Line,
            EndColumn = classInit.Position.Column + className.Length
        };

        // 设置当前类的 token 范围
        FindClassTokenRange(className);

        // 提取类文档注释
        string? documentation = null;
        if (typeTemplate.DocComment != null)
        {
            documentation = FormatDocComment(typeTemplate.DocComment);
        }

        var classSymbol = new SymbolInfo
        {
            Name = className,
            Kind = SymbolKind.Class,
            Type = $"class {className}",
            Location = location,
            Documentation = documentation
        };

        _symbolTable[className] = classSymbol;

        // 访问类的成员（方法、属性）
        VisitClassMembers(classSymbol, typeTemplate);

        // 重置类的 token 范围
        _currentClassStartTokenIndex = -1;
        _currentClassEndTokenIndex = -1;
    }

    /// <summary>
    /// 访问类的成员（方法和属性）
    /// </summary>
    private void VisitClassMembers(SymbolInfo classSymbol, TypeTemplate typeTemplate)
    {
        // 访问实例成员
        foreach (var (memberId, memberExpr) in typeTemplate.Variates)
        {
            if (memberExpr is FuncLangValue funcValue)
            {
                // 方法
                var memberSymbol = CreateMethodSymbol(memberId, funcValue, isStatic: false);
                memberSymbol.Parent = classSymbol;
                classSymbol.Members[memberId.IdName] = memberSymbol;
            }
            else
            {
                // 属性/字段
                var memberSymbol = CreatePropertySymbol(memberId, memberExpr, isStatic: false);
                memberSymbol.Parent = classSymbol;
                classSymbol.Members[memberId.IdName] = memberSymbol;
            }
        }

        // 访问静态成员
        foreach (var (memberId, memberExpr) in typeTemplate.StaticVariates)
        {
            if (memberExpr is FuncLangValue funcValue)
            {
                // 静态方法
                var memberSymbol = CreateMethodSymbol(memberId, funcValue, isStatic: true);
                memberSymbol.Parent = classSymbol;
                classSymbol.Members[memberId.IdName] = memberSymbol;
            }
            else
            {
                // 静态属性/字段
                var memberSymbol = CreatePropertySymbol(memberId, memberExpr, isStatic: true);
                memberSymbol.Parent = classSymbol;
                classSymbol.Members[memberId.IdName] = memberSymbol;
            }
        }
    }

    /// <summary>
    /// 创建方法符号信息
    /// </summary>
    private SymbolInfo CreateMethodSymbol(ClassMemberId memberId, FuncLangValue funcValue, bool isStatic)
    {
        var methodName = memberId.IdName;

        // 查找方法定义位置
        var tokenLocation = FindSymbolLocationFromTokens(methodName, LangTokenType.Func);
        var location = tokenLocation ?? new SourceLocation
        {
            Uri = uri,
            Line = funcValue.Position.Line,
            Column = funcValue.Position.Column,
            EndLine = funcValue.Position.Line,
            EndColumn = funcValue.Position.Column + methodName.Length
        };

        // 构建方法签名
        var paramList = funcValue.Ids != null
            ? string.Join(", ",
                funcValue.Ids.Select(p =>
                    $"{p.IdName}{(string.IsNullOrEmpty(p.AssumptionType) ? "" : ":" + p.AssumptionType)}"))
            : "";
        var returnType = funcValue.Id?.AssumptionType ?? "void";
        var staticKeyword = isStatic ? "static " : "";
        var methodSignature = $"{staticKeyword}func {methodName}({paramList}) -> {returnType}";

        // 提取方法文档注释
        string? documentation = null;
        if (funcValue.DocComment != null)
        {
            documentation = FormatDocComment(funcValue.DocComment);
        }
        else
        {
            // 如果函数值没有文档注释，尝试从 tokens 中查找
            var docComment = FindDocCommentForMember(methodName);
            if (docComment != null)
            {
                documentation = FormatDocComment(docComment);
            }
        }

        // 确定访问修饰符
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
            IsStatic = isStatic
        };

        // 添加方法参数到符号的参数列表
        if (funcValue.Ids != null)
        {
            foreach (var param in funcValue.Ids)
            {
                var paramLocation = FindSymbolLocationFromTokens(param.IdName, LangTokenType.Identifier);
                if (paramLocation == null)
                {
                    paramLocation = new SourceLocation
                    {
                        Uri = uri,
                        Line = param.Position.Line,
                        Column = param.Position.Column,
                        EndLine = param.Position.Line,
                        EndColumn = param.Position.Column + param.IdName.Length
                    };
                }

                var paramSymbol = new SymbolInfo
                {
                    Name = param.IdName,
                    Kind = SymbolKind.Parameter,
                    Type = param.AssumptionType ?? "var",
                    Location = paramLocation,
                    Parent = methodSymbol
                };

                methodSymbol.Parameters.Add(paramSymbol);
            }
        }

        return methodSymbol;
    }

    /// <summary>
    /// 创建属性符号信息
    /// </summary>
    private SymbolInfo CreatePropertySymbol(ClassMemberId memberId, LangExpression memberExpr, bool isStatic)
    {
        var propertyName = memberId.IdName;

        var location = new SourceLocation
        {
            Uri = uri,
            Line = memberExpr.Position.Line,
            Column = memberExpr.Position.Column,
            EndLine = memberExpr.Position.Line,
            EndColumn = memberExpr.Position.Column + propertyName.Length
        };

        var propertyType = memberId.AssumptionType ?? "var";
        var staticKeyword = isStatic ? "static " : "";

        // 确定访问修饰符
        var accessModifier = AccessModifier.Public;
        if (memberId.HasModifier(AccessModifierType.Private))
            accessModifier = AccessModifier.Private;
        else if (memberId.HasModifier(AccessModifierType.Protected))
            accessModifier = AccessModifier.Protected;

        // 提取属性文档注释
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
            IsStatic = isStatic
        };
    }

    /// <summary>
    /// 访问变量声明
    /// </summary>
    private void VisitVariable(SetStatement setStatement)
    {
        if (setStatement.Id == null) return;

        var varName = setStatement.Id.IdName;

        // 尝试从 token 列表中查找变量的位置
        var tokenLocation = FindVariableLocationFromTokens(varName);
        var location = tokenLocation ?? new SourceLocation
        {
            Uri = uri,
            Line = setStatement.Position.Line,
            Column = setStatement.Position.Column,
            EndLine = setStatement.Position.Line,
            EndColumn = setStatement.Position.Column
        };

        // 尝试推断类型
        var varType = setStatement.Id.AssumptionType;

        // 如果没有显式类型标注，尝试从赋值表达式推断
        if (string.IsNullOrEmpty(varType) && setStatement.Value != null)
        {
            varType = InferTypeFromExpression(setStatement.Value);
        }

        // 如果仍然无法推断，使用 "any"
        varType ??= "any";

        _symbolTable[varName] = new SymbolInfo
        {
            Name = varName,
            Kind = SymbolKind.Variable,
            Type = varType,
            Location = location
        };
    }

    /// <summary>
    /// 从表达式推断类型
    /// </summary>
    private string? InferTypeFromExpression(LangExpression expr)
    {
        switch (expr)
        {
            // 泛型实例化表达式 Stack<string>()
            case GenericInstanceExpression genericInstance:
                // 获取基础类型名称（忽略泛型参数）
                if (genericInstance.BaseExpression is LangId baseId)
                {
                    var genericClassName = baseId.IdName;
                    return genericClassName;
                }
                break;

            // 实例化表达式 User()
            case Instance instance:
                // Instance 表达式用于类构造函数调用
                var className = instance.Id.IdName;
                return className;

            // 操作表达式 - 处理成员方法调用：outer.createInner()
            case Operation { Opera: LangTokenType.Dot } dotOp:
                // 成员方法调用：Operation(Dot, objectId, Instance(methodName()))
                if (dotOp is { Left: LangId objectId, Right: Instance methodCall })
                {
                    // 获取对象的类型
                    if (_symbolTable.TryGetValue(objectId.IdName, out var objectSymbol))
                    {
                        // 查找对象类型对应的类
                        if (!string.IsNullOrEmpty(objectSymbol.Type) &&
                            _symbolTable.TryGetValue(objectSymbol.Type, out var classSymbol) &&
                            classSymbol.Kind == SymbolKind.Class)
                        {
                            // 查找方法
                            var memberName = methodCall.Id.IdName;
                            if (classSymbol.Members.TryGetValue(memberName, out var memberSymbol) &&
                                memberSymbol.Kind == SymbolKind.Method)
                            {
                                // 从方法签名中提取返回类型
                                var returnType = ExtractReturnTypeFromSignature(memberSymbol.Type);
                                return returnType;
                            }
                        }
                    }
                }
                break;

            // 函数调用表达式 User()
            case FunctionCallExpression funcCall:
                // 检查函数表达式是否是成员访问 (outer.createInner())
                // 成员访问会被解析为 Operation(Dot)，left 是对象，right 是方法名
                if (funcCall.FunctionExpression is Operation { Opera: LangTokenType.Dot, Left: LangId objectId2, Right: LangId methodId2 })
                {
                    // 获取对象的类型
                    if (_symbolTable.TryGetValue(objectId2.IdName, out var objectSymbol2))
                    {
                        // 查找对象类型对应的类
                        if (!string.IsNullOrEmpty(objectSymbol2.Type) &&
                            _symbolTable.TryGetValue(objectSymbol2.Type, out var classSymbol2) &&
                            classSymbol2.Kind == SymbolKind.Class)
                        {
                            // 查找方法
                            var memberName2 = methodId2.IdName;
                            if (classSymbol2.Members.TryGetValue(memberName2, out var memberSymbol2) &&
                                memberSymbol2.Kind == SymbolKind.Method)
                            {
                                // 从方法签名中提取返回类型
                                var returnType2 = ExtractReturnTypeFromSignature(memberSymbol2.Type);
                                return returnType2;
                            }
                        }
                    }
                }
                // 检查函数表达式是否是一个标识符
                else if (funcCall.FunctionExpression is LangId funcId)
                {
                    var funcName = funcId.IdName;
                    // 如果函数名首字母大写，很可能是类构造函数调用
                    if (!string.IsNullOrEmpty(funcName) && char.IsUpper(funcName[0]))
                    {
                        return funcName;
                    }

                    // 尝试从符号表查找函数的返回类型
                    if (_symbolTable.TryGetValue(funcName, out var funcSymbol) &&
                        funcSymbol.Kind == SymbolKind.Function)
                    {
                        var returnType = ExtractReturnTypeFromSignature(funcSymbol.Type);
                        return returnType;
                    }
                }
                break;

            // 字符串字面量
            case StringLangValue:
                return "string";

            // 整数字面量
            case IntLangValue:
                return "int";

            // 浮点数字面量
            case DoubleLangValue:
                return "double";

            // 布尔字面量
            case BoolLangValue:
                return "bool";

            // 标识符 - 从符号表查找
            case LangId langId:
                if (_symbolTable.TryGetValue(langId.IdName, out var refSymbol))
                {
                    return refSymbol.Type;
                }
                break;
        }

        return null;
    }

    /// <summary>
    /// 从函数/方法签名中提取返回类型
    /// </summary>
    /// <param name="signature">函数签名，格式如 "func name(params) -> returnType"</param>
    /// <returns>返回类型，如果无法解析则返回 null</returns>
    private string? ExtractReturnTypeFromSignature(string? signature)
    {
        if (string.IsNullOrEmpty(signature))
            return null;

        // 查找 " -> " 后的返回类型
        var arrowIndex = signature.IndexOf(" -> ", StringComparison.Ordinal);
        if (arrowIndex >= 0)
        {
            var returnType = signature.Substring(arrowIndex + 4).Trim();
            return returnType;
        }

        return null;
    }

    /// <summary>
    /// 格式化文档注释为Markdown
    /// </summary>
    private string FormatDocComment(DocCommentInfo docComment)
    {
        var lines = new List<string>();

        // 摘要
        if (!string.IsNullOrEmpty(docComment.Summary))
        {
            lines.Add(docComment.Summary);
            lines.Add("");
        }

        // 参数
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

        // 返回值
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

        // 异常
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

        // 示例
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

    /// <summary>
    /// 从 token 列表中查找符号的定义位置
    /// </summary>
    private SourceLocation? FindSymbolLocationFromTokens(string symbolName, LangTokenType expectedTokenType)
    {
        if (_tokens == null) return null;

        // 查找符号对应的 token
        // 对于函数和类，我们查找 func/class/async 关键字后面的标识符
        for (int i = 0; i < _tokens.Count; i++)
        {
            var token = _tokens[i];

            // 检查是否是我们要找的标识符
            if (token.Type == LangTokenType.Identifier && token.Value == symbolName)
            {
                // 向前查看是否有 func/class/async 关键字
                if (i > 0)
                {
                    var prevToken = _tokens[i - 1];
                    bool isDefinition = false;

                    // 检查是否是函数定义
                    if (expectedTokenType == LangTokenType.Func && prevToken.Type == LangTokenType.Func)
                    {
                        isDefinition = true;
                    }
                    // 检查是否是异步函数定义
                    else if (expectedTokenType == LangTokenType.Func && prevToken.Type == LangTokenType.Async)
                    {
                        // async func 的情况，定义从 async 关键字开始
                        isDefinition = true;
                    }
                    // 检查是否是类定义
                    else if (expectedTokenType == LangTokenType.Class && prevToken.Type == LangTokenType.Class)
                    {
                        isDefinition = true;
                    }

                    if (isDefinition)
                    {
                        // 使用 token 的实际位置
                        // 注意：词法分析器的行号是 1-based，列号是 0-based
                        var line = token.Line - 1; // 转换为 0-based
                        var column = token.Column; // 列号已经是 0-based，不需要转换

                        return new SourceLocation
                        {
                            Uri = uri,
                            Line = line,
                            Column = column,
                            EndLine = line,
                            EndColumn = column + symbolName.Length
                        };
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 在指定行中查找符号的列位置
    /// </summary>
    private int FindColumnInLine(int line, string symbolName, LangTokenType tokenType)
    {
        if (_sourceCode == null) return 0;

        // 将源代码按行分割
        var lines = _sourceCode.Split('\n');
        if (line < 0 || line >= lines.Length) return 0;

        var lineText = lines[line];

        // 查找符号名称在该行中的位置
        var index = lineText.IndexOf(symbolName, StringComparison.Ordinal);

        // 确保找到的是标识符，而不是其他地方的子字符串
        // 简化处理：返回第一次出现的位置
        return index >= 0 ? index : 0;
    }

    /// <summary>
    /// 从 token 列表中查找变量的定义位置
    /// </summary>
    private SourceLocation? FindVariableLocationFromTokens(string varName)
    {
        if (_tokens == null) return null;

        // 查找变量定义位置
        // 变量定义的模式是: identifier <- expression
        for (int i = 0; i < _tokens.Count - 1; i++)
        {
            var token = _tokens[i];
            var nextToken = _tokens[i + 1];

            // 检查是否是变量定义模式 (identifier <- ...)
            if (token.Type == LangTokenType.Identifier &&
                token.Value == varName &&
                nextToken.Type == LangTokenType.Assignment) // <- 运算符
            {
                // 使用 token 的实际位置
                // 注意：词法分析器的行号是 1-based，列号是 0-based
                var line = token.Line - 1; // 转换为 0-based
                var column = token.Column; // 列号已经是 0-based，不需要转换

                return new SourceLocation
                {
                    Uri = uri,
                    Line = line,
                    Column = column,
                    EndLine = line,
                    EndColumn = column + varName.Length
                };
            }
        }

        return null;
    }

    /// <summary>
    /// 在指定行中查找变量的列位置
    /// </summary>
    private int FindColumnInLineForVariable(int line, string varName)
    {
        if (_sourceCode == null) return 0;

        // 将源代码按行分割
        var lines = _sourceCode.Split('\n');
        if (line < 0 || line >= lines.Length) return 0;

        var lineText = lines[line];

        // 查找变量名在该行中的位置
        // 确保找到的是变量定义(后面跟着 <-)
        var index = lineText.IndexOf(varName, StringComparison.Ordinal);
        if (index >= 0)
        {
            // 简单验证:检查后面是否有 <-
            var afterVar = index + varName.Length;
            if (afterVar < lineText.Length && lineText.Substring(afterVar).TrimStart().StartsWith("<-"))
            {
                return index;
            }
        }

        return 0;
    }

    /// <summary>
    /// 从 tokens 中查找成员的文档注释
    /// </summary>
    private DocCommentInfo? FindDocCommentForMember(string memberName)
    {
        if (_tokens == null) return null;

        // 确定搜索范围
        int startIndex = _currentClassStartTokenIndex >= 0 ? _currentClassStartTokenIndex : 0;
        int endIndex = _currentClassEndTokenIndex >= 0 ? _currentClassEndTokenIndex : _tokens.Count;

        // 在指定范围内查找成员名称对应的标识符 token
        for (int i = startIndex; i < endIndex; i++)
        {
            var token = _tokens[i];

            // 找到成员名称
            if (token.Type == LangTokenType.Identifier && token.Value == memberName)
            {
                // 检查是否是成员定义（后面跟着 :、<-、( 或换行）
                bool isMemberDefinition = false;
                if (i + 1 < endIndex)
                {
                    var nextToken = _tokens[i + 1];
                    if (nextToken.Type == LangTokenType.Colon || // 类型注解
                        nextToken.Type == LangTokenType.Assignment || // 赋值
                        nextToken.Type == LangTokenType.LeftParen) // 函数
                    {
                        isMemberDefinition = true;
                    }
                }

                if (!isMemberDefinition)
                {
                    continue; // 这不是成员定义，跳过
                }

                // 向前查找文档注释
                var docCommentTokens = new List<LangToken>();
                var searchIndex = i - 1;

                // 跳过修饰符 (public, private, static等)
                while (searchIndex >= startIndex)
                {
                    var prevToken = _tokens[searchIndex];

                    // 如果是修饰符，继续向前查找
                    if (prevToken.Type == LangTokenType.Public ||
                        prevToken.Type == LangTokenType.Private ||
                        prevToken.Type == LangTokenType.Protected ||
                        prevToken.Type == LangTokenType.Static ||
                        prevToken.Type == LangTokenType.Func) // func 关键字
                    {
                        searchIndex--;
                        continue;
                    }

                    // 如果是文档注释，收集它
                    if (prevToken.Type == LangTokenType.DocComment)
                    {
                        docCommentTokens.Insert(0, prevToken);
                        searchIndex--;
                        continue;
                    }

                    // 遇到其他 token，停止搜索
                    break;
                }

                // 如果找到文档注释，解析并返回
                if (docCommentTokens.Count > 0)
                {
                    var docCommentLines = docCommentTokens.Select(t => t.Value).ToArray();
                    var rawComment = string.Join("\n", docCommentLines);
                    return DocCommentParser.Parse(rawComment);
                }

                // 如果找到成员定义但没有文档注释，返回 null
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// 查找类的 token 范围（从 class 关键字到右花括号）
    /// </summary>
    private void FindClassTokenRange(string className)
    {
        if (_tokens == null)
        {
            _currentClassStartTokenIndex = -1;
            _currentClassEndTokenIndex = -1;
            return;
        }

        // 查找 class 关键字后跟类名的位置
        for (int i = 0; i < _tokens.Count - 1; i++)
        {
            if (_tokens[i].Type == LangTokenType.Class &&
                _tokens[i + 1].Type == LangTokenType.Identifier &&
                _tokens[i + 1].Value == className)
            {
                _currentClassStartTokenIndex = i;

                // 查找对应的右花括号
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

                // 如果没找到匹配的右花括号，设置为文件末尾
                _currentClassEndTokenIndex = _tokens.Count;
                return;
            }
        }

        // 没找到类定义，使用默认值
        _currentClassStartTokenIndex = -1;
        _currentClassEndTokenIndex = -1;
    }

    /// <summary>
    /// 访问 native 函数声明
    /// </summary>
    private void VisitNativeStatement(NativeStatement nativeStatement)
    {
        // native 语句可以导入函数、类或方法
        // 我们需要从 NativeStatement 的内部字段中提取信息
        // 由于这些字段是私有的，我们需要通过反射或其他方式获取

        // 使用反射获取私有字段
        var type = nativeStatement.GetType();
        var methodNameField = type.GetField("MethodName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var nativeNameField = type.GetField("NativeName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var funcValueField = type.GetField("FuncValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var classNameField = type.GetField("ClassName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var methodName = methodNameField?.GetValue(nativeStatement) as string;
        var nativeName = nativeNameField?.GetValue(nativeStatement) as string;
        var funcValue = funcValueField?.GetValue(nativeStatement) as FuncLangValue;
        var className = classNameField?.GetValue(nativeStatement) as string;

        // 如果有方法名，说明这是一个 native 函数导入
        if (!string.IsNullOrEmpty(methodName))
        {
            var functionName = !string.IsNullOrEmpty(nativeName) ? nativeName : methodName;

            // 构建函数签名
            string funcSignature;
            if (funcValue?.Id != null)
            {
                var paramList = funcValue.Ids != null
                    ? string.Join(", ",
                        funcValue.Ids.Select(p =>
                            $"{p.IdName}{(string.IsNullOrEmpty(p.AssumptionType) ? "" : ":" + p.AssumptionType)}"))
                    : "";
                var returnType = funcValue.Id.AssumptionType ?? "void";
                funcSignature = $"native func {functionName}({paramList}) -> {returnType}";
            }
            else
            {
                funcSignature = $"native func {functionName}(...)";
            }

            var location = new SourceLocation
            {
                Uri = uri,
                Line = nativeStatement.Position.Line,
                Column = nativeStatement.Position.Column,
                EndLine = nativeStatement.Position.Line,
                EndColumn = nativeStatement.Position.Column + functionName.Length
            };

            _symbolTable[functionName] = new SymbolInfo
            {
                Name = functionName,
                Kind = SymbolKind.Function,
                Type = funcSignature,
                Location = location,
                Documentation = $"Native function from {className}"
            };
        }
    }

    /// <summary>
    /// 访问 extern 语句
    /// </summary>
    private void VisitExternStatement(ExternStatement externStatement)
    {
        // extern 语句声明外部函数或变量
        // 使用反射获取私有字段
        var type = externStatement.GetType();
        var functionsField = type.GetField("Functions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (functionsField?.GetValue(externStatement) is List<ExternFunctionDeclaration> functions)
        {
            foreach (var func in functions)
            {
                var functionName = func.Alias ?? func.FunctionName;

                // 构建函数签名
                string funcSignature;
                if (func.FunctionSignature != null)
                {
                    var funcValue = func.FunctionSignature.FuncValue;
                    var paramList = funcValue.Ids != null
                        ? string.Join(", ",
                            funcValue.Ids.Select(p =>
                                $"{p.IdName}{(string.IsNullOrEmpty(p.AssumptionType) ? "" : ":" + p.AssumptionType)}"))
                        : "";
                    var returnType = funcValue.Id?.AssumptionType ?? "void";
                    funcSignature = $"extern func {functionName}({paramList}) -> {returnType}";
                }
                else
                {
                    funcSignature = $"extern func {functionName}(...)";
                }

                var location = new SourceLocation
                {
                    Uri = uri,
                    Line = externStatement.Position.Line,
                    Column = externStatement.Position.Column,
                    EndLine = externStatement.Position.Line,
                    EndColumn = externStatement.Position.Column + functionName.Length
                };

                _symbolTable[functionName] = new SymbolInfo
                {
                    Name = functionName,
                    Kind = SymbolKind.Function,
                    Type = funcSignature,
                    Location = location,
                    Documentation = "External function"
                };
            }
        }
    }

    /// <summary>
    /// 访问 import 语句
    /// </summary>
    private void VisitImportStatement(ImportStatement importStatement)
    {
        // import 语句可能有别名
        // 使用反射获取私有字段
        var type = importStatement.GetType();
        var moduleAliasField = type.GetField("ModuleAlias", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var importStringField = type.GetField("ImportString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var moduleAlias = moduleAliasField?.GetValue(importStatement) as string;
        var importString = importStringField?.GetValue(importStatement) as string;

        // 如果有模块别名，添加到符号表
        if (!string.IsNullOrEmpty(moduleAlias))
        {
            var location = new SourceLocation
            {
                Uri = uri,
                Line = importStatement.Position.Line,
                Column = importStatement.Position.Column,
                EndLine = importStatement.Position.Line,
                EndColumn = importStatement.Position.Column + moduleAlias.Length
            };

            _symbolTable[moduleAlias] = new SymbolInfo
            {
                Name = moduleAlias,
                Kind = SymbolKind.Variable, // 模块作为变量
                Type = "module",
                Location = location,
                Documentation = $"Imported module: {importString}"
            };
        }
    }
}
