using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Bytecode;

/// <summary>
/// 生成器状态
/// </summary>
public enum GeneratorStatus
{
    /// <summary>未开始</summary>
    NotStarted,
    /// <summary>运行中（已暂停在yield）</summary>
    Suspended,
    /// <summary>已完成</summary>
    Completed
}

/// <summary>
/// 生成器执行状态
/// 保存生成器函数的执行上下文，支持暂停和恢复
/// </summary>
public class GeneratorState
{
    /// <summary>函数元数据</summary>
    public FunctionMetadata Function { get; }

    /// <summary>当前指令指针</summary>
    public int InstructionPointer { get; set; }

    /// <summary>局部变量</summary>
    public object?[] Locals { get; set; }

    /// <summary>操作数栈</summary>
    public Stack<LangValueType> Stack { get; set; }

    /// <summary>生成器状态</summary>
    public GeneratorStatus Status { get; set; }

    /// <summary>当前yield的值</summary>
    public LangValueType? CurrentValue { get; set; }

    /// <summary>调用参数（用于首次执行）</summary>
    public object?[]? Arguments { get; set; }

    public GeneratorState(FunctionMetadata function, object?[]? arguments = null)
    {
        Function = function;
        InstructionPointer = 0;
        Locals = new object?[function.LocalCount];
        Stack = new Stack<LangValueType>();
        Status = GeneratorStatus.NotStarted;
        Arguments = arguments;
    }

    /// <summary>
    /// 保存当前执行状态（在yield时调用）
    /// </summary>
    public void SaveState(int ip, object?[] locals, Stack<LangValueType> stack)
    {
        InstructionPointer = ip;

        // 深拷贝局部变量
        Array.Copy(locals, Locals, Math.Min(locals.Length, Locals.Length));

        // 深拷贝栈
        Stack = new Stack<LangValueType>(stack.Reverse());

        Status = GeneratorStatus.Suspended;
    }

    /// <summary>
    /// 恢复执行状态（在MoveNext时调用）
    /// </summary>
    public void RestoreState(out int ip, out object?[] locals, out Stack<LangValueType> stack)
    {
        ip = InstructionPointer;

        // 恢复局部变量
        locals = new object?[Locals.Length];
        Array.Copy(Locals, locals, Locals.Length);

        // 恢复栈
        stack = new Stack<LangValueType>(Stack.Reverse());
    }

    /// <summary>
    /// 标记生成器已完成
    /// </summary>
    public void Complete()
    {
        Status = GeneratorStatus.Completed;
        CurrentValue = null;
    }
}
