using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;

namespace Old8Lang.Interpreter;

/// <summary>
/// 闭包捕获的作用域缓存
/// 用于优化闭包性能，避免重复捕获不变的作用域
/// </summary>
public class CapturedScope
{
    /// <summary>
    /// 捕获的作用域列表（不可变视图）
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, LangValueType>> Scopes { get; }

    /// <summary>
    /// 导入信息的快照
    /// </summary>
    public IReadOnlyList<ImportInfo> ImportInfos { get; }

    /// <summary>
    /// 语言信息引用
    /// </summary>
    public LangInfo? LangInfo { get; }

    /// <summary>
    /// 源代码路径
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// 是否是不可变的闭包（用于优化）
    /// </summary>
    public bool IsImmutable { get; }

    /// <summary>
    /// 缓存的哈希码（用于快速比较）
    /// </summary>
    private readonly int _hashCode;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="scopes">捕获的作用域</param>
    /// <param name="importInfos">导入信息</param>
    /// <param name="langInfo">语言信息</param>
    /// <param name="path">源代码路径</param>
    /// <param name="isImmutable">是否不可变</param>
    public CapturedScope(
        List<Dictionary<string, LangValueType>> scopes,
        List<ImportInfo> importInfos,
        LangInfo? langInfo,
        string path,
        bool isImmutable = false)
    {
        // 创建作用域的只读包装
        Scopes = scopes.Select(s => (IReadOnlyDictionary<string, LangValueType>)s.AsReadOnly()).ToList();
        ImportInfos = importInfos.AsReadOnly();
        LangInfo = langInfo;
        Path = path;
        IsImmutable = isImmutable;

        // 预计算哈希码用于快速比较
        _hashCode = ComputeHashCode();
    }

    /// <summary>
    /// 从 VariateManager 创建捕获的作用域
    /// </summary>
    public static CapturedScope FromManager(VariateManager manager, bool copyScopes = false)
    {
        List<Dictionary<string, LangValueType>> scopesCopy;

        if (copyScopes)
        {
            // 深拷贝作用域（用于需要隔离的场景）
            scopesCopy = manager.Scopes.Select(s => new Dictionary<string, LangValueType>(s)).ToList();
        }
        else
        {
            // 直接引用（共享，用于允许修改的场景）
            scopesCopy = new List<Dictionary<string, LangValueType>>(manager.Scopes);
        }

        // 复制导入信息
        List<ImportInfo> importInfosCopy;
        lock (manager.ImportInfos)
        {
            importInfosCopy = manager.ImportInfos.ToList();
        }

        return new CapturedScope(
            scopesCopy,
            importInfosCopy,
            manager.LangInfo,
            manager.Path,
            isImmutable: false // 默认可变，允许函数体修改外部变量
        );
    }

    /// <summary>
    /// 恢复到 VariateManager
    /// </summary>
    public VariateManager ToManager(VariateManager? interpreter)
    {
        var manager = new VariateManager
        {
            LangInfo = LangInfo,
            Path = Path,
            Interpreter = interpreter?.Interpreter
        };

        // 恢复作用域（直接引用原始字典，不拷贝）
        manager.Scopes.Clear();
        foreach (var scope in Scopes)
        {
            // 将只读字典转换回可写字典
            manager.Scopes.Add(new Dictionary<string, LangValueType>(scope));
        }

        // 恢复导入信息
        manager.AddImportInfoRange(ImportInfos);

        return manager;
    }

    /// <summary>
    /// 计算哈希码
    /// </summary>
    private int ComputeHashCode()
    {
        var hash = new HashCode();
        hash.Add(Path);
        hash.Add(Scopes.Count);

        // 只使用前几个作用域的信息来计算哈希码（避免过于昂贵）
        var scopeCount = Math.Min(Scopes.Count, 3);
        for (int i = 0; i < scopeCount; i++)
        {
            hash.Add(Scopes[i].Count);
        }

        return hash.ToHashCode();
    }

    public override int GetHashCode() => _hashCode;

    /// <summary>
    /// 判断两个捕获的作用域是否相等（用于缓存键比较）
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not CapturedScope other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_hashCode != other._hashCode) return false;

        // 快速检查：路径和作用域数量
        if (Path != other.Path || Scopes.Count != other.Scopes.Count)
            return false;

        // 详细比较需要时才进行（通常不需要）
        return true;
    }
}
