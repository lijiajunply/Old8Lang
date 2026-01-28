using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// 语句解析器 - 特殊语句
/// </summary>
public partial class StatementParser
{
    private EnumInit ParseEnumDeclaration()
    {
        var startToken = CurrentToken;
        var startPos = CreateSourcePosition(startToken);

        // 消费 "enum" 关键字
        Expect(LangTokenType.Enum);

        // 解析枚举名称
        if (CurrentToken.Type != LangTokenType.Identifier)
        {
            throw CreateSyntaxError($"枚举定义需要枚举名称，但得到 {CurrentToken.Type}");
        }

        var enumName = CurrentToken.Value;
        CurrentIndex++;

        // 消费左花括号
        Expect(LangTokenType.LeftBrace);

        // 解析枚举成员
        var members = new List<(string name, LangExpression? value)>();

        // 处理空枚举
        if (CurrentToken.Type == LangTokenType.RightBrace)
        {
            CurrentIndex++;
            return new EnumInit(enumName, members, startPos);
        }

        // 解析第一个成员
        while (true)
        {
            // 解析成员名称
            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError($"枚举成员需要标识符，但得到 {CurrentToken.Type}");
            }

            var memberName = CurrentToken.Value;
            CurrentIndex++;

            // 检查是否有显式赋值
            LangExpression? memberValue = null;
            if (CurrentToken.Type == LangTokenType.Assignment)
            {
                CurrentIndex++;
                // 解析值表达式（必须是常量表达式，通常是整数字面量）
                memberValue = expressionParser.ParseExpression();
            }

            members.Add((memberName, memberValue));

            // 检查是否有更多成员
            if (CurrentToken.Type == LangTokenType.Comma)
            {
                CurrentIndex++;
                // 允许尾随逗号
                if (CurrentToken.Type == LangTokenType.RightBrace)
                {
                    break;
                }

                continue;
            }

            break;
        }

        // 消费右花括号
        Expect(LangTokenType.RightBrace);

