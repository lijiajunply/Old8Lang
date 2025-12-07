using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang;

namespace Old8Lang.AST.Statement;

/// <summary>
/// while语句
/// </summary>
public class WhileStatement(OldExpr expr, BlockStatement blockStatement, SourcePosition position = default) : OldStatement(position)
{
    public override void Run(VariateManager manager)
    {
        manager.AddChildren();
        while (true)
        {
            var value = expr.Run(manager);
            bool expr1;
            if (value is BoolValue varBool)
            {
                expr1 = varBool.Value;
            }
            else
            {
                throw new TypeError(this, "期望布尔类型", $"实际得到了 {value.GetType().Name}");
            }

            if (expr1)
            {
                blockStatement.Run(manager);
            }
            else
            {
                manager.RemoveChildren();
                return;
            }
        }
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 创建循环开始标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();

        // 跳转到循环开始
        ilGenerator.MarkLabel(loopStart);

        // 检查循环条件
        expr.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd); // 如果 loopCounter >= 10，跳转到 loopEnd
        
        blockStatement.GenerateIl(ilGenerator, local);

        // 跳转回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart); // 跳转到 loopStart

        // 循环结束标签
        ilGenerator.MarkLabel(loopEnd);
    }

    public override OldStatement this[int index] => blockStatement[index];

    public override int Count => blockStatement.Count;

    public override string ToString() => $"while({expr}){blockStatement}";
}