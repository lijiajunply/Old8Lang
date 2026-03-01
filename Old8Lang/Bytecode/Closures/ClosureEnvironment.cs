namespace Old8Lang.Bytecode.Closures;

/// <summary>
/// 闭包环境（局部捕获变量 + 可选父级环境）
/// </summary>
public sealed class ClosureEnvironment
{
    private static readonly Dictionary<string, object?> EmptyCapturedVariables = new(0);
    public static readonly ClosureEnvironment Empty = new(EmptyCapturedVariables);

    private readonly Dictionary<string, object?> _capturedVariables;

    /// <summary>
    /// 父级闭包环境（用于嵌套闭包按需回溯查找）
    /// </summary>
    public ClosureEnvironment? Parent { get; }

    /// <summary>
    /// 创建闭包环境
    /// </summary>
    public ClosureEnvironment(Dictionary<string, object?> capturedVariables, ClosureEnvironment? parent = null)
    {
        _capturedVariables = capturedVariables ?? throw new ArgumentNullException(nameof(capturedVariables));
        Parent = parent;
    }

    /// <summary>
    /// 局部捕获变量数量（不含父级）
    /// </summary>
    public int LocalCount => _capturedVariables.Count;

    /// <summary>
    /// 尝试获取变量值（先查局部，再向父级回溯）
    /// </summary>
    public bool TryGetValue(string variableName, out object? value)
    {
        for (var current = this; current != null; current = current.Parent)
        {
            if (current._capturedVariables.TryGetValue(variableName, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }

    /// <summary>
    /// 将当前环境（含父级）快照为平面字典
    /// </summary>
    public Dictionary<string, object?> SnapshotToDictionary()
    {
        var merged = Parent?.SnapshotToDictionary() ?? new Dictionary<string, object?>();
        foreach (var (name, value) in _capturedVariables)
        {
            merged[name] = value;
        }

        return merged;
    }
}
