using Old8Lang.AST.Expression.Value;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;

public class OldClassInit : OldStatement
{
    public AnyValue AnyValue { get; set; }
    public OldClassInit(AnyValue anyValue)
    {
        AnyValue = anyValue;
    }
    public override void Run(ref VariateManager Manager) => Manager.AddClassAndFunc(AnyValue.Id,AnyValue);

    public override string ToString() => AnyValue.ToString();
}