using System.Text;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 块语句：把声明/导入与普通语句分开保存，保持既有的执行与编译顺序。
/// </summary>
public partial class BlockStatement : OldStatement
{
    /// <summary>声明与静态导入语句，先于普通语句执行或编译。</summary>
    public readonly List<OldStatement> ImportStatements = [];

    /// <summary>普通语句（含动态导入），按源码顺序执行。</summary>
    public readonly List<OldStatement> OtherStatements = [];

    public override int Count => OtherStatements.Count;

    public int Total => ImportStatements.Count + OtherStatements.Count;

    public BlockStatement(IEnumerable<IOldLangTree> statements, SourcePosition position = default) : base(position)
    {
        // 遍历所有语句
        foreach (var statement in statements.OfType<OldStatement>())
        {
            // 根据语句类型添加到不同的列表中（通过语法层标记接口，避免引用具体声明节点类型）
            switch (statement)
            {
                case IBlockImportDeclaration importStmt:
                    // 动态导入需要放在 OtherStatements 中，以便在正常语句流中执行
                    if (importStmt.IsDynamicImport)
                    {
                        OtherStatements.Add(statement);
                    }
                    else
                    {
                        ImportStatements.Add(statement);
                    }

                    break;
                case IBlockDeclaration:
                    ImportStatements.Add(statement);
                    break;
                case ReturnStatement:
                    OtherStatements.Add(statement);
                    return;
                default:
                    OtherStatements.Add(statement);
                    break;
            }
        }
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitBlockStatement(this);

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var statement in ImportStatements)
            sb.Append(statement + Environment.NewLine);
        foreach (var statement in OtherStatements)
            sb.Append(statement + Environment.NewLine);

        return sb.ToString();
    }

    public string ToCode()
    {
        var sb = new StringBuilder();
        // 输出所有导入和初始化语句
        foreach (var statement in ImportStatements)
            sb.AppendLine(statement.ToString());
        // 输出其他语句
        foreach (var statement in OtherStatements)
            sb.AppendLine(statement.ToString());
        return sb.ToString();
    }

    public override OldStatement this[int index] => OtherStatements[index];

    public IOldLangTree GetImportStatement(int index) => ImportStatements[index];
}
