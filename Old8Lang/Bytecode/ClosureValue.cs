namespace Old8Lang.Bytecode;

/// <summary>
/// 闭包对象 - 包含函数元数据和捕获的变量
/// </summary>
public class ClosureValue
{
    /// <summary>函数元数据</summary>
    public FunctionMetadata Function { get; }

    /// <summary>捕获的变量字典（变量名 -> 值）</summary>
    public Dictionary<string, object?> CapturedVariables { get; }

    public ClosureValue(FunctionMetadata function, Dictionary<string, object?> capturedVariables)
    {
        Function = function ?? throw new ArgumentNullException(nameof(function));
        CapturedVariables = capturedVariables ?? new Dictionary<string, object?>();
    }

    /// <summary>
    /// 创建一个没有捕获变量的闭包（普通函数）
    /// </summary>
    public ClosureValue(FunctionMetadata function) : this(function, new Dictionary<string, object?>())
    {
    }

    public override string ToString()
    {
        return $"Closure[{Function.Name}, {CapturedVariables.Count} captured vars]";
    }
}