        return new EnumInit(enumName, members, startPos);
    }


    private UsingStatement ParseUsingStatement()
    {
        var startPos = new SourcePosition(CurrentToken.Line, CurrentToken.Column);
        Expect(LangTokenType.Using);

        string? variableName = null;
        LangExpression resourceExpression;

        // 检查是否是变量声明形式: using varName <- expr
        if (CurrentToken.Type == LangTokenType.Identifier)
        {
            var nextToken = Peek();
            if (nextToken.Type == LangTokenType.Assignment)
            {
                // using varName <- expr { ... }
                variableName = CurrentToken.Value;
                CurrentIndex++; // 消费标识符
                Expect(LangTokenType.Assignment); // 消费 <-
                resourceExpression = expressionParser.ParseExpression();
            }
            else
            {
                // using expr { ... }
                resourceExpression = expressionParser.ParseExpression();
            }
        }
        else
        {
            // using expr { ... }
            resourceExpression = expressionParser.ParseExpression();
        }

        // 解析 using 块（ParseBlock 会自己处理花括号）
        var blockStatement = ParseBlock();

        return new UsingStatement(variableName, resourceExpression, blockStatement, startPos);
    }

    /// <summary>
    /// 解析 defer 语句
    /// 语法：
    ///   defer statement
    ///   defer { ... }
    /// </summary>

    private DeferStatement ParseDeferStatement()
    {
        var startPos = new SourcePosition(CurrentToken.Line, CurrentToken.Column);
        Expect(LangTokenType.Defer);

        OldStatement statement;

        // 检查是否是代码块形式
        if (CurrentToken.Type == LangTokenType.LeftBrace)
        {
            // defer { ... }
            statement = ParseBlock();
        }
        else
        {
            // defer statement
            statement = ParseStatement();
        }

        return new DeferStatement(statement, startPos);
    }


    private SelectStatement ParseSelectStatement()
    {
        var startPos = new SourcePosition(CurrentToken.Line, CurrentToken.Column);
        Expect(LangTokenType.Select);
        Expect(LangTokenType.LeftBrace);

        var cases = new List<SelectCase>();
        BlockStatement? defaultCase = null;

        while (CurrentToken.Type != LangTokenType.RightBrace)
        {
            if (CurrentToken.Type == LangTokenType.Case)
            {
                Expect(LangTokenType.Case);

                // 解析第一个表达式
                var firstExpr = expressionParser.ParseExpression();

                // 判断是接收还是发送操作
                if (CurrentToken.Type == LangTokenType.From)
                {
                    // 接收操作：case value from channel -> { ... }
                    Expect(LangTokenType.From);

                    // 解析 channel 表达式
                    var channelExpr = expressionParser.ParseExpression();

                    Expect(LangTokenType.Arrow);

                    // 解析块
                    var block = ParseBlock();

                    // 提取变量名（如果第一个表达式是标识符）
                    string? variableName = null;
                    if (firstExpr is LangId id)
                    {
                        variableName = id.IdName;
                    }

                    cases.Add(new SelectCase(
                        channelExpression: channelExpr,
                        variableName: variableName,
                        blockStatement: block,
                        position: startPos
                    ));
                }
                else if (CurrentToken.Type == LangTokenType.Assignment)
                {
                    // 发送操作：case channel <- value -> { ... }
                    Expect(LangTokenType.Assignment);

                    // 解析发送值表达式
                    var sendValueExpr = expressionParser.ParseExpression();

                    Expect(LangTokenType.Arrow);

                    // 解析块
                    var block = ParseBlock();

                    cases.Add(new SelectCase(
                        channelExpression: firstExpr,
                        sendValueExpression: sendValueExpr,
                        blockStatement: block,
                        position: startPos
                    ));
                }
                else
                {
                    throw CreateSyntaxError(
                        $"select case 中期望 'from'（接收操作）或 '<-'（发送操作），但得到 {CurrentToken.Type}");
                }
            }
            else if (CurrentToken.Type == LangTokenType.Default)
            {
                Expect(LangTokenType.Default);
                Expect(LangTokenType.Arrow);
                defaultCase = ParseBlock();
            }
            else
            {
                throw CreateSyntaxError(
                    $"select 语句中期望 'case' 或 'default'，但得到 {CurrentToken.Type}");
            }
        }

        Expect(LangTokenType.RightBrace);
        return new SelectStatement(cases, defaultCase, startPos);
    }

    /// <summary>
    /// externStatement = "native" "extern" STRING (callingConvention)? externBody
    /// callingConvention = "cdecl" | "stdcall" | "winapi"
    /// externBody = "func" funcDeclaration ("as" identifier)?
    ///            | "{" funcDeclaration ("," funcDeclaration)* "}"
    /// funcDeclaration = identifier "(" parameters ")" "->" type
    /// </summary>
    /// <returns>Extern 语句</returns>

    public ExternStatement ParseExternStatement()
    {
        Expect(LangTokenType.Extern);

        // 解析 DLL 名称
        var dllName = CurrentToken.Value;
        Expect(LangTokenType.String);

        // 解析可选的调用约定（默认为 Cdecl）
        var defaultCallingConvention = CallingConventionType.Cdecl;
        if (CurrentToken.Type == LangTokenType.Identifier)
        {
            var conventionStr = CurrentToken.Value.ToLower();
            if (conventionStr is "cdecl" or "stdcall" or "winapi")
            {
                defaultCallingConvention = conventionStr switch
                {
                    "cdecl" => CallingConventionType.Cdecl,
                    "stdcall" => CallingConventionType.StdCall,
                    "winapi" => CallingConventionType.WinApi,
                    _ => CallingConventionType.Cdecl
                };
                Expect(LangTokenType.Identifier);
            }
            // 检查是否是 C# 类名（用于 extern "C#:System" Math { ... } 或 extern "dotnetdll:MyLib.dll" MyClass { ... } 语法）
            else if (dllName.StartsWith("C#:", StringComparison.OrdinalIgnoreCase) ||
                     dllName.StartsWith("cs:", StringComparison.OrdinalIgnoreCase) ||
                     dllName.StartsWith("csharp:", StringComparison.OrdinalIgnoreCase) ||
                     dllName.StartsWith("dotnetdll:", StringComparison.OrdinalIgnoreCase))
            {
                // 解析类名并附加到 dllName
                var className = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
                dllName = $"{dllName} {className}";
            }
        }

        var functions = new List<ExternFunctionDeclaration>();

        // 检查是否是函数块：{ func1, func2, ... }
        if (CurrentToken.Type == LangTokenType.LeftBrace)
        {
            Expect(LangTokenType.LeftBrace);

            while (CurrentToken.Type != LangTokenType.RightBrace)
            {
                // 解析单个函数声明
                var funcDecl = ParseExternFunctionDeclaration(defaultCallingConvention);
                functions.Add(funcDecl);

                // 跳过可选的逗号
                if (CurrentToken.Type == LangTokenType.Comma)
                {
                    Expect(LangTokenType.Comma);
                }
            }

            Expect(LangTokenType.RightBrace);
        }
        else
        {
            // 单个函数声明
            var funcDecl = ParseExternFunctionDeclaration(defaultCallingConvention);
            functions.Add(funcDecl);
        }

        // 自动检测 extern 类型
        var externType = ExternStatement.DetectExternType(dllName);

        return new ExternStatement(dllName, functions, defaultCallingConvention, externType);
    }

    /// <summary>
    /// 解析外部函数声明
    /// funcDeclaration = (callingConvention)? "func" identifier "(" parameters ")" "->" type ("as" identifier)?
    /// </summary>

    private ExternFunctionDeclaration ParseExternFunctionDeclaration(CallingConventionType defaultConvention)
    {
        // 解析可选的调用约定
        var callingConvention = defaultConvention;
        if (CurrentToken.Type == LangTokenType.Identifier)
        {
            var conventionStr = CurrentToken.Value.ToLower();
            if (conventionStr is "cdecl" or "stdcall" or "winapi")
            {
                callingConvention = conventionStr switch
                {
                    "cdecl" => CallingConventionType.Cdecl,
                    "stdcall" => CallingConventionType.StdCall,
                    "winapi" => CallingConventionType.WinApi,
                    _ => defaultConvention
                };
                Expect(LangTokenType.Identifier);
            }
        }

        // 期望 func 关键字
        Expect(LangTokenType.Func);

        // 解析函数名
        var functionName = CurrentToken.Value;
        Expect(LangTokenType.Identifier);

        // 解析参数列表
        Expect(LangTokenType.LeftParen);
        var parameters = functionParser.ParseIdList();
        Expect(LangTokenType.RightParen);

        // 解析返回类型（必须有 -> returnType）
        Expect(LangTokenType.Arrow);
        var returnType = string.Empty;
        if (CurrentToken.Type == LangTokenType.Identifier)
        {
            // Extern 函数的返回类型应该是简单标识符，不支持复杂类型注解
            // 使用简单的 token 读取，避免 ParseComplexTypeAnnotation 跨行读取导致的解析问题
            returnType = CurrentToken.Value;
            Expect(LangTokenType.Identifier);
        }

        // 创建函数签名（使用 FuncInit 来存储签名信息）
        var funcName = new LangId(functionName, returnType);
        var funcValue = new FuncLangValue(funcName, parameters, new BlockStatement(new List<IOldLangTree>()), null, default, false);
        var funcSignature = new FuncInit(funcValue);

        // 解析可选的别名
        string? alias = null;
        if (CurrentToken.Type == LangTokenType.As)
        {
            Expect(LangTokenType.As);
            alias = CurrentToken.Value;
            Expect(LangTokenType.Identifier);
        }

        return new ExternFunctionDeclaration(functionName, funcSignature, alias, callingConvention);
    }

    /// <summary>
    /// 检查字符串是否是调用约定关键字
    /// </summary>

    private static bool IsCallingConvention(string value)
    {
        var lower = value.ToLower();
        return lower is "cdecl" or "stdcall" or "winapi";
    }
}
