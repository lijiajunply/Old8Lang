namespace Old8Lang.Bytecode;

/// <summary>
/// 闭包对象 - 包含函数元数据和捕获的变量
/// </summary>
public class ClosureValue(FunctionMetadata function, Dictionary<string, object?>? capturedVariables)
{
    /// <summary>函数元数据</summary>
    public FunctionMetadata Function { get; } = function ?? throw new ArgumentNullException(nameof(function));

    /// <summary>捕获的变量字典（变量名 -> 值）</summary>
    public Dictionary<string, object?> CapturedVariables { get; } =
        capturedVariables ?? new Dictionary<string, object?>();

    public override string ToString()
    {
        return $"Closure[{Function.Name}, {CapturedVariables.Count} captured vars]";
    }
}