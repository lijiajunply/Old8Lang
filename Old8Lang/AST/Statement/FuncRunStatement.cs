using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Statement;

public class FuncRunStatement : OldStatement
{
    private readonly Instance? Instance;
    private readonly Operation? Operation;

    public FuncRunStatement(Instance instance, SourcePosition position = default) : base(position) => Instance = instance;
    public FuncRunStatement(Operation operation, SourcePosition position = default) : base(position) => Operation = operation;

    public override void Run(VariateManager manager)
    {
        if (Operation == null)
        {
            Instance?.Run(manager);
            return;
        }

        Operation.Run(manager);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        if (Operation == null)
        {
            if (Instance == null) return;
            var outputType = Instance.OutputType(local);
            Instance.LoadIlValue(ilGenerator, local);
            // 销毁栈上的值
            if (outputType != typeof(void)) ilGenerator.Emit(OpCodes.Pop);
            return;
        }

        Operation.LoadIlValue(ilGenerator, local);
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString() =>
        Instance == null ? Operation == null ? "" : Operation.ToString() : Instance.ToString();
}