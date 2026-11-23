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

    /// <summary>
    /// 尝试加载已提升的变量；变量未被提升为状态机字段时返回 <c>false</c> 且不发射指令。
    /// </summary>
    /// <remarks>
    /// 异步函数体里的标识符不一定是提升变量：模块静态类、具名函数值等需要回退到
    /// 常规定义解析链，不能直接按「字段不存在」报错。
    /// </remarks>
    bool TryLoadVariable(ILGenerator il, string name);

    /// <summary>获取已提升变量的字段类型；不存在时返回 null。</summary>
    Type? GetVariableType(string name);

    /// <summary>保存已提升的变量。</summary>
    void StoreVariable(ILGenerator il, string name);

    /// <summary>生成状态机的 return 序列。</summary>
    void EmitReturn(ILGenerator il, Type returnType);

    /// <summary>当前状态机是否为异步生成器（含 yield）。</summary>
    bool IsAsyncGenerator { get; }

    /// <summary>在异步生成器中将栈顶的值产出到异步流。</summary>
    void EmitYield(ILGenerator il, Type valueType);

    /// <summary>获取 await 表达式的状态编号。</summary>
    int GetStateIndex(AwaitExpression expr);

    /// <summary>获取任意异步挂起点节点（如 <c>AwaitExpression</c> 或 <c>AsyncForInStatement</c>）的状态编号。</summary>
    int GetAwaitNodeStateIndex(object node);

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
    /// 标记受保护区域在 <c>BeginExceptionBlock</c> 之前的落点。
    /// </summary>
    /// <remarks>
    /// 父区域派发应跳到这个落点，再由顺序执行进入 <c>try</c> 首指令；
    /// 不要让外层派发直接分支到受保护区域的首指令（部分 JIT 会把它当成
    /// 「跳进区域」而生成损坏的流图）。
    /// </remarks>
    void MarkProtectedRegionPreEntry(ILGenerator il, object regionNode);

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

    /// <summary>判断指定的 <see cref="TryStatement"/> 是否需要将 catch 块体搬移出 CLR 异常块以支持 await。</summary>
    bool RequiresAsyncCatch(TryStatement tryStmt);

    /// <summary>判断指定的 <see cref="TryStatement"/> 是否需要将 finally 块体搬移出 CLR 异常块以支持 await。</summary>
    bool RequiresAsyncFinally(TryStatement tryStmt);

    /// <summary>获取异步 finally 外层异常捕获保护区域的键。</summary>
    object GetFinallyGuardRegionKey(TryStatement tryStmt);

    /// <summary>压入当前生效的异步 finally 拦截作用域（用于拦截 try/catch 内的 return）。</summary>
    void PushAsyncFinallyScope(string reasonFieldName, Label finallyLabel);

    /// <summary>弹出当前异步 finally 拦截作用域。</summary>
    void PopAsyncFinallyScope();

    /// <summary>在异步 finally 尾部处理待决 return：若外层仍有异步 finally 则接力，否则完成任务。</summary>
    void EmitAsyncFinallyReturnContinuation(ILGenerator il);
}
