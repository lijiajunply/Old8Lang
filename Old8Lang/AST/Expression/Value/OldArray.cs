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
    public OldValue Post(OldInt i,OldValue value)
    {
        var a = Values[i.Value];
        Values[i.Value] = value;
        return a;
    }
    public OldValue Get(OldInt a) => Values[a.Value];
    public override string ToString() => Values[0] == null?OldLangTree.ListToString(Va):OldLangTree.ArrayToString(Values);
}