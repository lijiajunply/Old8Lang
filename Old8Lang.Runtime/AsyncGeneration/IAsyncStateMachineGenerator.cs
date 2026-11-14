using System.Reflection.Emit;
using Old8Lang.AST.Expression;

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

    /// <summary>生成 await 的 yield 序列。</summary>
    void EmitAwaitYield(ILGenerator il, int stateIndex, LocalBuilder awaiterLocal);

    /// <summary>生成 await 的恢复序列。</summary>
    void EmitAwaitResume(ILGenerator il, int stateIndex, LocalBuilder awaiterLocal);
}
