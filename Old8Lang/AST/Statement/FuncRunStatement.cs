using Old8Lang.AST.Visitor;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

public class FuncRunStatement : OldStatement
{
    private readonly Instance? Instance;
    private readonly Operation? Operation;
    private readonly AwaitExpression? AwaitExpr;

    public FuncRunStatement(Instance instance, SourcePosition position = default) : base(position) =>
        Instance = instance;

    public FuncRunStatement(Operation operation, SourcePosition position = default) : base(position) =>
        Operation = operation;

    public FuncRunStatement(AwaitExpression awaitExpr, SourcePosition position = default) : base(position) =>
        AwaitExpr = awaitExpr;

    public override void Run(VariateManager manager)
    {
        if (AwaitExpr != null)
        {
            AwaitExpr.Run(manager);
            return;
        }

        if (Operation == null)
        {
            Instance?.Run(manager);
            return;
        }

        Operation.Run(manager);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        if (AwaitExpr != null)
        {
            AwaitExpr.LoadIlValue(ilGenerator, local);
            // 销毁栈上的值
            var outputType = AwaitExpr.OutputType(local);
            if (outputType != typeof(void)) ilGenerator.Emit(OpCodes.Pop);
            return;
        }

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

    public override string? ToString() =>
        AwaitExpr != null ? AwaitExpr.ToString() :
        Instance == null ? Operation == null ? "" : Operation.ToString() : Instance.ToString();

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("FuncRunStatement 暂不支持 Visitor 模式访问");
    }
}