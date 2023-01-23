using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;

public class PrintStatement : OldStatement
{
    private OldExpr Value { get; set; }

    public PrintStatement(OldExpr               value) => Value = value;
    public override void Run(ref VariateManager Manager)
    {
        var a = Value.Run(ref Manager);
        Console.WriteLine(a);
    }
    public override string ToString() => $"print({Value})";
}