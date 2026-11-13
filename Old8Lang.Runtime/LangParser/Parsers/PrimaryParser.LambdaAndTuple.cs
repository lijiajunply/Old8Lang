using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// Primary 表达式解析器 - Lambda和元组解析
/// </summary>
public partial class PrimaryParser
{
    public LangExpression ParseLambdaOrTuple()
    {
        var leftParenToken = CurrentToken;
        var position = new SourcePosition(leftParenToken.Line, leftParenToken.Column, tokenValue: leftParenToken.Value);
        Expect(LangTokenType.LeftParen);

        // 保存当前位置，用于回滚
        var savedIndex = CurrentIndex;

        // 检查是否是异步Lambda表达式
        var isAsync = false;
        if (CurrentToken.Type == LangTokenType.Async && Peek().Type == LangTokenType.RightParen &&
            Peek(2).Type == LangTokenType.Arrow)
        {
            // 异步无参数Lambda：async () -> block 或 async () -> expression
            isAsync = true;
            Expect(LangTokenType.Async);
        }

        // 检查是否是Lambda表达式：() -> block 或 (params) -> block
        if (CurrentToken.Type == LangTokenType.RightParen)
        {
            Expect(LangTokenType.RightParen);

            // 检查是否有返回类型注解：():returnType -> ...
            LangId? returnTypeAnnotation = null;
            if (CurrentToken.Type == LangTokenType.Colon && Peek().Type == LangTokenType.Identifier)
            {
                Expect(LangTokenType.Colon);
                var returnTypeName = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
                returnTypeAnnotation = new LangId("", returnTypeName, null, position: position);
            }

            // 检查箭头符号
            if (CurrentToken.Type == LangTokenType.Arrow)
            {
                Expect(LangTokenType.Arrow);

                BlockStatement block;

                // 检查是块语句还是表达式
                if (CurrentToken.Type == LangTokenType.LeftBrace)
                {
                    // 块语句：():returnType -> { ... }
                    block = statementParserFactory().ParseBlock();
                }
                else
                {
                    // 表达式：():returnType -> expression
                    // 我们需要将表达式转换为块语句，添加return
                    var expr = expressionParserFactory().ParseExpression();
                    var returnStmt = new ReturnStatement(expr, position);
                    block = new BlockStatement([returnStmt]);
                }

                // 创建Lambda表达式，根据isAsync标志决定创建AsyncFuncLangValue还是FuncLangValue
                if (isAsync)
                {
                    return new AsyncFuncLangValue(returnTypeAnnotation, [], block, position);
                }
                else
                {
                    return new FuncLangValue(returnTypeAnnotation, [], block, null, position, true);
                }
            }
        }

        // 检查是否是有参数的Lambda表达式
        // 只有当括号内的内容是标识符列表时，才可能是Lambda表达式
        // 如果是其他表达式（如数字、字符串、表达式调用等），则是元组
        var isLambda = true;
        var ids = new List<LangId>();

        // 检查是否是异步Lambda表达式（有参数）
        if (CurrentToken.Type == LangTokenType.Async && Peek().Type == LangTokenType.Identifier)
        {
            // 异步有参数Lambda：async (params) -> block 或 async (params) -> expression
            isAsync = true;
            Expect(LangTokenType.Async);
        }

        // 检查第一个元素是否是标识符
        if (CurrentToken.Type == LangTokenType.Identifier)
        {
            // 预读检查：如果是 identifier: number/string/... 则不是Lambda，而是命名元组
            if (Peek(1).Type == LangTokenType.Colon)
            {
                var tokenAfterColon = Peek(2);
                // 如果冒号后不是类型名称（identifier），则是命名元组
                if (tokenAfterColon.Type != LangTokenType.Identifier)
                {
                    isLambda = false;
                }
                else
                {
                    // 冒号后是标识符，但需要检查是否为支持的类型名称
                    var potentialType = tokenAfterColon.Value;
                    var supportedTypes = new[] { "int", "double", "string", "bool", "char", "void", "list", "dict", "any", "tuple", "array" };
                    var isGenericTypeParameter = potentialType.Length == 1 && char.IsUpper(potentialType[0]);

                    if (!supportedTypes.Contains(potentialType) && !isGenericTypeParameter)
                    {
                        // 不是支持的类型，说明是命名元组
                        isLambda = false;
                    }
                }
            }

            // 如果还认为是Lambda，继续解析Lambda参数
            if (isLambda)
            {
                // 解析第一个参数，允许类型注解
                ids.Add(ParseLambdaParameter());

                // 解析更多参数，允许类型注解
                while (CurrentToken.Type == LangTokenType.Comma)
                {
                    Expect(LangTokenType.Comma);
                    if (CurrentToken.Type != LangTokenType.Identifier)
                    {
                        // 不是标识符，不是Lambda表达式
                        isLambda = false;
                        break;
                    }

                    ids.Add(ParseLambdaParameter());
                }
            }

            // 检查是否有箭头符号或返回类型注解
            if (isLambda && CurrentToken.Type == LangTokenType.RightParen)
            {
                Expect(LangTokenType.RightParen);

                // 检查是否有返回类型注解：(params):returnType -> ...
                LangId? returnTypeAnnotation = null;
                if (CurrentToken.Type == LangTokenType.Colon)
                {
                    Expect(LangTokenType.Colon);

                    // 解析简单返回类型注解
                    if (CurrentToken.Type == LangTokenType.Identifier)
                    {
                        var returnTypeName = CurrentToken.Value;
                        Expect(LangTokenType.Identifier);

                        // 验证是否为支持的类型
                        var supportedTypes = new[]
                            { "int", "double", "string", "bool", "char", "void", "list", "dict" };
                        if (!supportedTypes.Contains(returnTypeName))
                        {
                            throw CreateSyntaxError(
                                $"不支持的返回类型注解: {returnTypeName}。支持的类型: int, double, string, bool, char, void, list, dict");
                        }

                        returnTypeAnnotation = new LangId("", returnTypeName, null, position: position);
                    }
                    else
                    {
                        throw CreateSyntaxError($"期望返回类型名称，但得到 {CurrentToken.Type}");
                    }
                }

                // 检查箭头符号
                if (CurrentToken.Type == LangTokenType.Arrow)
                {
                    Expect(LangTokenType.Arrow);

                    BlockStatement block;

                    // 检查是块语句还是表达式
                    if (CurrentToken.Type == LangTokenType.LeftBrace)
                    {
                        // 块语句：(params):returnType -> { ... }
                        block = statementParserFactory().ParseBlock();
                    }
                    else
                    {
                        // 表达式：(params):returnType -> expression
                        // 我们需要将表达式转换为块语句，添加return
                        var expr = expressionParserFactory().ParseExpression();
                        var returnStmt = new ReturnStatement(expr, position);
                        block = new BlockStatement([returnStmt]);
                    }

                    // 创建Lambda表达式，根据isAsync标志决定创建AsyncFuncLangValue还是FuncLangValue
                    if (isAsync)
                    {
                        return new AsyncFuncLangValue(returnTypeAnnotation, ids, block, position);
                    }
                    else
                    {
                        return new FuncLangValue(returnTypeAnnotation, ids, block, null, position, true);
                    }
                }
            }

            // 严格检查：如果看起来像 Lambda 参数列表但缺少 ->
            if (isLambda && CurrentToken.Type == LangTokenType.RightParen)
            {
                var rightParenLine = CurrentToken.Line;
                var nextToken = Peek();

                // 检查右括号后是否还有内容，且在同一行，且不是分号
                if (nextToken.Type != LangTokenType.Semicolon &&
                    nextToken.Type != LangTokenType.EndOfFile &&
                    nextToken.Line == rightParenLine)
                {
                    // 构建参数列表字符串用于错误消息
                    var paramList = string.Join(", ", ids.Select(id => id.IdName));

                    throw CreateSyntaxError(
                        $"语法错误：Lambda 表达式缺少箭头 '->'。\n" +
                        $"检测到参数列表 '({paramList})'，但缺少 '->' 符号。\n" +
                        $"建议：使用 '({paramList}) -> expression' 或 '({paramList}) -> {{ ... }}' 格式定义 Lambda 表达式。\n" +
                        $"如果这不是 Lambda 表达式，请在参数列表后添加分号 ';' 或换行符。");
                }
            }
        }
        else
        {
            // 第一个元素不是标识符，不是Lambda表达式
            // isLambda = false;
        }

        // 元组：(expr1, expr2, ...) 或命名元组：(x: expr1, y: expr2, ...)
        // 回滚到左括号后，重新解析为表达式列表
        CurrentIndex = savedIndex;

        var elements = new List<LangExpression>();
        var fieldNames = new List<string?>(); // 字段名列表，null表示未命名
        bool hasAnyFieldName = false; // 标记是否有任何命名字段

        // 空括号情况：()
        if (CurrentToken.Type == LangTokenType.RightParen)
        {
            // 空括号没有箭头，语义不明确，抛出错误
            // 注意：() -> expr 的Lambda形式在前面已经处理过了（第1890行）
            throw CreateSyntaxError(
                "语法错误：空括号 '()' 不能作为表达式。建议：如果要定义无参Lambda，请使用 '() -> expression' 或 '() -> { ... }' 格式。");
        }

        // 解析第一个元素（可能是命名字段）
        var firstElement = ParseTupleElementWithOptionalName(out string? firstName);
        elements.Add(firstElement);
        fieldNames.Add(firstName);
        if (firstName is not null) hasAnyFieldName = true;

        // 检查是否有逗号
        bool hasComma = CurrentToken.Type == LangTokenType.Comma;

        // 解析更多元素
        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);

