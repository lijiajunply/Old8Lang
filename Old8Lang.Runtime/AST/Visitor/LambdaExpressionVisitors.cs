using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Runtime;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// lambda 语法节点在各后端的求值入口：先适配为既有运行时可调用值，再交给原 Visitor 处理。
/// </summary>
public partial class InterpreterVisitor
{
    /// <summary>访问 lambda 表达式。</summary>
    public LangValueType VisitLambdaExpression(LambdaExpression node) =>
        LambdaExpressionRuntimeFactory.Create(node).Accept(this);
}

public partial class CompilerVisitor
{
    /// <summary>访问 lambda 表达式。</summary>
    public object? VisitLambdaExpression(LambdaExpression node) =>
        LambdaExpressionRuntimeFactory.Create(node).Accept(this);
}

public partial class BytecodeVisitor
{
    /// <summary>访问 lambda 表达式。</summary>
    public Instruction? VisitLambdaExpression(LambdaExpression node) =>
        LambdaExpressionRuntimeFactory.Create(node).Accept(this);
}

public partial class TypeInferenceVisitor
{
    /// <summary>访问 lambda 表达式。</summary>
    public Type? VisitLambdaExpression(LambdaExpression node) =>
        LambdaExpressionRuntimeFactory.Create(node).Accept(this);
}
