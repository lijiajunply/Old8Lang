using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Generators;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// while语句
/// </summary>
public partial class WhileStatement(LangExpression expression, OldStatement blockStatement, SourcePosition position = default)
    : OldStatement(position)
{
    public override void Run(VariateManager manager)
    {
        // 检查是否使用新的生成器架构
        if (manager.GeneratorContext != null)
        {
            // 新架构：标准 while 循环，生成器断点由 BlockStatement 处理
            RunWithGeneratorContext(manager);
        }
        else
        {
            // 标准 while 循环（非生成器）
            RunStandard(manager);
        }
    }

    /// <summary>
    /// 标准 while 循环（非生成器）
    /// </summary>
    /// <param name="manager">变量管理器</param>
    private void RunStandard(VariateManager manager)
    {
        // 压入新的控制流状态
        manager.ControlFlowManager.PushState();

        try
        {
            // 标准 while 循环
            while (true)
            {

                // 获取条件表达式的值
                var value = expression.Run(manager);
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

                // 执行循环体
                blockStatement.Run(manager);

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
    private void RunWithGeneratorContext(VariateManager manager)
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
                // 获取条件表达式的值
                var value = expression.Run(manager);
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
                blockStatement.Run(manager);

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

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 创建循环标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();

        // 保存当前的break和continue标签，以便嵌套循环使用
        var oldBreakLabel = local.BreakLabel;
        var oldContinueLabel = local.ContinueLabel;

        // 设置当前循环的break和continue标签
        local.BreakLabel = loopEnd;
        local.ContinueLabel = loopStart; // while循环中continue直接跳转到循环开始

        // 循环开始标签
        ilGenerator.MarkLabel(loopStart);

        // 检查循环条件
        expression.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd); // 如果条件为false，跳转到循环结束

        blockStatement.GenerateIl(ilGenerator, local);

        // 跳转回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart); // 跳转到循环开始

        // 循环结束标签
        ilGenerator.MarkLabel(loopEnd);

        // 恢复之前的break和continue标签
        local.BreakLabel = oldBreakLabel;
        local.ContinueLabel = oldContinueLabel;
    }

    public override OldStatement this[int index] => blockStatement[index] ?? throw new IndexOutOfRangeException();

    public override int Count => blockStatement.Count;

    public override string ToString() => $"while {expression}\n{blockStatement}";
}