using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class ClassInit(AnyValue anyValue) : OldStatement
{
    private AnyValue AnyValue { get; } = anyValue;

    public override void Run(ref VariateManager Manager) => Manager.AddClassAndFunc(AnyValue);

    public override string ToString() => AnyValue.ToString();
}