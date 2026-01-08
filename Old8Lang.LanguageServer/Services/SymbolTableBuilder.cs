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
        if (string.IsNullOrEmpty(varType))
        {
            varType = InferTypeFromExpression(setStatement.Value);
        }

        // 如果仍然无法推断，使用 "var"
        varType ??= "var";

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
            // 函数调用表达式 User()
            case FunctionCallExpression funcCall:
                // 检查函数表达式是否是一个标识符
                if (funcCall.FunctionExpression is LangId funcId)
                {
                    var funcName = funcId.IdName;
                    // 如果函数名首字母大写，很可能是类构造函数调用
                    if (!string.IsNullOrEmpty(funcName) && char.IsUpper(funcName[0]))
                    {
                        return funcName;
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
                        // 返回标识符的位置
                        // 从所在行的文本中查找符号名称的列位置
                        var line = token.Line - 1; // 转换为0-based
                        var column = FindColumnInLine(line, symbolName, expectedTokenType);

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
                var line = token.Line - 1; // 转换为0-based
                var column = FindColumnInLineForVariable(line, varName);

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
}