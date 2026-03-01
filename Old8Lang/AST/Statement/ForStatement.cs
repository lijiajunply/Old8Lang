using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

public partial class ForStatement(
    SetStatement setStatement,
    LangExpression expression,
    OldStatement statement,
    BlockStatement blockStatement,
    SourcePosition position = default)
    : OldStatement(position)
{
    public SetStatement Init => setStatement;
    public LangExpression Condition => expression;
    public OldStatement Operation => statement;
    public BlockStatement Block => blockStatement;

    public override void Run(VariateManager manager)
    {
        manager.AddChildren();
        // 压入新的控制流状态
        manager.ControlFlowManager.PushState();

        try
        {
            // 使用 SetLocal 确保循环变量在当前作用域中创建，防止污染外层同名变量
            // 例如：外层 for i <- 0, i < 20 调用函数，函数内 for i <- 0, i < 5 不应修改外层 i
            if (setStatement.Id is not null)
            {
                var initValue = setStatement.Value.Run(manager);
                manager.SetLocal(setStatement.Id, initValue);
            }
            else
            {
                setStatement.Run(manager);
            }

            // 性能优化：检测简单循环模式（可以优化的循环）
            bool isSimpleLoop = IsSimpleCountingLoop();

            while (true)
            {
                // 重置控制流状态，确保每次循环迭代开始时清除之前的break/continue标志
                manager.ControlFlowManager.ResetCurrentState();

                var varExpr = expression.Run(manager);
                bool expr1;
                if (varExpr is BoolLangValue value)
                {
                    expr1 = value.Value;
                    // 优化：将临时布尔对象归还到对象池
                    value.ReturnToPool();
                }
                else
                    throw new TypeError(this, "期望布尔类型", $"实际得到了 {varExpr.GetType().Name}");

                if (expr1)
                {
                    // 记录循环迭代（性能监控）
                    manager.Interpreter?.PerformanceMonitor?.RecordLoopIteration();

                    blockStatement.Run(manager);

                    // 处理yield：如果循环体中遇到yield，返回以暂停执行
                    if (manager.IsYield)
                    {
                        // 提前执行循环增量，确保生成器恢复时继续推进
                        statement.Run(manager);
                        return;
                    }

                    // 处理break
                    if (manager.ControlFlowManager.BreakFlag)
                    {
                        manager.ControlFlowManager.BreakFlag = false;
                        break;
                    }

                    // 处理continue，执行循环增量操作
                    if (manager.ControlFlowManager.ContinueFlag)
                    {
                        manager.ControlFlowManager.ContinueFlag = false;
                        statement.Run(manager);
                        continue;
                    }

                    // 正常执行，执行循环增量操作
                    statement.Run(manager);
                }
                else
                    break;
            }
        }
        finally
        {
            // 弹出当前控制流状态
            manager.ControlFlowManager.PopState();
            manager.RemoveChildren();
        }
    }

    /// <summary>
    /// 检测是否为简单的计数循环（可以优化）
    /// </summary>
    private bool IsSimpleCountingLoop()
    {
        // 简单启发式：检查是否是二元比较表达式
        // 这是一个简化版本，实际实现可以更复杂
        return expression is Expression.Operation;
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        setStatement.GenerateIl(ilGenerator, local);

        // 创建循环标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();
        var continueLabel = ilGenerator.DefineLabel();

        // 保存当前的break和continue标签，以便嵌套循环使用
        var oldBreakLabel = local.BreakLabel;
        var oldContinueLabel = local.ContinueLabel;

        // 设置当前循环的break和continue标签
        local.BreakLabel = loopEnd;
        local.ContinueLabel = continueLabel;

        // 跳转到循环开始
        ilGenerator.MarkLabel(loopStart);

        // 检查循环条件
        expression.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd); // 如果条件为false，跳转到循环结束

        blockStatement.GenerateIl(ilGenerator, local);

        // continue标签：执行循环迭代语句
        ilGenerator.MarkLabel(continueLabel);
        statement.GenerateIl(ilGenerator, local);

        // 跳转回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart); // 跳转到循环开始

        // 循环结束标签
        ilGenerator.MarkLabel(loopEnd);

        // 恢复之前的break和continue标签
        local.BreakLabel = oldBreakLabel;
        local.ContinueLabel = oldContinueLabel;
    }

    public override OldStatement this[int index] => blockStatement[index];

    public override int Count => blockStatement.Count;

    public override string ToString()
    {
        var sb = new StringBuilder($"for {setStatement}, {expression}, {statement}");
        sb.AppendLine();
        sb.Append($"{{{blockStatement}\n}}");
        return sb.ToString();
    }
}