using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.InstanceMethods.Core;

/// <summary>
/// 实例方法注册器 - 单例模式，管理所有实例方法的注册和查找
/// </summary>
public sealed class InstanceMethodRegistry
{
    private static readonly Lazy<InstanceMethodRegistry> _instance =
        new(() => new InstanceMethodRegistry());

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static InstanceMethodRegistry Instance => _instance.Value;

    /// <summary>
    /// 按类型组织的方法映射：Type -> (MethodName -> InstanceMethodOverloadGroup)
    /// 方法名称不区分大小写
    /// </summary>
    private readonly Dictionary<Type, Dictionary<string, InstanceMethodOverloadGroup>> _methodsByType = new();

    /// <summary>
    /// 注册锁，确保线程安全
    /// </summary>
    private readonly Lock _registerLock = new();

    /// <summary>
    /// 私有构造函数，防止外部实例化
    /// </summary>
    private InstanceMethodRegistry()
    {
        // 延迟注册，在首次使用时自动注册所有内置方法
    }

    /// <summary>
    /// 注册一个实例方法
    /// </summary>
    /// <param name="method">要注册的方法</param>
    public void Register(IInstanceMethod method)
    {
        lock (_registerLock)
        {
            // 获取或创建该类型的方法字典
            if (!_methodsByType.TryGetValue(method.TargetType, out var methodDict))
            {
                methodDict = new Dictionary<string, InstanceMethodOverloadGroup>(StringComparer.OrdinalIgnoreCase);
                _methodsByType[method.TargetType] = methodDict;
            }

            // 为每个别名注册方法
            foreach (var name in method.Names)
            {
                // 获取或创建重载组
                if (!methodDict.TryGetValue(name, out var overloadGroup))
                {
                    overloadGroup = new InstanceMethodOverloadGroup(name, method.TargetType);
                    methodDict[name] = overloadGroup;
                }

                // 添加到重载组
                overloadGroup.AddOverload(method);
            }
        }
    }

    /// <summary>
    /// 批量注册多个实例方法
    /// </summary>
    /// <param name="methods">要注册的方法列表</param>
    public void RegisterRange(params IInstanceMethod[] methods)
    {
        foreach (var method in methods)
        {
            Register(method);
        }
    }

    /// <summary>
    /// 查找实例方法（支持继承查找）- 向后兼容方法，返回第一个重载
    /// </summary>
    /// <param name="instanceType">实例类型</param>
    /// <param name="methodName">方法名称</param>
    /// <returns>找到的方法，如果不存在返回 null</returns>
    public IInstanceMethod? TryGetMethod(Type instanceType, string methodName)
    {
        var overloadGroup = GetOverloadGroup(instanceType, methodName);
        return overloadGroup?.GetAllOverloads().FirstOrDefault();
    }

    /// <summary>
    /// 获取重载组（支持继承查找）
    /// </summary>
    /// <param name="instanceType">实例类型</param>
    /// <param name="methodName">方法名称</param>
    /// <returns>找到的重载组，如果不存在返回 null</returns>
    public InstanceMethodOverloadGroup? GetOverloadGroup(Type instanceType, string methodName)
    {
        lock (_registerLock)
        {
            // 1. 首先尝试精确匹配
            if (_methodsByType.TryGetValue(instanceType, out var methodDict))
            {
                if (methodDict.TryGetValue(methodName, out var overloadGroup))
                {
                    return overloadGroup;
                }
            }

            // 2. 尝试查找基类和接口的方法
            foreach (var kvp in _methodsByType)
            {
                var registeredType = kvp.Key;

                // 检查 instanceType 是否是 registeredType 的子类或实现
                if (registeredType.IsAssignableFrom(instanceType))
                {
                    if (kvp.Value.TryGetValue(methodName, out var overloadGroup))
                    {
                        return overloadGroup;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// 解析最匹配的重载
    /// </summary>
    /// <param name="instanceType">实例类型</param>
    /// <param name="methodName">方法名称</param>
    /// <param name="parameters">参数表达式列表</param>
    /// <param name="local">局部变量管理器（可选）</param>
    /// <returns>最匹配的方法，如果没有匹配返回 null</returns>
    public IInstanceMethod? ResolveMethod(Type instanceType, string methodName,
        List<LangExpression> parameters, LocalManager? local)
    {
        var overloadGroup = GetOverloadGroup(instanceType, methodName);
        return overloadGroup?.ResolveOverload(parameters, local);
    }

    /// <summary>
    /// 检查是否存在指定类型和名称的实例方法
    /// </summary>
    /// <param name="instanceType">实例类型</param>
    /// <param name="methodName">方法名称</param>
    /// <returns>如果存在返回 true，否则返回 false</returns>
    public bool HasMethod(Type instanceType, string methodName)
    {
        return TryGetMethod(instanceType, methodName) != null;
    }

    /// <summary>
    /// 获取指定类型的所有方法名称
    /// </summary>
    /// <param name="instanceType">实例类型</param>
    /// <returns>方法名称列表</returns>
    public IEnumerable<string> GetMethodNames(Type instanceType)
    {
        lock (_registerLock)
        {
            var methodNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 收集所有适用于该类型的方法名称
            foreach (var kvp in _methodsByType)
            {
                var registeredType = kvp.Key;

                if (registeredType.IsAssignableFrom(instanceType))
                {
                    foreach (var methodName in kvp.Value.Keys)
                    {
                        methodNames.Add(methodName);
                    }
                }
            }

            return methodNames;
        }
    }

    /// <summary>
    /// 获取所有已注册的类型
    /// </summary>
    /// <returns>类型列表</returns>
    public IEnumerable<Type> GetRegisteredTypes()
    {
        lock (_registerLock)
        {
            return _methodsByType.Keys.ToList();
        }
    }

    /// <summary>
    /// 清空所有注册的方法（主要用于测试）
    /// </summary>
    public void Clear()
    {
        lock (_registerLock)
        {
            _methodsByType.Clear();
        }
    }

    /// <summary>
    /// 注销指定类型和名称的方法
    /// </summary>
    /// <param name="instanceType">实例类型</param>
    /// <param name="methodName">方法名称</param>
    /// <returns>如果成功注销返回 true，否则返回 false</returns>
    public bool Unregister(Type instanceType, string methodName)
    {
        lock (_registerLock)
        {
            if (!_methodsByType.TryGetValue(instanceType, out var methodDict))
            {
                return false;
            }

            if (!methodDict.TryGetValue(methodName, out var overloadGroup))
            {
                return false;
            }

            // 移除重载组
            methodDict.Remove(methodName);

            // 如果该类型没有方法了，移除类型条目
            if (methodDict.Count == 0)
            {
                _methodsByType.Remove(instanceType);
            }

            return true;
        }
    }

    /// <summary>
    /// 获取注册的方法总数（去重后）
    /// </summary>
    /// <returns>方法总数</returns>
    public int GetMethodCount()
    {
        lock (_registerLock)
        {
            var uniqueMethods = new HashSet<IInstanceMethod>();
            foreach (var methodDict in _methodsByType.Values)
            {
                foreach (var overloadGroup in methodDict.Values)
                {
                    foreach (var method in overloadGroup.GetAllOverloads())
                    {
                        uniqueMethods.Add(method);
                    }
                }
            }
            return uniqueMethods.Count;
        }
    }
}
