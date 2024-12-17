using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class OldIf(OldExpr expr, BlockStatement blockStatement) : OldStatement
{
    public void Run(VariateManager Manager, ref bool r)
    {
        if (r == false) return;
        var exprValue = expr.Run(Manager);
        if (exprValue is not BoolValue { Value: true }) return;
        blockStatement.Run(Manager);
        r = false;
    }

    public override string ToString() => $"({expr})\n {{ {blockStatement} }}";

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        blockStatement.GenerateIL(ilGenerator, local);
    }

    public void GenerateConditionIL(ILGenerator ilGenerator, LocalManager local)
    {
        expr.LoadILValue(ilGenerator, local);
    }
}