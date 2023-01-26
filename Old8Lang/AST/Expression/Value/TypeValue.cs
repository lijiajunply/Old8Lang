using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class TypeValue : ValueType
{
    private OldID Id { get; set; }

    public TypeValue(OldID id) => Id = id;

    public override ValueType Run(ref VariateManager Manager)
    {
        var result = Manager.GetValue(Id);
        return new StringValue(result.TypeToString());
    }
    public override string ToString() => $"typeof({Id})";
}