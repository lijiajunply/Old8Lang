namespace Old8Lang.AST.Expression.Intermediates;

public class VoidValue : ValueType
{
    public override object GetValue() => throw new Exception("not value");
    public override ValueType Run(LangParser.VariateManager manager) => throw new Exception("not value");
    public override string ToString() => throw new Exception("not value");
}