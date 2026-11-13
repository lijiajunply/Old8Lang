using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Concurrency;
using Old8Lang.Error;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

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

/// <summary>
/// Select语句，用于Channel多路选择（类似Go的select）
/// </summary>
public partial class SelectStatement(
    List<SelectCase> cases,
    BlockStatement? defaultCase,
    SourcePosition position = default) : OldStatement(position)
{

    /// <summary>
    /// 获取所有case分支
    /// </summary>
    public List<SelectCase> Cases => cases;

    /// <summary>
    /// 获取default分支（可选）
    /// </summary>
    public BlockStatement? DefaultCase => defaultCase;



    public override OldStatement this[int index]
    {
        get
        {
            // Select 语句包含所有case块和default块
            if (index >= 0 && index < cases.Count)
            {
                return cases[index].BlockStatement;
            }

            if (defaultCase is not null && index == cases.Count)
            {
                return defaultCase;
            }

            // 超出范围，返回空语句
            return new BlockStatement(new List<IOldLangTree>());
        }
    }

    public override int Count => cases.Count + (defaultCase is not null ? 1 : 0); // case数量 + default块（如果有）
}
