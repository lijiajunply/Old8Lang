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

    /// <summary>
    /// 标准模式执行（非生成器）
    /// </summary>
    internal void RunStandard(VariateManager manager)
    {
        // 使用轮询策略实现多路选择
        while (true)
        {
            // 检查每个case
            bool allCasesClosed = true;

            foreach (var selectCase in cases)
            {
                if (selectCase.IsReceive)
                {
                    // 接收case：尝试非阻塞接收
                    int channelId;

                    // 检查 ChannelExpression 是否为 ChannelReceive 函数调用
                    if (selectCase.ChannelExpression is Instance
                        {
                            Id.IdName: "ChannelReceive", Ids.Count: > 0
                        } funcCall)
                    {
                        // 提取实际的 channel ID 参数
                        var channelIdValue = funcCall.Ids[0].Accept(new InterpreterVisitor(manager));
                        if (channelIdValue is not IntLangValue intVal)
                        {
                            continue;
                        }

                        channelId = intVal.Value;
                    }
                    else
                    {
                        // 直接执行 ChannelExpression 获取 channel ID
                        var channelIdValue = selectCase.ChannelExpression.Accept(new InterpreterVisitor(manager));
                        if (channelIdValue is not IntLangValue intVal)
                        {
                            continue;
                        }

                        channelId = intVal.Value;
                    }

                    var receiveResult = ResourceManager.TryReceiveChannel(channelId, 0);

                    // 检测 Channel 是否已关闭
                    if (receiveResult.IsChannelClosed)
                    {
                        continue;
                    }

                    allCasesClosed = false;

                    if (receiveResult.Success)
                    {
                        // 接收成功
                        if (selectCase.VariableName is not null)
                        {
                            manager.Set(new LangId(selectCase.VariableName),
                                LangValueType.ObjToValue(receiveResult.Value));
                        }

                        selectCase.BlockStatement.Accept(new InterpreterVisitor(manager));
                        return;
                    }
                }
                else
                {
                    // 发送case：尝试非阻塞发送
                    var channelIdValue = selectCase.ChannelExpression.Accept(new InterpreterVisitor(manager));
                    if (channelIdValue is IntLangValue intVal)
                    {
                        int channelId = intVal.Value;

                        // 检测 Channel 是否已关闭
                        if (ResourceManager.IsChannelClosed(channelId))
                        {
                            continue;
                        }

                        allCasesClosed = false;

                        var sendValue = selectCase.SendValueExpression!.Accept(new InterpreterVisitor(manager));
                        if (ResourceManager.TrySendChannel(channelId, sendValue.GetValue()!, 0))
                        {
                            // 发送成功
                            selectCase.BlockStatement.Accept(new InterpreterVisitor(manager));
                            return;
                        }
                    }
                }
            }

            // 如果有default分支，立即执行
            if (defaultCase is not null)
            {
                defaultCase.Accept(new InterpreterVisitor(manager));
                return;
            }

            // 所有 Channel 都已关闭，抛出异常
            if (allCasesClosed && cases.Count > 0)
            {
                throw new ChannelClosedError(this, "select 语句中所有 Channel 已关闭，无法进行任何操作");
            }

            // 短暂休眠避免CPU 100%占用
            Thread.Sleep(1);
        }
    }

    /// <summary>
    /// 生成器上下文模式执行
    /// </summary>
    internal void RunWithGeneratorContext(VariateManager manager)
    {
        var context = manager.GeneratorContext!;

        // 检查是否从select块内恢复
        bool isResumingFromSelectBlock = !string.IsNullOrEmpty(context.ExecutionPath) &&
                                         context.ExecutionPath.Contains("/select_block");

        if (isResumingFromSelectBlock)
        {
            // 从select块内恢复，继续执行
            RunStandard(manager);
        }
        else
        {
            // 首次进入select语句
            var oldPath = context.ExecutionPath;
            context.ExecutionPath = $"{oldPath}/select_block";

            try
            {
                RunStandard(manager);
            }
            finally
            {
                // 恢复执行路径
                context.ExecutionPath = oldPath;
            }
        }
    }

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
