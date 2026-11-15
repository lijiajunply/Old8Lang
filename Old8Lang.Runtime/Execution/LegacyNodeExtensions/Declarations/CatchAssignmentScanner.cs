using Old8Lang.AST.Statement;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 找出「在 try 块和 catch 块里都被赋值」的变量名。
/// </summary>
/// <remarks>
/// 这类变量的值类型会在运行期从普通值变成异常包装值：catch 分支把它赋成异常，
/// 成功分支则赋成普通值。IL 模式若按成功分支的值类型收窄槽位，catch 处的赋值
/// 会在编译期替换槽位，使成功分支写入的值在之后被读到未初始化的 null。
/// 因此这些变量改用 <c>object</c> 槽位承载。
/// 只在 catch 里被赋值、try 里不赋值的变量（常见写法
/// <c>caughtError &lt;- ""; try { ... } catch (e) { caughtError &lt;- e }</c>）
/// 不在此列：它们没有需要保留的成功分支值。
/// </remarks>
internal static class CatchAssignmentScanner
{
    /// <summary>
    /// 收集同时出现在 try 块与 catch 块赋值目标里的变量名。
    /// </summary>
    public static HashSet<string> Collect(OldStatement? statement)
    {
        var tryAssigned = new HashSet<string>(StringComparer.Ordinal);
        var catchAssigned = new HashSet<string>(StringComparer.Ordinal);
        Walk(statement, tryAssigned, catchAssigned, inTry: false, inCatch: false);

        tryAssigned.IntersectWith(catchAssigned);
        return tryAssigned;
    }

    private static void Walk(
        OldStatement? statement,
        HashSet<string> tryAssigned,
        HashSet<string> catchAssigned,
        bool inTry,
        bool inCatch)
    {
        if (statement is null)
        {
            return;
        }

        if (statement is SetStatement { Id: not null } set)
        {
            if (inTry)
            {
                tryAssigned.Add(set.Id.IdName);
            }

            if (inCatch)
            {
                catchAssigned.Add(set.Id.IdName);
            }
        }

        if (statement is TryStatement tryStatement)
        {
            Walk(tryStatement.TryBlock, tryAssigned, catchAssigned, inTry: true, inCatch);
            foreach (var (_, _, _, catchBlock) in tryStatement.CatchBlocks)
            {
                Walk(catchBlock, tryAssigned, catchAssigned, inTry: false, inCatch: true);
            }

            Walk(tryStatement.FinallyBlock, tryAssigned, catchAssigned, inTry, inCatch);
            return;
        }

        for (var i = 0; i < statement.Count; i++)
        {
            Walk(statement[i], tryAssigned, catchAssigned, inTry, inCatch);
        }
    }
}
