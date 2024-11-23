using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression;

public class OldID(string name) : OldExpr
{
    public readonly string IdName = name;
    public override string ToString() => IdName;

    public override bool Equals(object? obj)
    {
        var a = obj as OldID;
        return a?.IdName == IdName;
    }

    public override int GetHashCode()
    {
        return IdName.GetHashCode();
    }

    public override ValueType Run(ref VariateManager Manager) => Manager.GetValue(this) ?? new VoidValue();
}