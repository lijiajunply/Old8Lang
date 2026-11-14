using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 简单函数调用表达式（标识符调用，支持命名参数）。
/// </summary>
/// <remarks>
/// 该节点只承载调用语法；求值/编译时由 Runtime 适配为既有 <c>Instance</c>。
/// </remarks>
public sealed class CallExpression(
    LangId id,
    List<LangExpression> arguments,
    List<NamedArgument>? namedArguments = null,
    SourcePosition position = default) : LangExpression(position)
{
    /// <summary>被调用的标识符。</summary>
    public LangId Id { get; } = id;

    /// <summary>位置参数。</summary>
    public List<LangExpression> Arguments { get; } = arguments;

    /// <summary>命名参数（可能为空）。</summary>
    public List<NamedArgument> NamedArguments { get; } = namedArguments ?? [];

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitCallExpression(this);

    public override string ToString()
    {
        var arguments = string.Join(", ", this.Arguments.Select(a => a.ToString()));
        return $"{Id}({arguments})";
    }
}