            // 检查是否还有元素，或者是单元素元组的结束
            if (CurrentToken.Type == LangTokenType.RightParen)
            {
                // 单元素元组，没有更多元素
                break;
            }

            var element = ParseTupleElementWithOptionalName(out string? fieldName);
            elements.Add(element);
            fieldNames.Add(fieldName);
            if (fieldName is not null) hasAnyFieldName = true;
        }

        // 必须是右括号，否则抛出语法错误
        Expect(LangTokenType.RightParen);

        // 构建元组，支持任意数量元素
        if (elements.Count == 1)
        {
            // 检查是否是单元素元组还是括号表达式
            // 如果没有逗号，那么是括号表达式：(expr)
            // 如果有逗号，那么是单元素元组：(expr,)
            if (!hasComma)
            {
                // 单个表达式，返回表达式本身，不是元组
                return elements[0];
            }

            // 单元素元组：(expr,) 或 (x: expr,)
            // 注意：单元素元组不支持命名字段，因为命名字段需要多个元素才有意义
            // 使用列表构造函数创建单元素元组，避免引入 NullLangValue
            return new TupleLangValue([elements[0]], position);
        }

        // 多元素元组：使用支持命名字段的构造函数
        if (hasAnyFieldName)
        {
            // 有命名字段，使用命名元组构造函数
            return new TupleLangValue(elements, fieldNames, position);
        }
        else
        {
            // 无命名字段，统一使用列表构造函数
            return new TupleLangValue(elements, position);
        }
    }

    /// <summary>
    /// 解析元组元素，支持可选的命名语法：name: expression 或 expression
    /// </summary>
    /// <param name="fieldName">输出参数，字段名（如果有）</param>
    /// <returns>元组元素表达式</returns>

    private LangExpression ParseTupleElementWithOptionalName(out string? fieldName)
    {
        fieldName = null;

        // 尝试预读：检查模式是否为 identifier: expression
        int savedIndex = CurrentIndex;

        // 检查当前token是否是标识符
        if (CurrentToken.Type == LangTokenType.Identifier)
        {
            string potentialFieldName = CurrentToken.Value;
            CurrentIndex++; // 移动到下一个token

            // 检查是否紧跟冒号
            if (CurrentToken.Type == LangTokenType.Colon)
            {
                // 确认是命名元组语法：name: expression
                Expect(LangTokenType.Colon);
                fieldName = potentialFieldName;

                // 解析冒号后的表达式
                return expressionParserFactory().ParseExpression();
            }
            else
            {
                // 不是命名元组语法，回滚并正常解析表达式
                CurrentIndex = savedIndex;
            }
        }

        // 普通元组元素，没有命名
        return expressionParserFactory().ParseExpression();
    }

    /// <summary>
    /// 解析字符串树，支持模板字符串
    /// 支持格式：
    /// - $"string" 简单模板字符串
    /// - $"string {placeholder}" 带占位符的模板
    /// - $"string ${expression} string" 混合模板
    /// </summary>
    /// <returns>字符串树</returns>

    private LangId ParseLambdaParameter()
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

        // 处理类型注解：identifier:type (只支持简单类型)
        var typeAnnotation = "";
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);

            // 解析简单类型注解
            if (CurrentToken.Type == LangTokenType.Identifier)
            {
                typeAnnotation = CurrentToken.Value;
                Expect(LangTokenType.Identifier);

                // 验证是否为支持的类型
                var supportedTypes = new[] { "int", "double", "string", "bool", "char", "void", "list", "dict", "any" };
                // 允许单个大写字母作为泛型类型参数（如 T, U, V）
                var isGenericTypeParameter = typeAnnotation.Length == 1 && char.IsUpper(typeAnnotation[0]);

                if (!supportedTypes.Contains(typeAnnotation) && !isGenericTypeParameter)
                {
                    throw CreateSyntaxError(
                        $"不支持的类型注解: {typeAnnotation}。支持的类型: int, double, string, bool, char, void, list, dict, any 或单个大写字母作为泛型类型参数");
                }
            }
            else
            {
                throw CreateSyntaxError($"期望类型名称，但得到 {CurrentToken.Type}");
            }
        }

        return new LangId(value, typeAnnotation, null, position: position);
    }
}
