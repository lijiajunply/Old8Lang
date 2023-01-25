using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;

public class FuncRunStatement : OldStatement
{
    private OldInstance Instance { get; set; }

    public FuncRunStatement(OldInstance instance) => Instance = instance;

    public override void Run(ref VariateManager Manager) => Instance.Run(ref Manager);

    public override string ToString() => Instance.ToString();
}