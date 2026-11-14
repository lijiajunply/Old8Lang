using System.Text;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// if 语句。
/// </summary>
public partial class IfStatement(
    IfChild ifChildBlock,
    List<IfChild?> elifBlock,
    BlockStatement? elseBlockStatement,
    SourcePosition position = default)
    : OldStatement(position)
{
    public IEnumerable<IfChild> Children
    {
        get
        {
            yield return ifChildBlock;
            foreach (var item in elifBlock.OfType<IfChild>())
            {
                yield return item;
            }
        }
    }

    public BlockStatement? ElseBlock => elseBlockStatement;

    public override OldStatement? this[int index]
    {
        get
        {
            if (index == 0)
            {
                return ifChildBlock;
            }

            if (index == 1 + elifBlock.Count)
            {
                return elseBlockStatement;
            }

            return elifBlock[index - 1];
        }
    }

    public override int Count => 1 + elifBlock.Count + (elseBlockStatement is null ? 0 : 1);

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitIfStatement(this);

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"if {ifChildBlock}");
        foreach (var elif in elifBlock.OfType<IfChild>())
        {
            sb.AppendLine($"elif {elif}");
        }

        if (elseBlockStatement is not null)
        {
            sb.AppendLine($"else {elseBlockStatement}");
        }

        return sb.ToString();
    }
}
