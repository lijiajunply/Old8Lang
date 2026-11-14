using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// 调用语法节点在各后端的求值入口：适配为既有 <c>Instance</c> 后交给原 Visitor 处理。
/// </summary>
public partial class CompilerVisitor
{
    /// <summary>访问简单函数调用。</summary>
    public object? VisitCallExpression(CallExpression node) => node.ToInstance().Accept(this);
}

public partial class BytecodeVisitor
{
    /// <summary>访问简单函数调用。</summary>
    public Instruction? VisitCallExpression(CallExpression node) => node.ToInstance().Accept(this);
}

public partial class TypeInferenceVisitor
{
    /// <summary>访问简单函数调用。</summary>
    public Type? VisitCallExpression(CallExpression node) => node.ToInstance().Accept(this);
}

/// <summary>
/// 把调用语法节点归一化为既有运行时 <c>Instance</c>，供仍按
/// <c>Instance</c> 分派属性/方法访问的 Dot 实现复用。
/// </summary>
public static class CallSyntaxAdapter
{
    /// <summary>若表达式是 <see cref="CallExpression"/> 则转换为 <c>Instance</c>，否则原样返回。</summary>
    public static LangExpression? Normalize(LangExpression? expression) =>
        expression is CallExpression call ? call.ToInstance() : expression;
}

/// <summary>
/// 调用语法到运行时 <c>Instance</c> 的适配。
/// </summary>
public static class CallExpressionRuntimeExtensions
{
    /// <summary>创建等价的运行时调用节点。</summary>
    public static Instance ToInstance(this CallExpression node) =>
        new(node.Id, node.Arguments, node.NamedArguments, node.Position);
}
