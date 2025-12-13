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
        while (true)
        {
            var value = expression.Run(manager);
            bool expr1;
            if (value is BoolLangValue varBool)
            {
                expr1 = varBool.Value;
            }
            else
            {
                throw new TypeError(this, "期望布尔类型", $"实际得到了 {value.GetType().Name}");
            }

            if (expr1)
            {
                try
                {
                    blockStatement.Run(manager);
                }
                catch (BreakException)
                {
                    // 处理break
                    break;
                }
                catch (ContinueException)
                {
                    // 处理continue，直接进入下一轮循环
                }
            }
            else
            {
                manager.RemoveChildren();
                return;
            }
        }

        manager.RemoveChildren();
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