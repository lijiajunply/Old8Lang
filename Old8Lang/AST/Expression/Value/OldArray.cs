using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class OldArray : OldValue
{
    private OldValue[]    Values { get; set; }
    private List<OldExpr> Va     { get; set; }

    public OldArray(List<OldExpr> valuesList)
    {
        Values = new OldValue[valuesList.Count];
        Va     = valuesList;
    }
    public override OldValue Run(ref VariateManager Manager)
    {
        for (int i = 0; i < Va.Count; i++)
            Values[i] = Va[i].Run(ref Manager);
        return this;
    }
}