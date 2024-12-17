using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class FuncRunStatement : OldStatement
{
    private readonly Instance? Instance;
    private readonly Operation? Operation;

    public FuncRunStatement(Instance instance) => Instance = instance;
    public FuncRunStatement(Operation operation) => Operation = operation;

    public override void Run(VariateManager Manager)
    {
        if (Operation == null)
        {
            Instance?.Run(Manager);
            return;
        }

        Operation.Run(Manager);
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        if (Operation == null)
        {
            Instance?.GenerateILValue(ilGenerator, local);
            return;
        }

        Operation.GenerateILValue(ilGenerator, local);
    }

    public override string ToString() => Instance?.ToString()!;
}