namespace Old8Lang.Bytecode;

/// <summary>
/// 调用栈帧
/// </summary>
public class CallFrame
{
    /// <summary>当前执行的函数</summary>
    public FunctionMetadata Function { get; }

    /// <summary>局部变量数组</summary>
    public object?[] Locals { get; }

    /// <summary>指令指针(Instruction Pointer)</summary>
    public int IP { get; set; }

    /// <summary>返回地址(调用者的栈帧)</summary>
    public CallFrame? Caller { get; set; }

    /// <summary>函数参数(用于调试)</summary>
    public object?[]? Arguments { get; set; }

    public CallFrame(FunctionMetadata function, int localCount)
    {
        Function = function;
        Locals = new object?[localCount];
        IP = 0;
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
