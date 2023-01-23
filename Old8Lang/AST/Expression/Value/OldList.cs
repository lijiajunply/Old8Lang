using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class OldList : OldValue
{
    public new List<OldExpr>  Value  { get; set; }

    public List<OldValue> Values { get; set; } = new List<OldValue>();

    public OldList(List<OldExpr> value)
    {
        Value = value;
    }
    public override OldValue Run(ref VariateManager Manager)
    {
        foreach (var VARIABLE in Value)
            Values.Add(VARIABLE.Run(ref Manager));
        return this;
    }

    public OldValue Add(OldValue value)
    {
        Value.Add(value);
        return value;
    }

    public OldValue Remove(OldInt num)
    {
        var a = Values[num.Value];
        Value.RemoveAt(num.Value);
        return a;
    }

    public override string ToString() =>
        !Values.Any() ? "{"+OldLangTree.ListToString(Value)+"}" : "{"+OldLangTree.ListToString(Values)+"}";

    public override OldValue Dot(OldExpr dotExpr)
    {
        if (dotExpr is OldInstance)
        {
            var a = dotExpr as OldInstance;
            if (a.Id.IdName == "Add")
            {
                
            }
        }
        return null;
    }

    public override bool Equal(OldValue? otherValue) => false;
}