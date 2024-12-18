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
            if (Instance == null) return;
            Instance.LoadILValue(ilGenerator, local);
            // 销毁栈上的值
            if (Instance.OutputType(local) != typeof(void)) ilGenerator.Emit(OpCodes.Pop);
            return;
        }

        Operation.LoadILValue(ilGenerator, local);
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString() => Instance?.ToString()!;
}