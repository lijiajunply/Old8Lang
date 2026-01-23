using Old8Lang.AST;
using Old8Lang.AST.Expression;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// Primary 表达式解析器 - 泛型解析
/// </summary>
public partial class PrimaryParser
{
    private LangExpression ParseGenericInstantiation()
    {
        // 解析基础表达式（类名或函数名）
        var baseExpression = ParseIdentifier();
        var position = CreateSourcePosition(CurrentToken);

        // 解析类型参数列表（支持嵌套泛型）
        var typeArgumentsString = ParseGenericTypeArguments();

        // 拆分类型参数（处理逗号分隔的多个类型参数）
        // 注意：需要小心处理嵌套泛型，比如 "List<int>, Dict<string, int>"
        var typeArguments = SplitTypeArguments(typeArgumentsString);

        // 检查是否有调用参数
        if (CurrentToken.Type == LangTokenType.LeftParen)
        {
            // 有调用参数：Box<int>(...) 或 map<string>(...)
            Expect(LangTokenType.LeftParen);
            var callArguments = new List<LangExpression>();

            while (CurrentToken.Type != LangTokenType.RightParen)
            {
                if (CurrentToken.Type == LangTokenType.EndOfFile)
                {
                    throw CreateSyntaxError("意外的文件结束符，期望 ')'");
                }

                var arg = expressionParserFactory().ParseExpression();
                callArguments.Add(arg);

                if (CurrentToken.Type == LangTokenType.Comma)
                {
                    Expect(LangTokenType.Comma);
                    continue;
                }

                break;
            }

            Expect(LangTokenType.RightParen);

            // 返回泛型函数调用或类实例化
            return new GenericInstanceExpression(baseExpression, typeArguments, callArguments, position);
        }
        else
        {
            // 没有调用参数：Box<int> (只是类型引用，不调用构造)
            return new GenericInstanceExpression(baseExpression, typeArguments, position);
        }
    }

    /// <summary>
    /// 启发式判断 &lt; 是否为泛型的开始（而非比较运算符）
    ///
    /// 泛型特征：identifier &lt; TypeName [, TypeName]* > (
    ///
    /// 比较运算符特征：identifier &lt; number/identifier/expression
    /// </summary>
    /// <returns>如果可能是泛型返回 true</returns>

    private bool IsLikelyGenericInstantiation()
    {
        // 保存当前位置
        var savedIndex = CurrentIndex;

        try
        {
            // CurrentToken 是标识符（如 Box），Peek() 是 <
            // 记录外层标识符（调用者），用于上下文判断
            string? outerIdentifier = null;
            if (CurrentToken.Type == LangTokenType.Identifier)
            {
                outerIdentifier = CurrentToken.Value;
                CurrentIndex++;
            }

            // 现在 CurrentToken 应该是 <
            if (CurrentToken.Type != LangTokenType.LessThan)
                return false;

            CurrentIndex++;

            // 检查 < 后面的 token
            // 泛型：应该是类型名（标识符，且通常首字母大写或已知类型）
            // 比较：可能是数字、小写标识符、运算符等

            if (CurrentToken.Type != LangTokenType.Identifier)
                return false;

            // 记住内层标识符（类型参数）
            var innerIdentifier = CurrentToken.Value;
            CurrentIndex++;

            // 检查标识符后面的 token
            var nextTokenType = CurrentToken.Type;

            // 检查外层标识符是否是明确的类型名
            var outerIsTypeName = outerIdentifier is not null &&
                                  (char.IsUpper(outerIdentifier[0]) || IsBuiltInTypeName(outerIdentifier));

            // 强泛型证据：这些模式只能是泛型，不可能是比较运算符
            // 1. Type> - 泛型结束符
            // 2. Type, - 多个类型参数
            // 3. Type? - 可空类型
            if (nextTokenType == LangTokenType.GreaterThan ||
                nextTokenType == LangTokenType.Comma ||
                nextTokenType == LangTokenType.Question)
            {
                // 如果外层是明确的类型名（如 List, Box, int），那么即使内层是小写也是泛型
                // 例如：List<a> - List是类型名，a是自定义类型参数
                if (outerIsTypeName)
                {
                    return true;
                }

                // 如果外层不是类型名，再检查内层
                // 例如：int> 或 Box> 肯定是泛型，但 a < b > c 可能是比较链
                if (char.IsUpper(innerIdentifier[0]) || IsBuiltInTypeName(innerIdentifier))
                {
                    return true;
                }

                // 两者都不是类型名，保守返回 false
                return false;
            }

            // 强比较证据：这些模式只能是比较运算符
            if (nextTokenType == LangTokenType.Plus ||
                nextTokenType == LangTokenType.Minus ||
                nextTokenType == LangTokenType.Star ||
                nextTokenType == LangTokenType.Slash ||
                nextTokenType == LangTokenType.And ||
                nextTokenType == LangTokenType.Or ||
                nextTokenType == LangTokenType.RightParen ||
                nextTokenType == LangTokenType.LeftParen ||
                nextTokenType == LangTokenType.EndOfFile)
            {
                return false;
            }

            // 嵌套泛型的情况：Type< - 需要进一步检查
            if (nextTokenType == LangTokenType.LessThan)
            {
                // 如果外层是类型名（如 List<List<a>>），那么即使内层是小写也是泛型
                if (outerIsTypeName)
                {
                    return true;
                }

                // 只有类型名（首字母大写或内置类型）才可能是嵌套泛型
                if (char.IsUpper(innerIdentifier[0]) || IsBuiltInTypeName(innerIdentifier))
                {
                    return true;
                }

                // 小写变量名 + < 很可能是链式比较：a < b < c
                return false;
            }

            // 默认：只有明确的类型名才当作泛型
            // 如果外层是类型名，那么即使内层是小写也是泛型
            if (outerIsTypeName)
            {
                return true;
            }

            if (char.IsUpper(innerIdentifier[0]) || IsBuiltInTypeName(innerIdentifier))
            {
                return true;
            }

            // 所有其他情况：保守策略，当作比较运算符
            return false;
        }
        finally
        {
            // 恢复位置
            CurrentIndex = savedIndex;
        }
    }

