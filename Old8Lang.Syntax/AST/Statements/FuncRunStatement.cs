using Old8Lang.AST.Expression;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 表达式语句：求值一个表达式并丢弃结果。
/// </summary>
public partial class FuncRunStatement : OldStatement
{
    private readonly LangExpression? _expression;

    public LangExpression? Expression => _expression;

    /// <summary>
    /// 通用构造函数，支持任意表达式（包括调用、await、链式调用结果）。
    /// </summary>
    public FuncRunStatement(LangExpression expression, SourcePosition position = default) : base(position) =>
        _expression = expression;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitFuncRunStatement(this);

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string? ToString() => _expression?.ToString();
}
