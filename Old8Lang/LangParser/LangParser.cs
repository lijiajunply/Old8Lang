using Old8Lang.AST;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.LangParser.Core;
using Old8Lang.LangParser.Parsers;

namespace Old8Lang.LangParser;

/// <summary>
/// LangParser 门面类，保持向后兼容性
/// 内部委托给专门的解析器类
/// </summary>
public class LangParser
{
    private readonly ParserContext Context;
    private readonly StatementParser StatementParser;
    private readonly ExpressionParser ExpressionParser;
    private readonly PrimaryParser PrimaryParser;
    private readonly FunctionParser FunctionParser;
    private readonly ClassParser ClassParser;

    /// <summary>
    /// 构造函数，初始化所有解析器
    /// </summary>
    /// <param name="tokens">令牌列表</param>
    /// <param name="sourceCode">源代码</param>
    /// <param name="fileName">文件名</param>
    public LangParser(List<LangToken> tokens, string? sourceCode = null, string? fileName = null)
    {
        // 1. 创建共享上下文
        Context = new ParserContext(tokens, sourceCode, fileName);

        // 2. 创建各个解析器（解决循环依赖）

        // FunctionParser 需要 StatementParser（延迟创建）和 ExpressionParser（延迟创建）
        FunctionParser = new FunctionParser(
            Context,
            () => StatementParser!,
            () => ExpressionParser!);

        // PrimaryParser 需要 StatementParser（延迟创建）、ExpressionParser（延迟创建）和 FunctionParser
        PrimaryParser = new PrimaryParser(
            Context,
            () => StatementParser!,
            () => ExpressionParser!,
            FunctionParser);

        // ExpressionParser 需要 PrimaryParser
        ExpressionParser = new ExpressionParser(Context, PrimaryParser);

        // ClassParser 需要 StatementParser（延迟创建）
        ClassParser = new ClassParser(
            Context,
            () => StatementParser!);

        // StatementParser 需要 ExpressionParser, FunctionParser, ClassParser, PrimaryParser
        StatementParser = new StatementParser(
            Context,
            ExpressionParser,
            FunctionParser,
            ClassParser,
            PrimaryParser);
    }

    /// <summary>
    /// 解析程序（入口方法）
    /// root = statement* ;
    /// </summary>
    public BlockStatement ParseProgram()
    {
        var statements = new List<IOldLangTree>();
        var lastStatementLine = -1; // 跟踪上一个语句的行号

        try
        {
            while (Context.CurrentIndex < Context.Tokens.Count)
            {
                // 记录当前语句开始的行号
                var currentStatementLine = Context.CurrentToken.Line;

                // 如果当前语句与上一个语句在同一行，抛出错误
                if (lastStatementLine != -1 && currentStatementLine == lastStatementLine)
                {
                    throw new SyntaxError(
                        Context.CurrentToken.Value,
                        Context.CurrentToken.Line,
                        Context.CurrentToken.Column,
                        Context.FileName,
                        "语法错误：同一行上不能有多个语句。建议：在语句之间添加换行符，或使用分号分隔（如果语言支持）。",
                        GetSourceContext(Context.CurrentToken.Line));
                }

                statements.Add(StatementParser.ParseStatement());

                // 更新最后一个语句的行号
                lastStatementLine = currentStatementLine;
            }

            return new BlockStatement(statements);
        }
        catch (SyntaxError)
        {
            // 直接返回原始异常，不再重新包装
            throw;
        }
        catch (Exception ex)
        {
            // 处理其他类型的异常，添加上下文信息
            var currentToken = Context.CurrentToken;

            // 检查 currentToken 是否为无效
            var tokenValue = currentToken.Type == LangTokenType.EndOfFile ? "<unknown>" : currentToken.Value;
            var line = currentToken.Line;
            var column = currentToken.Column;

            string[] context;
            try
            {
                context = GetSourceContext(line);
            }
            catch
            {
                // 如果获取上下文失败，使用空数组
                context = [];
            }

            if (ex is Old8Exception old8Ex)
            {
                // 如果已经是 Old8Exception，添加上下文信息
                throw new SyntaxError(
                    tokenValue,
                    line,
                    column,
                    Context.FileName,
                    $"解析错误：{old8Ex.Message}",
                    context);
            }

            // 其他类型的异常，转换为 SyntaxError
            throw new SyntaxError(
                tokenValue,
                line,
                column,
                Context.FileName,
                $"解析时出现代码错误：{ex.Message}",
                context);
        }
    }

    /// <summary>
    /// 获取错误位置附近的源代码上下文
    /// </summary>
    /// <param name="line">错误行号</param>
    /// <returns>错误位置附近的源代码上下文（最多3行）</returns>
    private string[] GetSourceContext(int line)
    {
        // 使用缓存的分割结果
        var lines = Context.SourceLines;

        if (lines.Length == 0)
        {
            return Array.Empty<string>();
        }

        var contextLines = new List<string>(4); // 预分配容量

        // 获取错误行前后的上下文，最多显示3行上下文
        // 确保line至少为0，避免负数行号导致的问题
        var safeLine = Math.Max(0, line);
        var startLine = Math.Max(0, safeLine - 2);
        var endLine = Math.Min(lines.Length - 1, safeLine + 1);

        for (var i = startLine; i <= endLine; i++)
        {
            contextLines.Add(lines[i]);
        }

        return contextLines.ToArray();
    }
}