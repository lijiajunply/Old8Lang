using Old8Lang.AST.Statement;

// ReSharper disable CheckNamespace
namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// FuncLangValue - 生成器支持
/// </summary>
public partial class FuncLangValue
{
    private bool IsGenerator => ContainsYieldStatement(BlockStatement);

    private bool ContainsYieldStatement(OldStatement stmt)
    {
        if (stmt is YieldStatement)
            return true;

        // 特殊处理 TryStatement，因为它的 Count 返回 0
        if (stmt is TryStatement tryStmt)
        {
            // 使用公开属性访问块
            // 检查 try 块
            if (ContainsYieldStatement(tryStmt.TryBlock))
                return true;

            // 检查 catch 块
            foreach (var (_, _, _, catchBlock) in tryStmt.CatchBlocks)
            {
                if (ContainsYieldStatement(catchBlock))
                    return true;
            }

            // 检查 finally 块
            if (tryStmt.FinallyBlock is not null && ContainsYieldStatement(tryStmt.FinallyBlock))
                return true;

            return false;
        }

        // 检查块语句中的子语句
        for (int i = 0; i < stmt.Count; i++)
        {
            var child = stmt[i];
            if (child is not null && ContainsYieldStatement(child))
                return true;
        }

        return false;
    }


}
