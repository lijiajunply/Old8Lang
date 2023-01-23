using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldID : OldExpr
{
    public string IdName { get; set; }
    public OldID(string name) => IdName = name;
    public override string ToString() => IdName;
    public override bool Equals(object? obj)
    {
        var a = obj as OldID;
        return a.IdName == IdName;
    }

    public override OldValue Run(ref VariateManager Manager) => Manager.GetValue(this);
    
}