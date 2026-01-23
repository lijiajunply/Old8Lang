using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.LangParser.Core;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// 语句解析器 - 赋值语句
/// </summary>
public partial class StatementParser
{
    public SetStatement ParseSet()
    {
        // 特殊处理带有类型注解的赋值语句：a:int <- value 或 a:int (只有类型注解)
        if (CurrentToken.Type == LangTokenType.Identifier && Peek().Type == LangTokenType.Colon)
        {
            // 带有类型注解的赋值语句
            var id = functionParser.ParseTypedIdentifier(false); // 使用 ParseTypedIdentifier 处理类型注解

            // 检查是否有赋值符号
            if (CurrentToken.Type == LangTokenType.Assignment)
            {
                Expect(LangTokenType.Assignment);
                var expr = expressionParser.ParseExpression();
                return new SetStatement(id, expr, id.Position);
            }
            else
            {
                // 只有类型注解，没有赋值，创建赋值 Null 的语句
                var nullExpr = new NullLangValue(id.Position);
                return new SetStatement(id, nullExpr, id.Position);
            }
        }

        // 解析左值表达式 - 只能是标识符、this或复杂左值（如a.b或a[b]），不能是解构模式
        var leftExpr = primaryParser.ParsePrimary();

        // 处理点访问和索引访问等复杂左值表达式
        leftExpr = expressionParser.ParseDotExpr(leftExpr);

        // 检查是否有类型注解
        var assumptionType = "";
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);

            // 使用 FunctionParser 的 ParseComplexTypeAnnotation 处理复杂类型注解
            assumptionType = functionParser.ParseComplexTypeAnnotation();
        }

        // 检查是否有赋值符号
        // 如果没有赋值符号，说明这是一个只有类型注解的字段声明（会初始化为 Null）
        if (CurrentToken.Type != LangTokenType.Assignment)
        {
            // 只有类型注解，没有赋值，创建一个赋值 Null 的语句
            if (leftExpr is LangId leftLangId)
            {
                var nullExpr = new NullLangValue(leftLangId.Position);
                return new SetStatement(
                    new LangId(leftLangId.IdName, assumptionType, null, position: leftLangId.Position), nullExpr,
                    leftLangId.Position);
            }
            else
            {
                throw CreateSyntaxError("只有类型注解没有赋值时，左值必须是标识符");
            }
        }

        Expect(LangTokenType.Assignment);
        var expression = expressionParser.ParseExpression();

        // 处理不同类型的左值表达式
        if (leftExpr is LangId langId)
        {
            // 普通标识符赋值：identifier <- value
            return new SetStatement(new LangId(langId.IdName, assumptionType, position: leftExpr.Position), expression,
                leftExpr.Position);
        }

        // 复杂左值表达式赋值：a[b] <- value 或 a.a <- value 或 this.a <- value
        return new SetStatement(leftExpr, expression, leftExpr.Position);
    }

    /// <summary>
    /// 解析数组解构赋值：[a, b] &lt;- [1, 2]
    /// </summary>

    private SetStatement ParseArrayDestructuring()
    {
        Expect(LangTokenType.LeftBracket);
        var position = CreateSourcePosition(CurrentToken);

        // 解析解构的标识符列表
        var identifiers = new List<string>();
        while (CurrentToken.Type != LangTokenType.RightBracket)
        {
            // 允许跳过空元素，如 [a, , b]
            if (CurrentToken.Type == LangTokenType.Comma)
            {
                identifiers.Add("null");
                Expect(LangTokenType.Comma);
                continue;
            }

            // 解析标识符，只能是简单标识符，不能是复杂表达式
            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError("数组解构赋值中的标识符必须是简单标识符");
            }

            var ident = CurrentToken.Value;
            identifiers.Add(ident);
            Expect(LangTokenType.Identifier);

            // 检查是否还有更多元素
            if (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);
            }
        }

        Expect(LangTokenType.RightBracket);

        // 检查是否有类型注解
        var assumptionType = "";
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);
            assumptionType = CurrentToken.Value;
            Expect(LangTokenType.Identifier);

            // 检查是否为可空类型（例如 "int?"）
            if (CurrentToken.Type == LangTokenType.Question)
            {
                assumptionType += "?";
                Expect(LangTokenType.Question);
            }
        }

        Expect(LangTokenType.Assignment);
        var expression = expressionParser.ParseExpression();

        // 创建一个特殊的SetStatement，其中Id是一个Operation，表示数组解构
        var destructExpr = new Operation(
            new LangId("array_destruct"),
            LangTokenType.LeftBracket,
            new LangId(string.Join(",", identifiers)),
            position);

        return new SetStatement(destructExpr, expression, position);
    }

    /// <summary>
    /// 解析对象解构赋值：{name, age} &lt;- person
    /// </summary>

    private SetStatement ParseObjectDestructuring()
    {
        Expect(LangTokenType.LeftBrace);
        var position = CreateSourcePosition(CurrentToken);

        // 解析解构的属性列表
        var properties = new List<string>();
        while (CurrentToken.Type != LangTokenType.RightBrace)
        {
            // 解析属性名
            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError("对象解构赋值中的属性名必须是简单标识符");
            }

            var propertyName = CurrentToken.Value;
            Expect(LangTokenType.Identifier);

            // 检查是否有别名，如 {name: newName}
            var aliasName = propertyName;
            if (CurrentToken.Type == LangTokenType.Colon)
            {
                Expect(LangTokenType.Colon);
                if (CurrentToken.Type != LangTokenType.Identifier)
                {
                    throw CreateSyntaxError("对象解构赋值中的别名必须是简单标识符");
                }

                aliasName = CurrentToken.Value;
                Expect(LangTokenType.Identifier);

                // 格式：propertyName:aliasName
                properties.Add($"{propertyName}:{aliasName}");
            }
            else
            {
                // 只有属性名，没有别名
                properties.Add(propertyName);
            }

            // 检查是否还有更多属性
            if (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);
            }
        }

        Expect(LangTokenType.RightBrace);

        // 检查是否有类型注解
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);
            Expect(LangTokenType.Identifier);
        }

        Expect(LangTokenType.Assignment);
        var expression = expressionParser.ParseExpression();

        // 创建一个特殊的SetStatement，其中Id是一个Operation，表示对象解构
        var destructExpr = new Operation(
            new LangId("object_destruct"),
            LangTokenType.LeftBrace,
            new LangId(string.Join(",", properties)),
            position);

        return new SetStatement(destructExpr, expression, position);
    }

    // ifStatement = "if" expression block ( "elif" expression block )* ( "else" block )? ;

    public SetStatement ParsePlusPlus()
    {
        var identifier = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.PlusPlus);
        return new SetStatement(new LangId(identifier),
            new Operation(new LangId(identifier), LangTokenType.Plus, new IntLangValue(1)));
    }

    /// <summary>
    /// minusMinus = identifier "--"
    /// </summary>
    /// <returns>i--运算</returns>

    public SetStatement ParseMinusMinus()
    {
        var identifier = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.MinusMinus);
        return new SetStatement(new LangId(identifier),
            new Operation(new LangId(identifier), LangTokenType.Minus, new IntLangValue(1)));
    }

    /// <summary>
    /// block = "{" statement* "}"
    ///        | statement
    /// </summary>
    /// <returns>块语句</returns>

}
