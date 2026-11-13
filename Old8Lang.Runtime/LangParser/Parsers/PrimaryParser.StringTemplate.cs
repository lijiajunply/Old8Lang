using Old8Lang.AST;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// Primary 表达式解析器 - 字符串模板解析
/// </summary>
public partial class PrimaryParser
{
    public LangExpression ParseStringTemplate()
    {
        // 检查当前token是否是Dollar（用于字符串插值）
        if (CurrentToken.Type == LangTokenType.Dollar)
        {
            var dollarToken = CurrentToken;
            var position = new SourcePosition(dollarToken.Line, dollarToken.Column, tokenValue: dollarToken.Value);

            // 跳过$符号
            Expect(LangTokenType.Dollar);

            // 处理$"string" 格式（字符串插值）
            if (CurrentToken.Type == LangTokenType.String)
            {
                var stringValue = CurrentToken.Value;
                Expect(LangTokenType.String);

                // 完整的字符串模板解析
                var parts = new List<LangExpression>();
                var i = 0;
                var len = stringValue.Length;

                while (i < len)
                {
                    var c = stringValue[i];

                    if (c == '{' && i + 1 < len)
                    {
                        var next = stringValue[i + 1];

                        if (next == '{')
                        {
                            // 转义的 {{，添加一个 {
                            parts.Add(new StringLangValue("{", position));
                            i += 2;
                        }
                        else
                        {
                            // 普通的 {，开始解析表达式
                            i += 1;
                            var exprStart = i;
                            var braceCount = 1;
                            var inString = false;
                            var stringChar = '\0'; // 记录当前字符串的引号类型（单引号或双引号）

                            // 查找匹配的 }
                            var foundMatchingBrace = false;
                            while (i < len && braceCount > 0)
                            {
                                c = stringValue[i];

                                // 处理字符串中的引号
                                if (!inString && (c == '"' || c == '\''))
                                {
                                    inString = true;
                                    stringChar = c;
                                    i++;
                                    continue;
                                }

                                if (inString)
                                {
                                    // 在字符串中，查找匹配的结束引号
                                    if (c == stringChar)
                                    {
                                        // 检查是否是转义引号
                                        var backslashCount = 0;
                                        var j = i - 1;
                                        while (j >= 0 && stringValue[j] == '\\')
                                        {
                                            backslashCount++;
                                            j--;
                                        }

                                        var isEscaped = backslashCount % 2 == 1;

                                        if (!isEscaped)
                                        {
                                            inString = false;
                                            stringChar = '\0';
                                        }
                                    }

                                    i++;
                                    continue;
                                }

                                // 不在字符串中，才处理大括号
                                if (c == '{')
                                {
                                    braceCount++;
                                }
                                else if (c == '}')
                                {
                                    braceCount--;
                                    if (braceCount == 0)
                                    {
                                        foundMatchingBrace = true;
                                        break;
                                    }
                                }

                                i++;
                            }

                            if (foundMatchingBrace)
                            {
                                // 提取表达式字符串
                                var exprStr = stringValue.Substring(exprStart, i - exprStart).Trim();

                                // 检查表达式是否为空
                                if (string.IsNullOrWhiteSpace(exprStr))
                                {
                                    throw CreateSyntaxError("语法错误：字符串模板的花括号内不能为空。建议：在花括号内提供有效的表达式，如 ${variableName}。");
                                }

                                // 完整的表达式解析：支持所有表达式类型，包括点操作符
                                // 将表达式包装成括号表达式，然后作为赋值语句的右值
                                // 使用括号可以避免三元运算符中的 if 被误认为 if 语句
                                var wrappedExpr = $"__temp <- ({exprStr})";

                                // 将表达式字符串转换为Token流
                                var exprTokens = LangTokenizer.Tokenize(wrappedExpr);

                                // 创建一个新的LangParser实例来解析这个表达式
                                var exprParser = new LangParser(exprTokens, wrappedExpr,
                                    $"{Context.FileName}:template");

                                // 解析完整表达式
                                var programBlock = exprParser.ParseProgram();
                                if (programBlock.Count > 0 && programBlock[0] is SetStatement setStmt)
                                {
                                    parts.Add(setStmt.Value);
                                }
                                else
                                {
                                    throw CreateSyntaxError("无法解析字符串模板中的表达式");
                                }

                                i++;
                            }
                            else
                            {
                                // 未找到匹配的 }，抛出语法错误
                                throw CreateSyntaxError("字符串模板中缺少匹配的右大括号 '}'");
                            }
                        }
                    }
                    else if (c == '}')
                    {
                        if (i + 1 < len && stringValue[i + 1] == '}')
                        {
                            // 转义的 }}，添加一个 }
                            parts.Add(new StringLangValue("}", position));
                            i += 2;
                        }
                        else
                        {
                            // 普通的 }，直接添加
                            parts.Add(new StringLangValue("}", position));
                            i++;
                        }
                    }
                    else
                    {
                        // 普通字符，添加到结果中
                        var start = i;
                        while (i < len && stringValue[i] != '{' && stringValue[i] != '}')
                        {
                            i++;
                        }

                        var text = stringValue.Substring(start, i - start);
                        if (!string.IsNullOrEmpty(text))
                        {
                            parts.Add(new StringLangValue(text, position));
                        }
                    }
                }

                return new StringTemplateValue(parts, position);
            }
        }

        // 如果不是字符串插值，返回普通表达式
        return ParsePrimary();
    }
}
