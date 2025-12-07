using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;


namespace Old8Lang.AST.Statement;

public class OldIf(OldExpr expr, BlockStatement blockStatement) : OldStatement
{
    public void Run(LangParser.VariateManager manager, ref bool r)
    {
        if (r == false) return;
        var exprValue = expr.Run(manager);
        if (exprValue is not BoolValue { Value: true }) return;
        blockStatement.Run(manager);
        r = false;
    }

    public override string ToString() => $"({expr})\n {{ {blockStatement} }}";

    public override void Run(LangParser.VariateManager manager)
    {
        blockStatement.Run(manager);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        blockStatement.GenerateIl(ilGenerator, local);
    }

    public void GenerateConditionIl(ILGenerator ilGenerator, LocalManager local)
    {
        expr.LoadIlValue(ilGenerator, local);
    }

    public override OldStatement this[int index] => blockStatement[index];

    public override int Count => blockStatement.Count;
}