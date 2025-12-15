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
        manager.AddChildren();
        // 压入新的控制流状态
        manager.ControlFlowManager.PushState();
        
        try
        {
            // 只执行一次循环迭代，然后检查yield标志
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
                blockStatement.Run(manager);
                
                // 处理break
                if (manager.ControlFlowManager.BreakFlag)
                {
                    return;
                }
                
                // 处理yield：如果循环体中遇到yield，立即返回以暂停执行
                // 不执行循环变量更新，让下一次调用处理
                if (manager.IsYield)
                {
                    return;
                }
                
                // continue由标志位控制，直接返回，让下一次调用处理
                if (manager.ControlFlowManager.ContinueFlag)
                {
                    return;
                }
                
                // 正常执行，返回，让下一次调用处理下一次迭代
                return;
            }
            else
            {
                return;
            }
        }
        finally
        {
            // 弹出当前控制流状态
            manager.ControlFlowManager.PopState();
            manager.RemoveChildren();
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