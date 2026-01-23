using Old8Lang.Bytecode.Metadata;

namespace Old8Lang.Bytecode.Core;

/// <summary>
/// 调用栈帧
/// </summary>
public class CallFrame(FunctionMetadata function, int localCount)
{
    /// <summary>当前执行的函数</summary>
    public FunctionMetadata Function { get; } = function;

    /// <summary>局部变量数组</summary>
    public object?[] Locals { get; } = new object?[localCount];

    /// <summary>指令指针(Instruction Pointer)</summary>
    public int IP { get; set; } = 0;

    /// <summary>返回地址(调用者的栈帧)</summary>
    public CallFrame? Caller { get; set; }

    /// <summary>函数参数(用于调试)</summary>
    public object?[]? Arguments { get; set; }

    /// <summary>Defer栈 - 存储延迟执行的指令位置(LIFO顺序)</summary>
    public Stack<int> DeferStack { get; } = new();

    /// <summary>生成器ID（如果此帧是生成器函数的执行帧）</summary>
    public int? GeneratorId { get; set; }

    /// <summary>异步生成器ID（如果此帧是异步生成器函数的执行帧）</summary>
    public int? AsyncGeneratorId { get; set; }

    /// <summary>闭包捕获的变量环境（用于闭包函数）</summary>
    public Dictionary<string, object?>? ClosureEnvironment { get; set; }

    /// <summary>常量池（用于模块导入的函数）</summary>
    public ConstantPool? ConstantPool { get; set; }

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
