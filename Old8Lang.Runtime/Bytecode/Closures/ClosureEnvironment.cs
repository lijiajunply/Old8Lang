using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace Old8Lang.Bytecode.Closures;

/// <summary>
/// 闭包环境（局部捕获变量 + 可选父级环境）
/// </summary>
/// <remarks>
/// 捕获项一律以 <see cref="UpValueCell"/> 共享单元的形式存放，因此闭包对环境变量的
/// 读写与外层作用域双向可见。详见 <see cref="UpValueCell"/>。
/// </remarks>
public sealed class ClosureEnvironment
{
    public static readonly ClosureEnvironment Empty = new([], [], null);
    private const int LinearScanThreshold = 4;
    private const int FrozenLookupThreshold = 6;

    private readonly string[] _capturedNames;
    private readonly UpValueCell[] _capturedCells;
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
            _capturedCells = [];
            _frozenNameToIndex = null;
        }
        else
        {
            _capturedNames = new string[capturedVariables.Count];
            _capturedCells = new UpValueCell[capturedVariables.Count];

            var index = 0;
            foreach (var (name, value) in capturedVariables)
            {
                _capturedNames[index] = name;
                _capturedCells[index] = new UpValueCell(value);
                index++;
            }

            _frozenNameToIndex = BuildFrozenIndex(_capturedNames);
        }

        Parent = parent;
    }

    /// <summary>
    /// 用一批原始值创建闭包环境（每个值各自新建共享单元）
    /// </summary>
    public ClosureEnvironment(string[] capturedNames, object?[] capturedValues, ClosureEnvironment? parent = null)
        : this(capturedNames, WrapIntoNewCells(capturedValues), parent)
    {
    }

    /// <summary>
    /// 用现成的共享单元创建闭包环境（用于把外层帧已经装箱的变量按引用传给闭包）
    /// </summary>
    public ClosureEnvironment(string[] capturedNames, UpValueCell[] capturedCells, ClosureEnvironment? parent = null)
    {
        ArgumentNullException.ThrowIfNull(capturedNames);
        ArgumentNullException.ThrowIfNull(capturedCells);
        if (capturedNames.Length != capturedCells.Length)
        {
            throw new ArgumentException("闭包名称和值数量不匹配");
        }

        _capturedNames = capturedNames;
        _capturedCells = capturedCells;
        _frozenNameToIndex = BuildFrozenIndex(capturedNames);
        Parent = parent;
    }

    private static UpValueCell[] WrapIntoNewCells(object?[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var cells = new UpValueCell[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            cells[i] = new UpValueCell(values[i]);
        }

        return cells;
    }

    private static FrozenDictionary<string, int>? BuildFrozenIndex(string[] names)
    {
        return names.Length >= FrozenLookupThreshold
            ? names
                .Select((name, idx) => new KeyValuePair<string, int>(name, idx))
                .ToFrozenDictionary(StringComparer.Ordinal)
            : null;
    }

    /// <summary>
    /// 局部捕获变量数量（不含父级）
    /// </summary>
    public int LocalCount => _capturedNames.Length;

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

    /// <summary>
    /// 取本环境的共享单元（不回溯父级）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UpValueCell GetLocalCell(int index)
    {
        return _capturedCells[index];
    }

    /// <summary>
    /// 取本环境的变量值（不回溯父级）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object? GetLocalValue(int index)
    {
        return _capturedCells[index].Value;
    }

    /// <summary>
    /// 沿父级链查找变量的共享单元
    /// </summary>
    public bool TryGetCell(string variableName, out UpValueCell? cell)
    {
        for (var current = this; current != null; current = current.Parent)
        {
            if (current.TryGetLocalIndex(variableName, out var index))
            {
                cell = current._capturedCells[index];
                return true;
            }
        }

        cell = null;
        return false;
    }

    public bool TryGetValue(string variableName, out object? value)
    {
        if (TryGetCell(variableName, out var cell))
        {
            value = cell!.Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// 沿父级链写入被捕获的变量（经由共享单元，外层可见）
    /// </summary>
    /// <returns>该名字是否是被本环境（或父级）捕获的变量</returns>
    public bool TrySetValue(string variableName, object? value)
    {
        if (!TryGetCell(variableName, out var cell))
        {
            return false;
        }

        cell!.Value = value;
        return true;
    }

    /// <summary>
    /// 将当前环境（含父级）快照为平面字典
    /// </summary>
    public Dictionary<string, object?> SnapshotToDictionary()
    {
        var merged = Parent?.SnapshotToDictionary() ?? new Dictionary<string, object?>();
        for (var i = 0; i < _capturedNames.Length; i++)
        {
            merged[_capturedNames[i]] = _capturedCells[i].Value;
        }

        return merged;
    }
}
