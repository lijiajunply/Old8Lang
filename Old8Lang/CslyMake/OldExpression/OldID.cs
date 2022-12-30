namespace Old8Lang.CslyMake.OldExpression;

public class OldID : OldExpr
{
    public string Location { get; set; }
    public string IdName { get; set; }
    public OldID(string name) => IdName = name;
    public override string ToString() => IdName;
}