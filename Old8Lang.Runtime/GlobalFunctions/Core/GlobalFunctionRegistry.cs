using Old8Lang.AST;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.GlobalFunctions.Core;

/// <summary>
/// 全局函数注册器 - 单例模式，管理所有全局函数的注册和查找
/// 支持函数重载（同名不同参数签名）
/// </summary>
public sealed class GlobalFunctionRegistry
{
    private static readonly Lazy<GlobalFunctionRegistry> _instance =
        new(() => new GlobalFunctionRegistry());

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static GlobalFunctionRegistry Instance => _instance.Value;

    /// <summary>
    /// 函数名称到重载组的映射（不区分大小写）
    /// </summary>
    private readonly Dictionary<string, OverloadGroup> _overloadGroups =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 注册锁，确保线程安全
    /// </summary>
    private readonly Lock _registerLock = new();

    /// <summary>
    /// 私有构造函数，防止外部实例化
    /// </summary>
    private GlobalFunctionRegistry()
    {
        // 延迟注册，在首次使用时自动注册所有内置函数
    }

    /// <summary>
    /// 注册一个全局函数
    /// </summary>
    /// <param name="function">要注册的函数</param>
    public void Register(IGlobalFunction function)
    {
        lock (_registerLock)
        {
            // 为每个别名注册到对应的重载组
            foreach (var name in function.Names)
            {
                if (!_overloadGroups.TryGetValue(name, out var group))
                {
                    group = new OverloadGroup(name);
                    _overloadGroups[name] = group;
                }

                group.AddOverload(function);
            }
        }
    }

    /// <summary>
    /// 批量注册多个全局函数
    /// </summary>
    /// <param name="functions">要注册的函数列表</param>
    public void RegisterRange(params IGlobalFunction[] functions)
    {
        foreach (var function in functions)
        {
            Register(function);
        }
    }

    /// <summary>
    /// 根据参数解析最匹配的函数重载
    /// </summary>
    /// <param name="name">函数名称</param>
    /// <param name="parameters">参数表达式列表</param>
    /// <param name="local">局部变量管理器（可为 null）</param>
    /// <returns>最匹配的函数，如果不存在返回 null</returns>
    public IGlobalFunction? ResolveFunction(string name, List<LangExpression> parameters, LocalManager? local)
    {
        lock (_registerLock)
        {
            if (!_overloadGroups.TryGetValue(name, out var group))
                return null;

            return group.ResolveOverload(parameters, local);
        }
    }

    /// <summary>
    /// 查找全局函数（向后兼容，返回第一个重载）
    /// </summary>
    /// <param name="name">函数名称</param>
    /// <returns>找到的函数，如果不存在返回 null</returns>
    public IGlobalFunction? TryGetFunction(string name)
    {
        lock (_registerLock)
        {
            if (!_overloadGroups.TryGetValue(name, out var group))
                return null;

            return group.Overloads.FirstOrDefault();
        }
    }

    /// <summary>
    /// 获取指定名称的重载组
    /// </summary>
    /// <param name="name">函数名称</param>
    /// <returns>重载组，如果不存在返回 null</returns>
    public OverloadGroup? GetOverloadGroup(string name)
    {
        lock (_registerLock)
        {
            _overloadGroups.TryGetValue(name, out var group);
            return group;
        }
    }

    /// <summary>
    /// 检查是否存在指定名称的全局函数
    /// </summary>
    /// <param name="name">函数名称</param>
    /// <returns>如果存在返回 true，否则返回 false</returns>
    public bool HasFunction(string name)
    {
        lock (_registerLock)
        {
            return _overloadGroups.ContainsKey(name);
        }
    }

    /// <summary>
    /// 获取所有已注册的函数名称
    /// </summary>
    /// <returns>函数名称列表</returns>
    public IEnumerable<string> GetAllFunctionNames()
    {
        lock (_registerLock)
        {
            return _overloadGroups.Keys.ToList();
        }
    }

    /// <summary>
    /// 获取所有重载组
    /// </summary>
    /// <returns>重载组列表</returns>
    public IEnumerable<OverloadGroup> GetAllOverloadGroups()
    {
        lock (_registerLock)
        {
            return _overloadGroups.Values.ToList();
        }
    }

    /// <summary>
    /// 清空所有注册的函数（主要用于测试）
    /// </summary>
    public void Clear()
    {
        lock (_registerLock)
        {
            _overloadGroups.Clear();
        }
    }

    /// <summary>
    /// 注销指定名称的函数（移除所有重载）
    /// </summary>
    /// <param name="name">函数名称</param>
    /// <returns>如果成功注销返回 true，否则返回 false</returns>
    public bool Unregister(string name)
    {
        lock (_registerLock)
        {
            if (!_overloadGroups.TryGetValue(name, out var group))
                return false;

            // 获取该重载组中所有函数的所有别名
            var allNames = group.Overloads
                .SelectMany(f => f.Names)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // 移除所有相关的重载组
            foreach (var alias in allNames)
            {
                _overloadGroups.Remove(alias);
            }

            return true;
        }
    }

    /// <summary>
    /// 获取指定函数名称的重载数量
    /// </summary>
    /// <param name="name">函数名称</param>
    /// <returns>重载数量，如果函数不存在返回 0</returns>
    public int GetOverloadCount(string name)
    {
        lock (_registerLock)
        {
            if (!_overloadGroups.TryGetValue(name, out var group))
                return 0;

            return group.Overloads.Count;
        }
    }
}
