using Old8Lang.AST.Expression;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;

public class SetStatement : OldStatement
{
    public OldID Id { get; set; } 
    public OldExpr Value { get; set; }

    public SetStatement(OldID id, OldExpr value)
    {
        Id = id;
        Value = value;
    }

    public override void Run(ref VariateManager Manager)
    {
        var result = Value.Run(ref Manager);
        Manager.Set(Id, result);
    }

    public override string ToString() => $"{Id} = {Value}";
}