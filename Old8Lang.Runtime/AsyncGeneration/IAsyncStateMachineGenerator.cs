using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;

namespace Old8Lang.AsyncGeneration;

/// <summary>
/// 异步状态机生成器在共享运行时中的契约。
/// </summary>
/// <remarks>
/// `LocalManager` 与仍留在 Runtime 的 IL 辅助需要读写当前状态机的变量槽与 await 状态，
/// 但具体生成器位于 <c>Old8Lang.Compiler</c>。这里只暴露这些共享操作。
/// </remarks>
public interface IAsyncStateMachineGenerator
{
    /// <summary>把变量提升为状态机字段。</summary>
    void DefineVariable(string name, Type type);

    /// <summary>加载已提升的变量。</summary>
    void LoadVariable(ILGenerator il, string name);

    /// <summary>保存已提升的变量。</summary>
    void StoreVariable(ILGenerator il, string name);

    /// <summary>生成状态机的 return 序列。</summary>
    void EmitReturn(ILGenerator il, Type returnType);

    /// <summary>获取 await 表达式的状态编号。</summary>
    int GetStateIndex(AwaitExpression expr);

    /// <summary>
    /// 进入受保护区域（try 块 / using 块）：标记区域首指令，并发射该区域内部的状态派发链。
    /// </summary>
    /// <param name="regionNode">区域对应的 AST 节点（<c>TryStatement</c> 或 <c>UsingStatement</c>）。</param>
    /// <remarks>
    /// 不能只在 <c>MoveNext</c> 开头用一个 <c>switch</c> 跳到所有恢复标签：恢复标签可能
    /// 位于受保护区域内部，而 CLR 的 JIT 拒绝从区域外部跳进区域中间
    /// （<c>InvalidProgramException</c>），只允许跳到区域首指令。因此外层派发跳到
    /// 内层区域入口，内层区域在自己的入口处派发到本区域的恢复标签。
    /// </remarks>
    void EnterProtectedRegion(ILGenerator il, object regionNode);

    /// <summary>
    /// 在用户 finally 体之前发射守卫：状态机处于挂起状态（state ≥ 0）时跳过 finally 体。
    /// </summary>
    /// <remarks>
    /// await 挂起时用 <c>leave</c> 离开受保护区域，CLR 会沿路执行 finally；
    /// 挂起不是函数退出，不能执行用户 finally。Roslyn 也使用同样的
    /// <c>if (state &lt; 0)</c> 守卫。跳转目标由调用方在 finally 体之后标记。
    /// </remarks>
    void EmitSuspendedFinallyGuard(ILGenerator il, Label skipLabel);

    /// <summary>生成 await 的 yield 序列。</summary>
    void EmitAwaitYield(ILGenerator il, int stateIndex, LocalBuilder awaiterLocal);

    /// <summary>生成 await 的恢复序列。</summary>
    void EmitAwaitResume(ILGenerator il, int stateIndex, LocalBuilder awaiterLocal);
}
