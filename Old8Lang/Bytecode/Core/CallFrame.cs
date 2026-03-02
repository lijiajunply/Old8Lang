using Old8Lang.Bytecode.Closures;
using Old8Lang.Bytecode.Metadata;

namespace Old8Lang.Bytecode.Core;

/// <summary>
/// 调用栈帧
/// </summary>
public class CallFrame
{
    /// <summary>当前执行的函数</summary>
    public FunctionMetadata Function { get; private set; } = null!;

    /// <summary>局部变量数组</summary>
    public object?[] Locals { get; private set; } = [];

    /// <summary>局部变量有效槽位数量</summary>
    public int LocalCount { get; private set; }

    /// <summary>局部变量数组是否来自池</summary>
    public bool UsesPooledLocals { get; private set; }

    /// <summary>指令指针(Instruction Pointer)</summary>
    public int IP { get; set; } = 0;

    /// <summary>返回地址(调用者的栈帧)</summary>
    public CallFrame? Caller { get; set; }

    /// <summary>函数参数(用于调试)</summary>
    public object?[]? Arguments { get; set; }

    private Stack<int>? _deferStack;

    /// <summary>Defer栈 - 存储延迟执行的指令位置(LIFO顺序)，按需分配</summary>
    public Stack<int> DeferStack => _deferStack ??= new Stack<int>();

    /// <summary>生成器ID（如果此帧是生成器函数的执行帧）</summary>
    public int? GeneratorId { get; set; }

    /// <summary>异步生成器ID（如果此帧是异步生成器函数的执行帧）</summary>
    public int? AsyncGeneratorId { get; set; }

    /// <summary>闭包捕获的变量环境（用于闭包函数）</summary>
    public ClosureEnvironment? ClosureEnvironment { get; set; }

    /// <summary>常量池（用于模块导入的函数）</summary>
    public ConstantPool? ConstantPool { get; set; }

    /// <summary>
    /// 创建调用栈帧（默认分配新的局部变量数组）
    /// </summary>
    public CallFrame(FunctionMetadata function, int localCount)
        : this(function, new object?[localCount], localCount, usesPooledLocals: false)
    {
    }

    /// <summary>
    /// 创建调用栈帧（允许外部提供局部变量数组）
    /// </summary>
    public CallFrame(FunctionMetadata function, object?[] locals, int localCount, bool usesPooledLocals)
    {
        Function = function;
        Locals = locals;
        LocalCount = localCount;
        UsesPooledLocals = usesPooledLocals;
    }

    /// <summary>
    /// 从对象池取出后重新初始化（复用帧实例，避免分配）
    /// </summary>
    internal void ReinitializeFromPool(FunctionMetadata function, object?[] locals, int localCount, bool usesPooledLocals)
    {
        Function = function;
        Locals = locals;
        LocalCount = localCount;
        UsesPooledLocals = usesPooledLocals;
        IP = 0;
        Caller = null;
        Arguments = null;
        _deferStack?.Clear();   // 清空但保留 Stack 实例，下次继续复用
        GeneratorId = null;
        AsyncGeneratorId = null;
        ClosureEnvironment = null;
        ConstantPool = null;
    }

    /// <summary>
    /// 归还对象池前清理持有的引用（防止 GC root 泄漏）
    /// </summary>
    internal void ClearForPool()
    {
        Arguments = null;
        ClosureEnvironment = null;
        ConstantPool = null;
        Caller = null;
        // Function/Locals/LocalCount/UsesPooledLocals 在 ReinitializeFromPool 时会被覆盖，无需清空
    }

    /// <summary>是否存在待执行的 defer 指令</summary>
    public bool HasDeferredInstructions => _deferStack is { Count: > 0 };

    /// <summary>尝试弹出一个 defer 入口</summary>
    public bool TryPopDefer(out int deferStartPosition)
    {
        if (_deferStack is { Count: > 0 })
        {
            deferStartPosition = _deferStack.Pop();
            return true;
        }

        deferStartPosition = default;
        return false;
    }

    /// <summary>
    /// 获取当前指令
    /// </summary>
    public Instruction? CurrentInstruction
    {
        get
        {
            if (IP >= 0 && IP < Function.Instructions.Count)
                return Function.Instructions[IP];
            return null;
        }
    }

    /// <summary>
    /// 是否已执行完毕
    /// </summary>
    public bool IsFinished => IP >= Function.Instructions.Count;

    public override string ToString()
    {
        return $"CallFrame[{Function.Name}, IP={IP}/{Function.Instructions.Count}]";
    }
}
