using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace Old8Lang.Bytecode.Closures;

/// <summary>
/// 闭包环境（局部捕获变量 + 可选父级环境）
/// </summary>
public sealed class ClosureEnvironment
{
    public static readonly ClosureEnvironment Empty = new([], [], null);
    private const int LinearScanThreshold = 4;
    private const int FrozenLookupThreshold = 6;

    private readonly string[] _capturedNames;
    private readonly object?[] _capturedValues;
    private readonly FrozenDictionary<string, int>? _frozenNameToIndex;
    private Dictionary<string, int>? _nameToIndex;

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
            _capturedNames = [];
            _capturedValues = [];
            _frozenNameToIndex = null;
        }
        else
        {
            _capturedNames = new string[capturedVariables.Count];
            _capturedValues = new object?[capturedVariables.Count];

            var index = 0;
            foreach (var (name, value) in capturedVariables)
            {
                _capturedNames[index] = name;
                _capturedValues[index] = value;
                index++;
            }

            _frozenNameToIndex = capturedVariables.Count >= FrozenLookupThreshold
                ? _capturedNames
                    .Select((name, idx) => new KeyValuePair<string, int>(name, idx))
                    .ToFrozenDictionary(StringComparer.Ordinal)
                : null;
        }

        Parent = parent;
    }

    public ClosureEnvironment(string[] capturedNames, object?[] capturedValues, ClosureEnvironment? parent = null)
    {
        ArgumentNullException.ThrowIfNull(capturedNames);
        ArgumentNullException.ThrowIfNull(capturedValues);
        if (capturedNames.Length != capturedValues.Length)
        {
            throw new ArgumentException("闭包名称和值数量不匹配");
        }

        _capturedNames = capturedNames;
        _capturedValues = capturedValues;
        _frozenNameToIndex = capturedNames.Length >= FrozenLookupThreshold
            ? capturedNames
                .Select((name, idx) => new KeyValuePair<string, int>(name, idx))
                .ToFrozenDictionary(StringComparer.Ordinal)
            : null;

        Parent = parent;
    }

    /// <summary>
    /// 局部捕获变量数量（不含父级）
    /// </summary>
    public int LocalCount => _capturedNames.Length;

    /// <summary>
    /// 尝试获取变量值（先查局部，再向父级回溯）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetLocalValue(string variableName, out object? value)
    {
        if (TryGetLocalIndex(variableName, out var index))
        {
            value = _capturedValues[index];
            return true;
        }

        value = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetLocalIndex(string variableName, out int index)
    {
        if (_capturedNames.Length <= LinearScanThreshold)
        {
            for (var i = 0; i < _capturedNames.Length; i++)
            {
                if (string.Equals(_capturedNames[i], variableName, StringComparison.Ordinal))
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        if (_frozenNameToIndex != null)
        {
            return _frozenNameToIndex.TryGetValue(variableName, out index);
        }

        var nameToIndex = _nameToIndex;
        if (nameToIndex == null)
        {
            nameToIndex = new Dictionary<string, int>(_capturedNames.Length, StringComparer.Ordinal);
            for (var i = 0; i < _capturedNames.Length; i++)
            {
                if (!nameToIndex.ContainsKey(_capturedNames[i]))
                {
                    nameToIndex[_capturedNames[i]] = i;
                }
            }

            _nameToIndex = nameToIndex;
        }

        return nameToIndex.TryGetValue(variableName, out index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object? GetLocalValue(int index)
    {
        return _capturedValues[index];
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
        for (var i = 0; i < _capturedNames.Length; i++)
        {
            merged[_capturedNames[i]] = _capturedValues[i];
        }

        return merged;
    }
}
