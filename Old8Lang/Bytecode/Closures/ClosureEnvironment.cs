using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace Old8Lang.Bytecode.Closures;

/// <summary>
/// 闭包环境（局部捕获变量 + 可选父级环境）
/// </summary>
public sealed class ClosureEnvironment
{
    private static readonly Dictionary<string, object?> EmptyCapturedVariables = new(0);
    public static readonly ClosureEnvironment Empty = new(EmptyCapturedVariables);
    private const int FrozenLookupThreshold = 6;
    private static readonly FrozenDictionary<string, object?> EmptyFrozenCapturedVariables =
        EmptyCapturedVariables.ToFrozenDictionary(StringComparer.Ordinal);

    private readonly Dictionary<string, object?>? _capturedVariables;
    private readonly FrozenDictionary<string, object?>? _frozenCapturedVariables;

    /// <summary>
    /// 父级闭包环境（用于嵌套闭包按需回溯查找）
    /// </summary>
    public ClosureEnvironment? Parent { get; }

    /// <summary>
    /// 创建闭包环境
    /// </summary>
    public ClosureEnvironment(Dictionary<string, object?> capturedVariables, ClosureEnvironment? parent = null)
    {
        ArgumentNullException.ThrowIfNull(capturedVariables);

        if (capturedVariables.Count == 0)
        {
            _capturedVariables = null;
            _frozenCapturedVariables = EmptyFrozenCapturedVariables;
        }
        else if (capturedVariables.Count >= FrozenLookupThreshold)
        {
            _capturedVariables = null;
            _frozenCapturedVariables = capturedVariables.ToFrozenDictionary(StringComparer.Ordinal);
        }
        else
        {
            _capturedVariables = capturedVariables;
            _frozenCapturedVariables = null;
        }

        Parent = parent;
    }

    /// <summary>
    /// 局部捕获变量数量（不含父级）
    /// </summary>
    public int LocalCount => _capturedVariables?.Count ?? _frozenCapturedVariables?.Count ?? 0;

    /// <summary>
    /// 尝试获取变量值（先查局部，再向父级回溯）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetLocalValue(string variableName, out object? value)
    {
        if (_capturedVariables != null)
        {
            return _capturedVariables.TryGetValue(variableName, out value);
        }

        if (_frozenCapturedVariables != null)
        {
            return _frozenCapturedVariables.TryGetValue(variableName, out value);
        }

        value = null;
        return false;
    }

    public bool TryGetValue(string variableName, out object? value)
    {
        for (var current = this; current != null; current = current.Parent)
        {
            if (current.TryGetLocalValue(variableName, out value))
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
        if (_capturedVariables != null)
        {
            foreach (var (name, value) in _capturedVariables)
            {
                merged[name] = value;
            }
        }
        else if (_frozenCapturedVariables != null)
        {
            foreach (var (name, value) in _frozenCapturedVariables)
            {
                merged[name] = value;
            }
        }

        return merged;
    }
}
