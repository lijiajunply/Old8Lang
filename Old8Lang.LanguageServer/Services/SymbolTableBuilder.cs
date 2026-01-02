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
public class SymbolTableBuilder(string uri, List<LangToken>? tokens = null)
{
    private readonly Dictionary<string, SymbolInfo> _symbolTable = new();
    private readonly List<LangToken>? _tokens = tokens;

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
        }
    }

    /// <summary>
    /// 访问函数声明
    /// </summary>
    private void VisitFunction(FuncInit funcInit)
    {
        var funcValue = funcInit.FuncLangValue;
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

        _symbolTable[funcName] = new SymbolInfo
        {
            Name = funcName,
            Kind = SymbolKind.Function,
            Type = funcSignature,
            Location = location,
            Documentation = documentation
        };

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

        _symbolTable[funcName] = new SymbolInfo
        {
            Name = funcName,
            Kind = SymbolKind.Function,
            Type = funcSignature,
            Location = location,
            Documentation = documentation
        };

        // 注意：异步函数的 BlockStatement 是 internal 的，且函数体内的局部变量不应该被添加到全局符号表
    }

    /// <summary>
    /// 访问类声明
    /// </summary>
    private void VisitClass(ClassInit classInit)
    {
        var typeTemplate = classInit.AnyLangValue;
        var className = typeTemplate.ClassName;

        // 尝试从 token 列表中查找位置
        var tokenLocation = FindSymbolLocationFromTokens(className, LangTokenType.Class);
        var location = tokenLocation ?? new SourceLocation
        {
            Uri = uri,
            Line = classInit.Position.Line,
            Column = classInit.Position.Column,
            EndLine = classInit.Position.Line,
            EndColumn = classInit.Position.Column + className.Length
        };

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

        // 确定访问修饰符
        var accessModifier = AccessModifier.Public;
        if (memberId.HasModifier(AccessModifierType.Private))
            accessModifier = AccessModifier.Private;
        else if (memberId.HasModifier(AccessModifierType.Protected))
            accessModifier = AccessModifier.Protected;

        return new SymbolInfo
        {
            Name = methodName,
            Kind = SymbolKind.Method,
            Type = methodSignature,
            Location = location,
            Documentation = documentation,
            AccessModifier = accessModifier,
            IsStatic = isStatic
        };
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

        return new SymbolInfo
        {
            Name = propertyName,
            Kind = SymbolKind.Property,
            Type = $"{staticKeyword}{propertyType}",
            Location = location,
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
        var location = new SourceLocation
        {
            Uri = uri,
            Line = setStatement.Position.Line,
            Column = setStatement.Position.Column,
            EndLine = setStatement.Position.Line,
            EndColumn = setStatement.Position.Column
        };

        var varType = setStatement.Id.AssumptionType ?? "var";

        _symbolTable[varName] = new SymbolInfo
        {
            Name = varName,
            Kind = SymbolKind.Variable,
            Type = varType,
            Location = location
        };
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
                        // async func 的情况，需要再向前查找
                        isDefinition = true;
                    }
                    // 检查是否是类定义
                    else if (expectedTokenType == LangTokenType.Class && prevToken.Type == LangTokenType.Class)
                    {
                        isDefinition = true;
                    }

                    if (isDefinition)
                    {
                        return new SourceLocation
                        {
                            Uri = uri,
                            Line = token.Line - 1, // Token 从 1 开始，LSP 从 0 开始
                            Column = token.Column - 1,
                            EndLine = token.Line - 1,
                            EndColumn = token.Column - 1 + symbolName.Length
                        };
                    }
                }
            }
        }

        return null;
    }
}