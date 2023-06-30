using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression;

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
    protected bool Equals(OldID other)
    {
        return IdName == other.IdName;
    }

    public override ValueType Run(ref VariateManager Manager) => Manager.GetValue(this);
}