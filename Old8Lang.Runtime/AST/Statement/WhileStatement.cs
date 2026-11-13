using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// while语句
/// </summary>
public partial class WhileStatement(LangExpression expression, OldStatement blockStatement, SourcePosition position = default)
    : OldStatement(position)
{
    public LangExpression Condition => expression;
    public OldStatement Block => blockStatement;

    /// <summary>
    /// 标准 while 循环（非生成器）
    /// </summary>
    /// <param name="manager">变量管理器</param>
    internal void RunStandard(VariateManager manager)
    {
        // 压入新的控制流状态
        manager.ControlFlowManager.PushState();

        try
        {
            // 标准 while 循环
            while (true)
            {

                // 获取条件表达式的值
                var value = expression.Accept(new InterpreterVisitor(manager));
                if (value is not BoolLangValue varBool)
                {
                    throw new TypeError(this, "期望布尔类型", $"实际得到了 {value.GetType().Name}");
                }

                bool conditionResult = varBool.Value;
                // 注意：不要在这里调用 ReturnToPool()，因为表达式可能会重用这个对象
                // 如果在这里归还到池并重置，下次迭代时可能会得到错误的值

                // 如果条件为 false，退出循环
                if (!conditionResult)
                {
                    break;
                }

                // 记录循环迭代（性能监控）
                manager.Interpreter?.PerformanceMonitor?.RecordLoopIteration();

                // 执行循环体
                blockStatement.Accept(new InterpreterVisitor(manager));

                // 处理 break
                if (manager.ControlFlowManager.BreakFlag)
                {
                    manager.ControlFlowManager.BreakFlag = false;
                    break;
                }

                // 处理 continue
                if (manager.ControlFlowManager.ContinueFlag)
                {
                    manager.ControlFlowManager.ContinueFlag = false;
                }
            }
        }
        finally
        {
            // 弹出当前控制流状态
            manager.ControlFlowManager.PopState();
        }
    }

    /// <summary>
    /// 使用新架构运行（基于路径的状态恢复）
    /// </summary>
    /// <param name="manager">变量管理器</param>
    internal void RunWithGeneratorContext(VariateManager manager)
    {
        manager.ControlFlowManager.PushState();
        var context = manager.GeneratorContext!;

        // 获取当前循环路径
        var loopPath = context.GetCurrentPath() + "/while";

        // 从循环状态中恢复起始迭代次数（仅用于跟踪）
        int iterationCount = 0;
        if (context.LoopStates.TryGetValue(loopPath, out var savedIteration))
        {
            iterationCount = savedIteration;
        }

        // 将循环路径压栈
        context.PathStack.Push("/while");

        try
        {
            // 标准 while 循环
            while (true)
            {
                // 重置控制流状态
                manager.ControlFlowManager.ResetCurrentState();

                // 获取条件表达式的值
                var value = expression.Accept(new InterpreterVisitor(manager));
                if (value is not BoolLangValue varBool)
                {
                    throw new TypeError(this, "期望布尔类型", $"实际得到了 {value.GetType().Name}");
                }

                bool conditionResult = varBool.Value;

                // 如果条件为 false，退出循环
                if (!conditionResult)
                {
                    break;
                }

                // 执行循环体
                blockStatement.Accept(new InterpreterVisitor(manager));

                // 检查是否遇到 yield
                if (context.HasYielded)
                {
                    // 保存当前迭代次数
                    context.LoopStates[loopPath] = iterationCount;
                    return;
                }

                // 处理 break
                if (manager.ControlFlowManager.BreakFlag)
                {
                    manager.ControlFlowManager.BreakFlag = false;
                    break;
                }

                // 处理 continue
                if (manager.ControlFlowManager.ContinueFlag)
                {
                    manager.ControlFlowManager.ContinueFlag = false;
                    continue;
                }

                // 循环体执行完毕，进入下一次迭代前清除ExecutionPath
                // 这确保下一次迭代能够完整执行
                context.ExecutionPath = "";
                iterationCount++;
            }

            // 循环完成，清除循环状态
            context.LoopStates.Remove(loopPath);
        }
        finally
        {
            // 弹出循环路径
            context.PathStack.Pop();
            manager.ControlFlowManager.PopState();
        }
    }

    public override OldStatement this[int index] => blockStatement[index] ?? throw new IndexOutOfRangeException();

    public override int Count => blockStatement.Count;

    public override string ToString() => $"while {expression}\n{blockStatement}";
}