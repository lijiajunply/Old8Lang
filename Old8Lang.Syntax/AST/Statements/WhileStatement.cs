using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// while 语句。
/// </summary>
public partial class WhileStatement(LangExpression expression, OldStatement blockStatement, SourcePosition position = default)
    : OldStatement(position)
{
    public LangExpression Condition => expression;
    public OldStatement Block => blockStatement;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitWhileStatement(this);

    public override OldStatement this[int index] => blockStatement[index] ?? throw new IndexOutOfRangeException();

    public override int Count => blockStatement.Count;

    public override string ToString() => $"while {expression}\n{blockStatement}";
}
