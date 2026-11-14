using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// Select语句，用于Channel多路选择（类似Go的select）
/// </summary>
public partial class SelectStatement(
    List<SelectCase> cases,
    BlockStatement? defaultCase,
    SourcePosition position = default) : OldStatement(position)
{
    /// <summary>
    /// 获取所有case分支
    /// </summary>
    public List<SelectCase> Cases => cases;

    /// <summary>
    /// 获取default分支（可选）
    /// </summary>
    public BlockStatement? DefaultCase => defaultCase;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitSelectStatement(this);

    public override OldStatement this[int index]
    {
        get
        {
            // Select 语句包含所有case块和default块
            if (index >= 0 && index < cases.Count)
            {
                return cases[index].BlockStatement;
            }

            if (defaultCase is not null && index == cases.Count)
            {
                return defaultCase;
            }

            // 超出范围，返回空语句
            return new BlockStatement(new List<IOldLangTree>());
        }
    }

    public override int Count => cases.Count + (defaultCase is not null ? 1 : 0); // case数量 + default块（如果有）
}