    /// <summary>
    /// 判断是否为内置类型名称
    /// </summary>

    private bool IsBuiltInTypeName(string name)
    {
        return name switch
        {
            "int" or "string" or "double" or "bool" or "char" or
                "long" or "float" or "byte" or "short" or "decimal" or
                "void" or "object" or "dynamic" => true,
            _ => false
        };
    }

    /// <summary>
    /// 解析泛型类型参数，支持嵌套泛型
    /// 语法：&lt;int>, &lt;T>, &lt;List&lt;int>>, &lt;List&lt;List&lt;string>>>
    /// </summary>
    /// <returns>类型参数字符串（不包括外层 &lt; 和 >）</returns>

    private string ParseGenericTypeArguments()
    {
        var result = "";
        Expect(LangTokenType.LessThan);

        while (CurrentToken.Type != LangTokenType.GreaterThan)
        {
            if (CurrentToken.Type == LangTokenType.EndOfFile)
            {
                throw CreateSyntaxError("意外的文件结束符，期望 '>'");
            }

            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError($"期望类型参数名称，但得到 {CurrentToken.Type}");
            }

            result += CurrentToken.Value;
            Expect(LangTokenType.Identifier);

            // 递归处理嵌套泛型
            if (CurrentToken.Type == LangTokenType.LessThan)
            {
                result += "<" + ParseGenericTypeArguments() + ">";
            }

            // 可空类型标记
            if (CurrentToken.Type == LangTokenType.Question)
            {
                result += "?";
                Expect(LangTokenType.Question);
            }

            if (CurrentToken.Type == LangTokenType.Comma)
            {
                result += ", ";
                Expect(LangTokenType.Comma);
                continue;
            }

            break;
        }

        Expect(LangTokenType.GreaterThan);
        return result;
    }

    /// <summary>
    /// 拆分类型参数字符串为独立的类型参数列表
    /// 例如: "string, int" -> ["string", "int"]
    ///       "List<int>, Dict<string, int>" -> ["List<int>", "Dict<string, int>"]
    /// </summary>
    /// <param name="typeArgumentsString">类型参数字符串</param>
    /// <returns>类型参数列表</returns>

    private List<string> SplitTypeArguments(string typeArgumentsString)
    {
        var result = new List<string>();
        var current = "";
        var depth = 0; // 嵌套深度（用于处理嵌套泛型）

        for (int i = 0; i < typeArgumentsString.Length; i++)
        {
            char c = typeArgumentsString[i];

            if (c == '<')
            {
                depth++;
                current += c;
            }
            else if (c == '>')
            {
                depth--;
                current += c;
            }
            else if (c == ',' && depth == 0)
            {
                // 顶层逗号，分隔类型参数
                result.Add(current.Trim());
                current = "";
            }
            else
            {
                current += c;
            }
        }

        // 添加最后一个参数
        if (!string.IsNullOrWhiteSpace(current))
        {
            result.Add(current.Trim());
        }

        return result;
    }


}
