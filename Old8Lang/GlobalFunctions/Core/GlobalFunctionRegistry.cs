using Old8Lang.Error;
using Old8Lang.AST;

namespace Old8Lang.GlobalFunctions.Core;

/// <summary>
/// 全局函数注册器 - 单例模式，管理所有全局函数的注册和查找
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
    /// 函数名称到函数实现的映射（不区分大小写）
    /// </summary>
    private readonly Dictionary<string, IGlobalFunction> _functions =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 注册锁，确保线程安全
    /// </summary>
    private readonly object _registerLock = new();

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
            // 检查是否已经注册过这个函数对象
            foreach (var existingFunc in _functions.Values.Distinct())
            {
                if (ReferenceEquals(existingFunc, function))
                {
                    // 函数已经注册过，跳过
                    return;
                }
            }

            // 注册所有别名
            foreach (var name in function.Names)
            {
                _functions[name] = function;
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
    /// 查找全局函数
    /// </summary>
    /// <param name="name">函数名称</param>
    /// <returns>找到的函数，如果不存在返回 null</returns>
    public IGlobalFunction? TryGetFunction(string name)
    {
        _functions.TryGetValue(name, out var function);
        return function;
    }

    /// <summary>
    /// 检查是否存在指定名称的全局函数
    /// </summary>
    /// <param name="name">函数名称</param>
    /// <returns>如果存在返回 true，否则返回 false</returns>
    public bool HasFunction(string name)
    {
        return _functions.ContainsKey(name);
    }

    /// <summary>
    /// 获取所有已注册的函数名称
    /// </summary>
    /// <returns>函数名称列表</returns>
    public IEnumerable<string> GetAllFunctionNames()
    {
        return _functions.Keys.Distinct();
    }

    /// <summary>
    /// 清空所有注册的函数（主要用于测试）
    /// </summary>
    public void Clear()
    {
        lock (_registerLock)
        {
            _functions.Clear();
        }
    }

    /// <summary>
    /// 注销指定名称的函数
    /// </summary>
    /// <param name="name">函数名称</param>
    /// <returns>如果成功注销返回 true，否则返回 false</returns>
    public bool Unregister(string name)
    {
        lock (_registerLock)
        {
            if (_functions.TryGetValue(name, out var function))
            {
                // 移除该函数的所有别名
                foreach (var alias in function.Names)
                {
                    _functions.Remove(alias);
                }
                return true;
            }
            return false;
        }
    }
}
