using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

public class IfChild(LangExpression expression, BlockStatement blockStatement, SourcePosition position = default)
    : OldStatement(position)
{
    
    public void Run(VariateManager manager, ref bool r)
    {
        if (!r) return;
        var exprValue = expression.Run(manager);
        if (exprValue is not BoolLangValue { Value: true }) return;
        blockStatement.Run(manager);
        r = false;
    }

    public override string ToString() => $"{expression}\n {{ {blockStatement} }}";

    public override void Run(VariateManager manager)
    {
        blockStatement.Run(manager);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        blockStatement.GenerateIl(ilGenerator, local);
    }

    public void GenerateConditionIl(ILGenerator ilGenerator, LocalManager local)
    {
        expression.LoadIlValue(ilGenerator, local);
    }

    public override OldStatement this[int index] => blockStatement[index];

    public override int Count => blockStatement.Count;
}