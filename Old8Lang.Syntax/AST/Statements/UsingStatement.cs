using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// using 语句，用于资源自动管理（类似于 C# 的 using 语句）。
/// </summary>
/// <param name="variableName">可选的变量名，用于存储资源</param>
/// <param name="resourceExpression">返回资源ID的表达式</param>
/// <param name="blockStatement">using块中的语句</param>
/// <param name="position">位置信息</param>
public partial class UsingStatement(
    string? variableName,
    LangExpression resourceExpression,
    BlockStatement blockStatement,
    SourcePosition position = default) : OldStatement(position)
{
    /// <summary>
    /// 获取变量名（可能为null）
    /// </summary>
    public string? VariableName => variableName;

    /// <summary>
    /// 获取资源表达式
    /// </summary>
    public LangExpression ResourceExpression => resourceExpression;

    /// <summary>
    /// 获取using块
    /// </summary>
    public BlockStatement BlockStatement => blockStatement;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitUsingStatement(this);

    public override OldStatement this[int index]
    {
        get
        {
            // Using 语句只包含一个块语句
            if (index == 0)
            {
                return blockStatement;
            }

            // 超出范围，返回空语句
            return new BlockStatement(new List<OldStatement>());
        }
    }

    public override int Count => 1; // 只有 using 块
}
