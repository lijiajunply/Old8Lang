using Old8Lang.AST.Expression;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class SetStatement(OldID id, OldExpr value) : OldStatement
{
    public readonly OldID Id = id;
    public readonly OldExpr Value = value;

    public override void Run(ref VariateManager Manager)
    {
        var result = Value.Run(ref Manager);
        Manager.Set(Id, result);
    }

    public override string ToString() => $"var {Id} = {Value};";
}