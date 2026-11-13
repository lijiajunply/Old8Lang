using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Expression.AnyValues;

/// <summary>
/// 方法查找表（类似 C# 的 VTable）
/// 提供 O(1) 的方法查找性能
/// </summary>
public class MethodTable
{
    /// <summary>
    /// 方法名到方法信息的映射
    /// key: 方法名
    /// value: 方法信息列表（支持重载）
    /// </summary>
    private readonly Dictionary<string, List<LangMethodInfo>> _methodMap = new();

    /// <summary>
    /// 所有方法的列表（用于遍历）
    /// </summary>
    private readonly List<LangMethodInfo> _allMethods = [];

    /// <summary>
    /// 添加方法到查找表
    /// </summary>
    /// <param name="methodInfo">方法信息</param>
    /// <param name="allowOverride">是否允许覆盖父类方法（默认为true）</param>
    public void AddMethod(LangMethodInfo methodInfo, bool allowOverride = true)
    {
        if (!_methodMap.TryGetValue(methodInfo.MethodName, out var value))
        {
            value = [];
            _methodMap[methodInfo.MethodName] = value;
        }

        // 如果允许覆盖，先移除所有同名方法
        if (allowOverride && value.Count > 0)
        {
            // 从 AllMethods 中移除旧方法
            foreach (var oldMethod in value.ToList())
            {
                _allMethods.Remove(oldMethod);
            }
            // 清空当前方法列表
            value.Clear();
        }

        value.Add(methodInfo);
        _allMethods.Add(methodInfo);
    }

    /// <summary>
    /// 根据方法名查找方法（支持重载）
    /// 复杂度：O(1)
    /// </summary>
    public List<LangMethodInfo>? LookupMethod(string methodName)
    {
        return _methodMap.GetValueOrDefault(methodName);
    }

    /// <summary>
    /// 查找单个方法（不支持重载，如果有多个重载会返回第一个）
    /// </summary>
    public LangMethodInfo? LookupSingleMethod(string methodName)
    {
        if (_methodMap.TryGetValue(methodName, out var methods) && methods.Count > 0)
        {
            return methods[0];
        }

        return null;
    }

    /// <summary>
    /// 检查是否包含指定方法
    /// </summary>
    public bool ContainsMethod(string methodName)
    {
        return _methodMap.ContainsKey(methodName);
    }

    /// <summary>
    /// 获取所有方法
    /// </summary>
    public IReadOnlyList<LangMethodInfo> GetAllMethods()
    {
        return _allMethods.AsReadOnly();
    }

    /// <summary>
    /// 获取所有方法名列表
    /// </summary>
    public List<string> GetAllMethodNames()
    {
        return _methodMap.Keys.ToList();
    }

    /// <summary>
    /// 查找单个方法（用于反射）
    /// </summary>
    public LangMethodInfo? FindMethod(string methodName)
    {
        return LookupSingleMethod(methodName);
    }

    /// <summary>
    /// 合并另一个方法表（用于继承）
    /// </summary>
    /// <param name="parentTable">父类的方法表</param>
    /// <param name="allowOverride">是否允许子类方法覆盖父类方法</param>
    public void MergeFrom(MethodTable parentTable, bool allowOverride = true)
    {
        foreach (var method in parentTable._allMethods)
        {
            // 如果当前表中已经有同名方法，根据 allowOverride 决定是否覆盖
            if (_methodMap.ContainsKey(method.MethodName) && !allowOverride)
            {
                continue; // 子类方法优先，不添加父类的同名方法
            }

            // 如果允许覆盖或当前没有同名方法，添加父类方法
            if (!_methodMap.ContainsKey(method.MethodName))
            {
                AddMethod(method);
            }
        }
    }

    /// <summary>
    /// 获取方法数量
    /// </summary>
    public int Count => _allMethods.Count;
}

/// <summary>
/// 方法信息
/// 存储方法的元数据和实现
/// </summary>
public class LangMethodInfo(
    string methodName,
    FuncLangValue implementation,
    HashSet<AccessModifierType>? modifiers = null,
    bool isStatic = false,
    bool isVirtual = false,
    bool isAbstract = false,
    string? originClassName = null)
{
    /// <summary>
    /// 方法名
    /// </summary>
    public string MethodName { get; } = methodName;

    /// <summary>
    /// 方法实现（FuncLangValue）
    /// </summary>
    public FuncLangValue Implementation { get; } = implementation;

    /// <summary>
    /// 访问修饰符
    /// </summary>
    public HashSet<AccessModifierType> Modifiers { get; } = modifiers ?? [];

    /// <summary>
    /// 是否为静态方法
    /// </summary>
    public bool IsStatic { get; } = isStatic;

    /// <summary>
    /// 是否为虚方法（可被重写）
    /// </summary>
    public bool IsVirtual { get; } = isVirtual;

    /// <summary>
    /// 是否为抽象方法
    /// </summary>
    public bool IsAbstract { get; } = isAbstract;

    /// <summary>
    /// 参数数量（用于重载解析）
    /// </summary>
    public int ParameterCount => Implementation.Ids?.Count ?? 0;

    /// <summary>
    /// 方法来源类名（用于追踪继承）
    /// </summary>
    public string? OriginClassName { get; } = originClassName;

    /// <summary>
    /// 检查是否有指定修饰符
    /// </summary>
    public bool HasModifier(AccessModifierType modifier)
    {
        return Modifiers.Contains(modifier);
    }

    /// <summary>
    /// 检查访问权限
    /// </summary>
    public bool IsAccessibleFrom(bool isInternalAccess)
    {
        bool isPrivate = HasModifier(AccessModifierType.Private);
        bool isProtected = HasModifier(AccessModifierType.Protected);

        // 私有成员只能在类内部访问
        if (isPrivate && !isInternalAccess)
            return false;

        // 受保护成员只能在类内部访问
        if (isProtected && !isInternalAccess)
            return false;

        // 公开成员都可以访问
        return true;
    }
}