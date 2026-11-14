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
/// SelectStatement 的解释执行辅助实现（阶段 2 从语法节点迁出）。
/// </summary>
internal static class SelectStatementRuntimeExtensions
{
    /// <summary>
    /// 标准模式执行（非生成器）
    /// </summary>
    internal static void RunStandard(this SelectStatement node, VariateManager manager)
    {
        // 使用轮询策略实现多路选择
        while (true)
        {
            // 检查每个case
            bool allCasesClosed = true;

            foreach (var selectCase in node.Cases)
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
                        var channelIdValue = funcCall.Ids[0].Accept(RuntimeVisitors.CreateInterpreter(manager));
                        if (channelIdValue is not IntLangValue intVal)
                        {
                            continue;
                        }

                        channelId = intVal.Value;
                    }
                    else
                    {
                        // 直接执行 ChannelExpression 获取 channel ID
                        var channelIdValue = selectCase.ChannelExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
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

                        selectCase.BlockStatement.Accept(RuntimeVisitors.CreateInterpreter(manager));
                        return;
                    }
                }
                else
                {
                    // 发送case：尝试非阻塞发送
                    var channelIdValue = selectCase.ChannelExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
                    if (channelIdValue is IntLangValue intVal)
                    {
                        int channelId = intVal.Value;

                        // 检测 Channel 是否已关闭
                        if (ResourceManager.IsChannelClosed(channelId))
                        {
                            continue;
                        }

                        allCasesClosed = false;

                        var sendValue = selectCase.SendValueExpression!.Accept(RuntimeVisitors.CreateInterpreter(manager));
                        if (ResourceManager.TrySendChannel(channelId, sendValue.GetValue()!, 0))
                        {
                            // 发送成功
                            selectCase.BlockStatement.Accept(RuntimeVisitors.CreateInterpreter(manager));
                            return;
                        }
                    }
                }
            }

            // 如果有default分支，立即执行
            if (node.DefaultCase is not null)
            {
                node.DefaultCase.Accept(RuntimeVisitors.CreateInterpreter(manager));
                return;
            }

            // 所有 Channel 都已关闭，抛出异常
            if (allCasesClosed && node.Cases.Count > 0)
            {
                throw new ChannelClosedError(node, "select 语句中所有 Channel 已关闭，无法进行任何操作");
            }

            // 短暂休眠避免CPU 100%占用
            Thread.Sleep(1);
        }
    }
    /// <summary>
    /// 生成器上下文模式执行
    /// </summary>
    internal static void RunWithGeneratorContext(this SelectStatement node, VariateManager manager)
    {
        var context = manager.GeneratorContext!;

        // 检查是否从select块内恢复
        bool isResumingFromSelectBlock = !string.IsNullOrEmpty(context.ExecutionPath) &&
                                         context.ExecutionPath.Contains("/select_block");

        if (isResumingFromSelectBlock)
        {
            // 从select块内恢复，继续执行
            node.RunStandard(manager);
        }
        else
        {
            // 首次进入select语句
            var oldPath = context.ExecutionPath;
            context.ExecutionPath = $"{oldPath}/select_block";

            try
            {
                node.RunStandard(manager);
            }
            finally
            {
                // 恢复执行路径
                context.ExecutionPath = oldPath;
            }
        }
    }
}
