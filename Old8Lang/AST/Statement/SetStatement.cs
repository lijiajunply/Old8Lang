using Old8Lang.AST.Expression;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class SetStatement(OldID id, OldExpr value) : OldStatement
{
    public OldID Id { get; set; } = id;
    public OldExpr Value { get; set; } = value;

    public override void Run(ref VariateManager Manager)
    {
        var result = Value.Run(ref Manager);
        Manager.Set(Id, result);
    }

    public override string ToString() => $"var {Id} = {Value};";
}