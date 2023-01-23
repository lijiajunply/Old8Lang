using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class OldType : OldValue
{
    private OldID Id { get; set; }

    public OldType(OldID id) => Id = id;

    public override OldValue Run(ref VariateManager Manager)
    {
        var result = Manager.GetValue(Id);
        return new OldString(result.TypeToString());
    }
    public override string ToString() => $"typeof({Id})";
}