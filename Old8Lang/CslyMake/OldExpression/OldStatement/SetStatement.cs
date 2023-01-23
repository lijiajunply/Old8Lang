using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class SetStatement : OldStatement
{
    public OldID Id { get; set; } 
    public OldExpr Value { get; set; }
    public List<OldID> Init_ID { get; set; }

    public SetStatement(OldID id, OldExpr value)
    {
        Id = id;
        Value = value;
    }

    public SetStatement(OldID id, OldExpr value, List<OldID> a)
    {
        Id = id;
        Value = value;
        Init_ID = a;
    }

    public override void Run(ref VariateManager Manager)
    {
        var result = Value.Run(ref Manager);
        Manager.Set(Id, result);
    }

    public override string ToString() => $"{Id} = {Value} ";
}