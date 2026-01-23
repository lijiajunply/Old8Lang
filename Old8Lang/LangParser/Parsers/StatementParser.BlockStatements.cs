using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.LangParser.Core;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// 语句解析器 - 块语句
/// </summary>
public partial class StatementParser
{
    public BlockStatement ParseBlock()
    {
        // 处理大括号包围的块
        if (CurrentToken.Type == LangTokenType.LeftBrace)
        {
            Expect(LangTokenType.LeftBrace);
            var statements = new List<IOldLangTree>();

            try
            {
                while (CurrentToken.Type != LangTokenType.RightBrace)
                {
                    // 跳过开头的分号（空语句）
                    SkipOptionalSemicolons();

                    // 如果跳过分号后遇到右大括号，退出循环
                    if (CurrentToken.Type == LangTokenType.RightBrace)
                    {
                        break;
                    }

                    // 尝试解析语句
                    var statement = ParseStatement();

                    // 只有当语句不是空语句时才添加到列表中
                    if (!(statement is SetStatement { Id.IdName: "", Value: LangId { IdName: "" } }))
                    {
                        statements.Add(statement);
                    }

                    SkipOptionalSemicolons(); // 跳过可选的分号分隔符
                }
            }
            catch (SyntaxError)
            {
                // 直接抛出语法错误，不再包装
                throw;
            }
            catch (Exception ex)
            {
                if (ex.Message != "EndOfBlock")
                {
                    throw;
                }
            }

            Expect(LangTokenType.RightBrace);
            return new BlockStatement(statements);
        }

        // 处理单个语句 - 没有大括号的情况
        // 保存当前索引，以便在解析失败时恢复
        var savedIndex = CurrentIndex;

        try
        {
            // 解析单个语句
            var statement = ParseStatement();
            // 返回包含这个语句的BlockStatement
            return new BlockStatement([statement]);
        }
        catch (SyntaxError)
        {
            // 如果解析失败，恢复索引并重新抛出错误
            CurrentIndex = savedIndex;
            throw;
        }
    }

    /// <summary>
    /// 解析try语句
    /// </summary>
    /// <returns>TryStatement对象</returns>

    public TryStatement ParseTryStatement()
    {
        Expect(LangTokenType.Try);

        // 解析try块
        var tryBlock = ParseBlock();

        // 解析catch块列表
        var catchBlocks = new List<(string? exceptionType, LangId? exceptionVar, LangExpression? filter, BlockStatement catchBlock)>();

        // 循环解析catch块
        while (CurrentToken.Type == LangTokenType.Catch)
        {
            Expect(LangTokenType.Catch);

            string? exceptionType = null;
            LangId? exceptionVar = null;

            // 检查是否有异常类型和变量
            if (CurrentToken.Type == LangTokenType.LeftParen)
            {
                Expect(LangTokenType.LeftParen);

                // 解析异常类型（如果有）
                if (CurrentToken.Type == LangTokenType.Identifier)
                {
                    exceptionType = CurrentToken.Value;
                    CurrentIndex++;

                    // 解析异常变量（如果有）
                    if (CurrentToken.Type == LangTokenType.Identifier)
                    {
                        exceptionVar = new LangId(CurrentToken.Value, position: CreateSourcePosition(CurrentToken));
                        CurrentIndex++;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(exceptionType) && !exceptionType.Contains("Exception") && 
                            !(exceptionType.Length > 0 && char.IsUpper(exceptionType[0])))
                        {
                            exceptionVar = new LangId(exceptionType, position: CreateSourcePosition(CurrentToken));
                            exceptionType = null;
                        }
                    }
                }

                Expect(LangTokenType.RightParen);
            }

            // 解析可选的 where 子句 (Exception Filter)
            LangExpression? filter = null;
            if (CurrentToken.Type == LangTokenType.Where)
            {
                Expect(LangTokenType.Where);
                filter = expressionParser.ParseExpression();
            }

            // 解析catch块
            var catchBlock = ParseBlock();
            catchBlocks.Add((exceptionType, exceptionVar, filter, catchBlock));
        }

        // 解析finally块（可选）
        BlockStatement? finallyBlock = null;
        if (CurrentToken.Type == LangTokenType.Finally)
        {
            Expect(LangTokenType.Finally);
            // 直接解析finally块，不使用ParseBlock，避免finally被视为单独的语句
            var statements = new List<IOldLangTree>();
            if (CurrentToken.Type == LangTokenType.LeftBrace)
            {
                CurrentIndex++;
                while (CurrentToken.Type != LangTokenType.RightBrace)
                {
                    statements.Add(ParseStatement());
                }

                CurrentIndex++;
            }
            else
            {
                statements.Add(ParseStatement());
            }

            finallyBlock = new BlockStatement(statements);
        }

        // 创建TryStatement对象
        return new TryStatement(tryBlock, catchBlocks, finallyBlock, CreateSourcePosition(CurrentToken));
    }

    /// <summary>
    /// 跳过泛型类型注解，返回泛型结束后的token索引偏移量
    /// 例如：对于 "List<T>"，从 < 开始跳过，返回 > 后的token位置
    /// 支持嵌套泛型：List<List<int>>
    /// </summary>
    /// <param name="startOffset">起始偏移量（相对于当前位置）</param>
    /// <returns>泛型结束后的token索引偏移量</returns>

    private int SkipGenericTypeAnnotation(int startOffset)
    {
        var offset = startOffset;
        var depth = 0;
        var started = false;

        while (offset < 100) // 防止无限循环
        {
            var token = Peek(offset);

            if (token.Type == LangTokenType.EndOfFile)
            {
                break;
            }

            if (token.Type == LangTokenType.LessThan)
            {
                depth++;
                started = true;
            }
            else if (token.Type == LangTokenType.GreaterThan)
            {
                depth--;
                if (depth == 0 && started)
                {
                    // 找到匹配的右尖括号，返回下一个位置
                    return offset + 1;
                }
            }

            offset++;
        }

        // 如果没找到匹配的右尖括号，返回当前偏移量
        return offset;
    }

    /// <summary>
    /// 解析并消费泛型类型注解，返回类型字符串（不包括 < 和 >）
    /// 例如：对于 "<int>"，返回 "int"
    /// 支持嵌套泛型：<List<int>> 返回 "List<int>"
    /// </summary>
    /// <returns>泛型类型字符串</returns>

    private string SkipAndParseGenericTypeAnnotation()
    {
        var result = "";
        Expect(LangTokenType.LessThan);
        var depth = 1;

        while (depth > 0)
        {
            if (CurrentToken.Type == LangTokenType.EndOfFile)
            {
                throw CreateSyntaxError("意外的文件结束符，期望 '>'");
            }

            if (CurrentToken.Type == LangTokenType.LessThan)
            {
                result += "<";
                depth++;
                CurrentIndex++;
            }
            else if (CurrentToken.Type == LangTokenType.GreaterThan)
            {
                depth--;
                if (depth > 0)
                {
                    result += ">";
                }

                CurrentIndex++;
            }
            else if (CurrentToken.Type == LangTokenType.Comma)
            {
                result += ", ";
                CurrentIndex++;
            }
            else if (CurrentToken.Type == LangTokenType.Question)
            {
                result += "?";
                CurrentIndex++;
            }
            else
            {
                result += CurrentToken.Value;
                CurrentIndex++;
            }
        }

        return result;
    }


}
