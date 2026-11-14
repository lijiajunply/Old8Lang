using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// switch 语句。
/// </summary>
public partial class SwitchStatement(
    LangExpression switchExpression,
    List<CaseStatement> switchCaseList,
    BlockStatement? defaultBlockStatement = null,
    SourcePosition position = default)
    : OldStatement(position)
{
    public LangExpression SwitchExpression => switchExpression;
    public List<CaseStatement> SwitchCaseList => switchCaseList;
    public BlockStatement? DefaultBlockStatement => defaultBlockStatement;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitSwitchStatement(this);

    public override OldStatement this[int index] => switchCaseList[index];

    public override int Count => switchCaseList.Count;
}

/// <summary>
/// switch 的单个 case 分支。
/// </summary>
public partial class CaseStatement(
    LangExpression expression,
    BlockStatement blockStatement,
    SourcePosition position = default)
    : OldStatement(position)
{
    public LangExpression Expression { get; } = expression;
    public BlockStatement BlockStatement { get; } = blockStatement;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitCaseStatement(this);

    public override OldStatement this[int index] => BlockStatement[index];

    public override int Count => BlockStatement.Count;
}
