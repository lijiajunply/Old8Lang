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
    private readonly GenericInstanceExpression? GenericInstance;

    public FuncRunStatement(Instance instance, SourcePosition position = default) : base(position) =>
        Instance = instance;

    public FuncRunStatement(Operation operation, SourcePosition position = default) : base(position) =>
        Operation = operation;

    public FuncRunStatement(AwaitExpression awaitExpr, SourcePosition position = default) : base(position) =>
        AwaitExpr = awaitExpr;

    public FuncRunStatement(GenericInstanceExpression genericInstance, SourcePosition position = default) : base(position) =>
        GenericInstance = genericInstance;

    public override void Run(VariateManager manager)
    {
        if (AwaitExpr is not null)
        {
            AwaitExpr.Run(manager);
            return;
        }

        if (GenericInstance is not null)
        {
            GenericInstance.Run(manager);
            return;
        }

        if (Operation is null)
        {
            Instance?.Run(manager);
            return;
        }

        Operation.Run(manager);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        if (AwaitExpr is not null)
        {
            AwaitExpr.LoadIlValue(ilGenerator, local);
            // 销毁栈上的值
            var outputType = AwaitExpr.OutputType(local);
            if (outputType != typeof(void)) ilGenerator.Emit(OpCodes.Pop);
            return;
        }

        if (Operation is null)
        {
            if (Instance is null) return;
            var outputType = Instance.OutputType(local);
            Instance.LoadIlValue(ilGenerator, local);
            // 销毁栈上的值
            if (outputType != typeof(void)) ilGenerator.Emit(OpCodes.Pop);
            return;
        }

        Operation.LoadIlValue(ilGenerator, local);
        // 销毁栈上的值
        var opOutputType = Operation.OutputType(local);
        if (opOutputType != typeof(void)) ilGenerator.Emit(OpCodes.Pop);
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string? ToString() =>
        AwaitExpr is not null ? AwaitExpr.ToString() :
        GenericInstance is not null ? GenericInstance.ToString() :
        Instance is null ? Operation is null ? "" : Operation.ToString() : Instance.ToString();

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        // FuncRunStatement 是一个包装表达式作为语句的节点
        // 它内部只包含一个表达式,我们让这个表达式接受visitor,然后丢弃结果
        if (Instance != null)
        {
            return Instance.Accept(visitor);
        }
        if (Operation != null)
        {
            return Operation.Accept(visitor);
        }
        if (AwaitExpr != null)
        {
            return AwaitExpr.Accept(visitor);
        }
        if (GenericInstance != null)
        {
            return GenericInstance.Accept(visitor);
        }

        // 如果都为空,返回默认值
        return default(TResult)!;
    }
}