using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class OldList : OldValue
{
    public new List<OldExpr> Value  { get; set; }

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
        Values.Add(value);
        return value;
    }

    public OldValue Remove(OldInt num)
    {
        var a = Values[num.Value];
        Values.RemoveAt(num.Value);
        return a;
    }

    public override string ToString() =>
        !Values.Any() ? "{"+OldLangTree.ListToString(Value)+"}" : "{"+OldLangTree.ListToString(Values)+"}";

    public override OldValue Dot(OldExpr dotExpr)
    {
        if (dotExpr is OldInstance a)
        {
            if (a.Id.IdName == "Add")
            {
               var result = a.Ids[0] as OldValue;
               return Add(result);
            }
            if (a.Id.IdName == "Remove")
            {
                var result = a.Ids[0] as OldInt;
                return Remove(result);
            }
        }
        return null;
    }

    public override bool Equal(OldValue? otherValue) => false;
}