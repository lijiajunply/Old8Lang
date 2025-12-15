using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

/// <summary>
/// while语句
/// </summary>
public class WhileStatement(LangExpression expression, OldStatement blockStatement, SourcePosition position = default)
    : OldStatement(position)
{
    public override void Run(VariateManager manager)
    {
        // 压入新的控制流状态
        manager.ControlFlowManager.PushState();

        try
        {
            // 检查是否在生成器上下文中执行
            bool isInGenerator = manager.IsInGenerator;

            // 如果在生成器中，使用单次迭代模式（支持yield暂停）
            // 否则，使用标准while循环模式
            if (isInGenerator)
            {
                // 生成器模式：只执行一次循环迭代，然后检查yield标志
                // 这样生成器可以在每次yield后暂停执行

                // 在每次循环迭代开始时重置控制流标志
                manager.ControlFlowManager.ResetCurrentState();

                // 优化：直接获取布尔值，避免临时对象创建
                var value = expression.Run(manager);
                bool expr1;
                if (value is BoolLangValue varBool)
                {
                    expr1 = varBool.Value;
                    // 优化：将临时布尔对象归还到对象池
                    varBool.ReturnToPool();
                }
                else
                {
                    throw new TypeError(this, "期望布尔类型", $"实际得到了 {value.GetType().Name}");
                }

                if (expr1)
                {
                    // 重置循环体的执行位置，确保每次迭代从头开始
                    if (blockStatement is BlockStatement block)
                    {
                        block.ResetGeneratorPosition();
                    }

                    // 循环体是嵌套的 BlockStatement，使用执行位置栈机制
                    blockStatement.Run(manager);

                    // 处理break
                    if (manager.ControlFlowManager.BreakFlag)
                    {
                        // 清除break标志，退出循环
                        manager.ControlFlowManager.BreakFlag = false;
                        return;
                    }

                    // 处理yield：如果循环体中遇到yield，立即返回以暂停执行
                    // 不清除yield标志，让上层BlockStatement处理
                    if (manager.IsYield)
                    {
                        return;
                    }

                    // 处理continue：清除continue标志，继续下一次迭代
                    if (manager.ControlFlowManager.ContinueFlag)
                    {
                        manager.ControlFlowManager.ContinueFlag = false;
                    }
                }
                else
                {
                    // 条件为false，循环结束
                    return;
                }
            }
            else
            {
                // 标准while循环模式：循环直到条件为false
                while (true)
                {
                    // 在每次循环迭代开始时重置控制流标志
                    manager.ControlFlowManager.ResetCurrentState();

                    // 优化：直接获取布尔值，避免临时对象创建
                    var value = expression.Run(manager);
                    bool expr1;
                    if (value is BoolLangValue varBool)
                    {
                        expr1 = varBool.Value;
                        // 优化：将临时布尔对象归还到对象池
                        varBool.ReturnToPool();
                    }
                    else
                    {
                        throw new TypeError(this, "期望布尔类型", $"实际得到了 {value.GetType().Name}");
                    }

                    // 如果条件为false，退出循环
                    if (!expr1)
                    {
                        break;
                    }

                    // 执行循环体
                    blockStatement.Run(manager);

                    // 处理break
                    if (manager.ControlFlowManager.BreakFlag)
                    {
                        // 清除break标志，退出循环
                        manager.ControlFlowManager.BreakFlag = false;
                        break;
                    }

                    // 处理continue：清除continue标志，继续下一次迭代
                    if (manager.ControlFlowManager.ContinueFlag)
                    {
                        manager.ControlFlowManager.ContinueFlag = false;
                        continue;
                    }
                }
            }
        }
        finally
        {
            // 弹出当前控制流状态
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