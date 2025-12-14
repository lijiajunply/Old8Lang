using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser.Core;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// 函数解析器，负责解析函数声明、参数列表、Lambda
/// </summary>
public class FunctionParser(
    ParserContext context,
    Func<StatementParser> statementParserFactory,
    Func<ExpressionParser> expressionParserFactory)
    : ParserBase(context)
{
    /// <summary>
    /// 解析函数声明
    /// funcDeclaration = ( "func" identifier | identifier ) "(" idList? ")" ( "->" )? block
    /// </summary>
    public FuncInit ParseFuncDeclaration()
    {
        var isUseFunc = CurrentToken.Type == LangTokenType.Func;
        if (isUseFunc)
        {
            Expect(LangTokenType.Func);
        }

        var funcName = ParseIdentifier();
        var returnType = string.Empty;

        // 检查是否有类型注解语法：identifier:returnType
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);
            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError("请返回类型标识符");
            }

            returnType = CurrentToken.Value;
            Expect(LangTokenType.Identifier);
        }

        Expect(LangTokenType.LeftParen);
        var parameters = ParseIdList();
        Expect(LangTokenType.RightParen);

        // 检查是否有箭头语法用于返回类型注解或函数体
        if (CurrentToken.Type == LangTokenType.Arrow)
        {
            Expect(LangTokenType.Arrow);
            // 解析返回类型标识符（如果有）
            if (isUseFunc && CurrentToken.Type == LangTokenType.Identifier)
            {
                if (!string.IsNullOrEmpty(returnType))
                {
                    throw CreateSyntaxError("函数返回类型重复声明");
                }

                returnType = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
            }
        }

        // 如果有返回类型注解，创建新的LangId并设置AssumptionType
        var updatedFuncName = funcName;
        if (!string.IsNullOrEmpty(returnType))
        {
            updatedFuncName = new LangId(funcName.IdName, returnType, position: funcName.Position);
        }

        var stmtParser = statementParserFactory();
        var block = stmtParser.ParseBlock();

        // 普通函数声明,生成 FuncInit，设置 IsLambda 为 false
        return new FuncInit(new FuncLangValue(updatedFuncName, parameters, block, isLambda: false));
    }

    /// <summary>
    /// 解析异步函数声明
    /// asyncFuncDeclaration = "async" "func" identifier "(" idList? ")" ( "->" returnType )? block
    /// </summary>
    public AsyncFuncInit ParseAsyncFuncDeclaration()
    {
        // 这里假设 async 和 func 关键字已经被消费了
        var funcName = ParseIdentifier();
        var returnType = string.Empty;

        // 检查是否有类型注解语法：identifier:returnType
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);
            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError("请返回类型标识符");
            }

            returnType = CurrentToken.Value;
            Expect(LangTokenType.Identifier);
        }

        Expect(LangTokenType.LeftParen);
        var parameters = ParseIdList();
        Expect(LangTokenType.RightParen);

        // 检查是否有箭头语法用于返回类型注解或函数体
        if (CurrentToken.Type == LangTokenType.Arrow)
        {
            Expect(LangTokenType.Arrow);
            // 解析返回类型标识符（如果有）
            if (CurrentToken.Type == LangTokenType.Identifier)
            {
                if (!string.IsNullOrEmpty(returnType))
                {
                    throw CreateSyntaxError("函数返回类型重复声明");
                }

                returnType = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
            }
        }

        // 如果有返回类型注解，创建新的LangId并设置AssumptionType
        var updatedFuncName = funcName;
        if (!string.IsNullOrEmpty(returnType))
        {
            updatedFuncName = new LangId(funcName.IdName, returnType, position: funcName.Position);
        }

        var stmtParser = statementParserFactory();
        var block = stmtParser.ParseBlock();

        // 异步函数声明，生成 AsyncFuncInit
        return new AsyncFuncInit(
            new AsyncFuncLangValue(updatedFuncName, parameters, block, updatedFuncName.Position),
            updatedFuncName.Position);
    }

    /// <summary>
    /// 解析标识符列表（函数参数）
    /// </summary>
    public List<LangId> ParseIdList()
    {
        var ids = new List<LangId>();

        // 检查当前token是否是标识符或关键字
        if (CurrentToken.Type is LangTokenType.Identifier or LangTokenType.Func or LangTokenType.Class
            or LangTokenType.If or LangTokenType.Else or LangTokenType.While or LangTokenType.For
            or LangTokenType.Return or LangTokenType.Import or LangTokenType.True or LangTokenType.False)
        {
            // 解析第一个参数，允许类型注解
            ids.Add(ParseTypedIdentifier(true));

            // 跳过默认参数值（如果有）
            if (CurrentToken.Type == LangTokenType.Assignment)
            {
                // 跳过赋值符号和表达式
                while (CurrentToken.Type != LangTokenType.Comma && CurrentToken.Type != LangTokenType.RightParen)
                {
                    CurrentIndex++;
                }
            }

            // 解析更多参数，允许类型注解
            while (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);
                ids.Add(ParseTypedIdentifier(true));

                // 跳过默认参数值（如果有）
                if (CurrentToken.Type == LangTokenType.Assignment)
                {
                    // 跳过赋值符号和表达式
                    while (CurrentToken.Type != LangTokenType.Comma && CurrentToken.Type != LangTokenType.RightParen)
                    {
                        CurrentIndex++;
                    }
                }
            }
        }

        return ids;
    }

    /// <summary>
    /// 解析参数列表（函数调用）
    /// </summary>
    public List<LangExpression> ParseArgList()
    {
        var args = new List<LangExpression>();

        if (CurrentToken.Type == LangTokenType.RightParen)
        {
            return args;
        }

        var exprParser = expressionParserFactory();
        args.Add(exprParser.ParseExpression());
        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            args.Add(exprParser.ParseExpression());
        }

        return args;
    }

    /// <summary>
    /// 解析带有类型注解或默认参数的标识符，用于赋值语句、函数参数和lambda参数
    /// </summary>
    public LangId ParseTypedIdentifier(bool isNeedDefaultValue)
    {
        var identifierToken = CurrentToken;
        var position = new SourcePosition(identifierToken.Line, identifierToken.Column,
            tokenValue: identifierToken.Value);
        var value = identifierToken.Value;

        // 检查当前token是否是标识符或关键字
        if (CurrentToken.Type is LangTokenType.Identifier or LangTokenType.Func or LangTokenType.Class
            or LangTokenType.If or LangTokenType.Else or LangTokenType.While or LangTokenType.For
            or LangTokenType.Return or LangTokenType.Import or LangTokenType.True or LangTokenType.False)
        {
            CurrentIndex++;
        }
        else
        {
            Expect(LangTokenType.Identifier);
        }

        // 处理类型注解或默认参数：identifier:type 或 identifier:default_value
        var typeAnnotation = "";
        LangExpression? defaultValue = null;

        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);

            // 检查下一个token类型，判断是类型注解还是默认参数
            if (CurrentToken.Type is LangTokenType.Identifier)
            {
                // 类型注解：identifier:type
                typeAnnotation = CurrentToken.Value;
                if (typeAnnotation == "")
                {
                    throw CreateSyntaxError("类型注解不能为空");
                }

                Expect(LangTokenType.Identifier);
            }
            else if (isNeedDefaultValue)
            {
                // 默认参数：identifier:default_value
                var exprParser = expressionParserFactory();
                defaultValue = exprParser.ParseExpression();
            }
        }

        return new LangId(value, typeAnnotation, defaultValue, position);
    }

    /// <summary>
    /// 解析标识符
    /// </summary>
    public LangId ParseIdentifier()
    {
        var identifierToken = CurrentToken;
        var position = new SourcePosition(identifierToken.Line, identifierToken.Column,
            tokenValue: identifierToken.Value);
        var value = identifierToken.Value;

        // 检查当前token是否是标识符或关键字
        if (CurrentToken.Type is LangTokenType.Identifier or LangTokenType.Func or LangTokenType.Class
            or LangTokenType.If or LangTokenType.Else or LangTokenType.While or LangTokenType.For
            or LangTokenType.Return or LangTokenType.Import or LangTokenType.True or LangTokenType.False)
        {
            CurrentIndex++;
        }
        else
        {
            Expect(LangTokenType.Identifier);
        }

        // 默认不处理类型注解
        return new LangId(value, position: position);
    }
}
