using System.Text;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 经典 for 循环语句。
/// </summary>
public partial class ForStatement(
    SetStatement setStatement,
    LangExpression expression,
    OldStatement statement,
    BlockStatement blockStatement,
    SourcePosition position = default)
    : OldStatement(position)
{
    public SetStatement Init => setStatement;
    public LangExpression Condition => expression;
    public OldStatement Operation => statement;
    public BlockStatement Block => blockStatement;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitForStatement(this);

    public override OldStatement this[int index] => blockStatement[index];

    public override int Count => blockStatement.Count;

    public override string ToString()
    {
        var sb = new StringBuilder($"for {setStatement}, {expression}, {statement}");
        sb.AppendLine();
        sb.Append($"{{{blockStatement}\n}}");
        return sb.ToString();
    }
}
