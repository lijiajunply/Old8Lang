using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class ClassInit : OldStatement
{
    private AnyValue AnyValue { get; set; }
    public ClassInit(AnyValue anyValue)
    {
        AnyValue = anyValue;
    }
    public override void Run(ref VariateManager Manager) => Manager.AddClassAndFunc(AnyValue);

    public override string ToString() => AnyValue.ToString();
}