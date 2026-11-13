using Old8Lang.AST.Visitor;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

public class FuncRunStatement : OldStatement
{
    internal readonly Instance? _instance;
    internal readonly Operation? _operation;
    internal readonly AwaitExpression? _awaitExpr;
    internal readonly GenericInstanceExpression? _genericInstance;
    internal readonly LangExpression? _expression;

    public LangExpression? Expression =>
        _expression ??
        (LangExpression?)_awaitExpr ??
        (LangExpression?)_genericInstance ??
        (LangExpression?)_instance ??
        _operation;

    public FuncRunStatement(Instance instance, SourcePosition position = default) : base(position) =>
        _instance = instance;

    public FuncRunStatement(Operation operation, SourcePosition position = default) : base(position) =>
        _operation = operation;

    public FuncRunStatement(AwaitExpression awaitExpr, SourcePosition position = default) : base(position) =>
        _awaitExpr = awaitExpr;

    public FuncRunStatement(GenericInstanceExpression genericInstance, SourcePosition position = default) :
        base(position) =>
        _genericInstance = genericInstance;

    /// <summary>
    /// 通用构造函数，支持任意表达式（包括链式调用结果）
    /// </summary>
    public FuncRunStatement(LangExpression expression, SourcePosition position = default) : base(position) =>
        _expression = expression;

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string? ToString() =>
        _expression is not null ? _expression.ToString() :
        _awaitExpr is not null ? _awaitExpr.ToString() :
        _genericInstance is not null ? _genericInstance.ToString() :
        _instance is null ? _operation is null ? "" : _operation.ToString() : _instance.ToString();

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        // FuncRunStatement 是一个包装表达式作为语句的节点
        // 它内部只包含一个表达式,我们让这个表达式接受visitor,然后丢弃结果
        if (_expression != null)
        {
            return _expression.Accept(visitor);
        }

        if (_instance != null)
        {
            return _instance.Accept(visitor);
        }

        if (_operation != null)
        {
            return _operation.Accept(visitor);
        }

        if (_awaitExpr != null)
        {
            return _awaitExpr.Accept(visitor);
        }

        if (_genericInstance != null)
        {
            return _genericInstance.Accept(visitor);
        }

        // 如果都为空,返回默认值
        return default(TResult)!;
    }
}