using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

/// <summary>
/// if语句
/// </summary>
public class IfStatement(OldIf ifBlock, List<OldIf?> elifBlock, BlockStatement? elseBlockStatement)
    : OldStatement
{
    public override void Run(VariateManager Manager)
    {
        var r = true;
        Manager.AddChildren();
        ifBlock.Run(Manager, ref r);
        Manager.RemoveChildren();
        foreach (var variable in elifBlock.OfType<OldIf>())
        {
            Manager.AddChildren();
            variable.Run(Manager, ref r);
            Manager.RemoveChildren();
        }

        if (r)
            elseBlockStatement?.Run(Manager);
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        var labelElse = ilGenerator.DefineLabel();
        var labelEnd = ilGenerator.DefineLabel();

        // 处理 if 块
        ifBlock.GenerateConditionIL(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Brfalse, labelElse);

        // if 部分
        ifBlock.GenerateIL(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Br, labelEnd);

        // 处理 elif 块
        ilGenerator.MarkLabel(labelElse);
        foreach (var elif in elifBlock.OfType<OldIf>())
        {
            var nextElif = ilGenerator.DefineLabel();
            elif.GenerateConditionIL(ilGenerator, local);
            ilGenerator.Emit(OpCodes.Brfalse, nextElif);

            // elif 部分
            elif.GenerateIL(ilGenerator, local);
            ilGenerator.Emit(OpCodes.Br, labelEnd);

            ilGenerator.MarkLabel(nextElif);
        }

        // 处理 else 块
        elseBlockStatement?.GenerateIL(ilGenerator, local);

        // 结束标签
        ilGenerator.MarkLabel(labelEnd);
    }

    public override string ToString() =>
        $"if {ifBlock} else if{Apis.ListToString(elifBlock)} \nelse {{ {elseBlockStatement} }}";
}