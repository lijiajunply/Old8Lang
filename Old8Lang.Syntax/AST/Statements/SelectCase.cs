namespace Old8Lang.AST.Statement;

/// <summary>
/// Select语句中的单个case
/// </summary>
public class SelectCase
{
    /// <summary>
    /// 是否为接收操作（true: 接收, false: 发送）
    /// </summary>
    public bool IsReceive { get; }

    /// <summary>
    /// Channel表达式
    /// </summary>
    public LangExpression ChannelExpression { get; }

    /// <summary>
    /// 接收时的变量名（可选）
    /// </summary>
    public string? VariableName { get; }

    /// <summary>
    /// 发送时的值表达式（可选）
    /// </summary>
    public LangExpression? SendValueExpression { get; }

    /// <summary>
    /// case块中的语句
    /// </summary>
    public BlockStatement BlockStatement { get; }

    /// <summary>
    /// 源代码位置
    /// </summary>
    public SourcePosition Position { get; }

    /// <summary>
    /// 构造接收case：value &lt;- channel
    /// </summary>
    public SelectCase(
        LangExpression channelExpression,
        string? variableName,
        BlockStatement blockStatement,
        SourcePosition position)
    {
        IsReceive = true;
        ChannelExpression = channelExpression;
        VariableName = variableName;
        SendValueExpression = null;
        BlockStatement = blockStatement;
        Position = position;
    }

    /// <summary>
    /// 构造发送case：channel &lt;- value
    /// </summary>
    public SelectCase(
        LangExpression channelExpression,
        LangExpression sendValueExpression,
        BlockStatement blockStatement,
        SourcePosition position)
    {
        IsReceive = false;
        ChannelExpression = channelExpression;
        VariableName = null;
        SendValueExpression = sendValueExpression;
        BlockStatement = blockStatement;
        Position = position;
    }
}
