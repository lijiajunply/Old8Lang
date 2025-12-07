using Old8Lang.LangParser;


namespace Old8Lang.AST.Expression.Value;

public class VoidValue : ValueType
{
    public override object GetValue() => throw new Exception("not value");
    public override ValueType Run(Old8Lang.LangParser.VariateManager Manager) => throw new Exception("not value");
    public override string ToString() => throw new Exception("not value");
}