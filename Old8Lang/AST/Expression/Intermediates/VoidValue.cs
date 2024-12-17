using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class VoidValue : ValueType
{
    public override object GetValue() => throw new Exception("not value");
    public override ValueType Run(VariateManager Manager) => throw new Exception("not value");
    public override string ToString() => throw new Exception("not value");
}