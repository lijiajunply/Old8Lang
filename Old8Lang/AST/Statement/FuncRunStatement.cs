using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class FuncRunStatement : OldStatement
{
    private Instance? Instance { get; set; }
    private Operation? Operation { get; set; }

    public FuncRunStatement(Instance instance) => Instance = instance;
    public FuncRunStatement(Operation operation) => Operation = operation;

    public override void Run(ref VariateManager Manager)
    {
        if (Operation == null)
        {
            Instance?.Run(ref Manager);
            return;
        }

        Operation.Run(ref Manager);
    }

    public override string ToString() => Instance?.ToString()!;
}