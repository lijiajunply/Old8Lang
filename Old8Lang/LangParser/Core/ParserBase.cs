using Old8Lang.AST;
using Old8Lang.Error;
using Old8Lang.LangParser.ParserHelpers;

namespace Old8Lang.LangParser.Core;

/// <summary>
/// 基础解析器，提供所有解析器通用的基础操作
/// </summary>
public abstract class ParserBase(ParserContext context)
{
    protected readonly ParserContext Context = context;

    // 快捷访问属性
    protected LangToken CurrentToken => Context.CurrentToken;

    protected int CurrentIndex
    {
        get => Context.CurrentIndex;
        set => Context.CurrentIndex = value;
    }

    protected List<LangToken> Tokens => Context.Tokens;

    /// <summary>
    /// 期望特定 Token 类型
    /// </summary>
    /// <param name="type">期望的 Token 类型</param>
    /// <exception cref="SyntaxError">当实际 Token 类型与期望不符时抛出</exception>
    protected void Expect(LangTokenType type)
    {
        if (CurrentToken.Type == type)
        {
            CurrentIndex++;
        }
        else
        {
            var actualType = CurrentToken.Type;
            var actualValue = CurrentToken.Value;

            var detailedMessage = ExpectHelper.GetDetailedMessage(type, actualType, actualValue);
            var suggestion = ExpectHelper.GetSuggestion(type);

            // 抛出带有上下文的错误
            throw new SyntaxError(
                CurrentToken.Value,
                CurrentToken.Line,
                CurrentToken.Column,
                Context.FileName,
                detailedMessage + " " + suggestion,
                GetSourceContextWindow(CurrentToken.Line));
        }
    }

    /// <summary>
    /// 查看后续 Token
    /// </summary>
    /// <param name="offset">偏移量（默认为1）</param>
    /// <returns>后续令牌</returns>
    protected LangToken Peek(int offset = 1) => Context.Peek(offset);

    /// <summary>
    /// 获取错误位置附近的源代码上下文
    /// </summary>
    /// <param name="line">错误行号（从1开始）</param>
    /// <returns>错误位置附近的源代码上下文（最多3行）</returns>
    private string[] GetSourceContext(int line)
    {
        return GetSourceContextWindow(line).Lines;
    }

    /// <summary>
    /// 获取错误位置附近的源代码上下文及其起始行号
    /// </summary>
    /// <param name="line">错误行号（从1开始）</param>
    /// <returns>上下文窗口（行数组 + 首行真实行号）</returns>
    private SourceContextWindow GetSourceContextWindow(int line)
    {
        // 使用缓存的分割结果
        var lines = Context.SourceLines;

        if (lines.Length == 0)
        {
            return SourceContextWindow.Empty;
        }

        var contextLines = new List<string>(4); // 预分配容量

        // 获取错误行前后的上下文，最多显示3行
        // line 为 1 起始行号，先换算成 0 起始下标
        var zeroBasedLine = Math.Max(0, line - 1);
        var startLine = Math.Max(0, zeroBasedLine - 1);
        var endLine = Math.Min(lines.Length - 1, zeroBasedLine + 1);

        for (var i = startLine; i <= endLine; i++)
        {
            contextLines.Add(lines[i]);
        }

        return new SourceContextWindow(contextLines.ToArray(), startLine + 1);
    }

    /// <summary>
    /// 创建语法错误
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>语法错误对象</returns>
    protected SyntaxError CreateSyntaxError(string message)
    {
        var context = GetSourceContextWindow(CurrentToken.Line);
        return new SyntaxError(
            CurrentToken.Value,
            CurrentToken.Line,
            CurrentToken.Column,
            Context.FileName,
            message,
            context);
    }

    /// <summary>
    /// 创建源代码位置信息
    /// </summary>
    /// <param name="token">令牌</param>
    /// <returns>位置信息对象</returns>
    protected SourcePosition CreateSourcePosition(LangToken token)
    {
        return new SourcePosition(
            token.Line,
            token.Column,
            Context.FileName,
            token.Value);
    }

    /// <summary>
    /// 跳过可选的分号分隔符（支持连续多个分号）
    /// </summary>
    protected void SkipOptionalSemicolons()
    {
        while (CurrentToken.Type == LangTokenType.Semicolon)
        {
            CurrentIndex++;
        }
    }

    /// <summary>
    /// 收集当前位置之前的文档注释
    /// 向前查找连续的文档注释 Token，并将它们合并后解析为结构化文档注释
    /// </summary>
    /// <returns>结构化的文档注释信息，如果没有则返回 null</returns>
    protected DocCommentInfo? CollectPrecedingDocComments()
    {
        var docCommentTokens = new List<LangToken>();

        // 向前查找连续的文档注释
        var searchIndex = CurrentIndex - 1;
        while (searchIndex >= 0)
        {
            var token = Tokens[searchIndex];

            // 如果找到文档注释，添加到列表
            if (token.Type == LangTokenType.DocComment)
            {
                docCommentTokens.Insert(0, token);
                searchIndex--;
            }
            // 如果遇到非文档注释的 token，停止搜索
            else
            {
                break;
            }
        }

        // 如果没有找到文档注释，返回 null
        if (docCommentTokens.Count == 0)
        {
            return null;
        }

        // 将所有文档注释合并为一个字符串
        var docCommentLines = docCommentTokens.Select(t => t.Value).ToArray();
        var rawComment = string.Join("\n", docCommentLines);

        // 使用 DocCommentParser 解析为结构化文档注释
        return DocCommentParser.Parse(rawComment);
    }
}
