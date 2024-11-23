using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class ClassInit(AnyValue anyValue) : OldStatement
{
    public override void Run(ref VariateManager Manager) => Manager.AddClassAndFunc(anyValue);

    public override string ToString() => anyValue.ToString();
}