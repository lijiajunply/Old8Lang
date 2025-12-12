using Old8Lang.Error;
using Old8Lang.LangParser.ParserHelpers;

namespace Old8Lang.LangParser.Core;

/// <summary>
/// 基础解析器，提供所有解析器通用的基础操作
/// </summary>
public abstract class ParserBase
{
    protected readonly ParserContext Context;

    protected ParserBase(ParserContext context)
    {
        Context = context;
    }

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
                GetSourceContext(CurrentToken.Line));
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
    /// <param name="line">错误行号</param>
    /// <returns>错误位置附近的源代码上下文（最多3行）</returns>
    protected string[] GetSourceContext(int line)
    {
        if (string.IsNullOrEmpty(Context.SourceCode))
        {
            return [];
        }

        var lines = Context.SourceCode.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var contextLines = new List<string>();

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

    /// <summary>
    /// 创建语法错误
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>语法错误对象</returns>
    protected SyntaxError CreateSyntaxError(string message)
    {
        var context = GetSourceContext(CurrentToken.Line);
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
}
