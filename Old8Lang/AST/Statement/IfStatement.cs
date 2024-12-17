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
        throw new NotImplementedException();
    }

    public override string ToString() =>
        $"if {ifBlock} else if{Apis.ListToString(elifBlock)} \nelse {{ {elseBlockStatement} }}";
}