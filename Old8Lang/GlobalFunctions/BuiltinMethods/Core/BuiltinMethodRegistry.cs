using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.GlobalFunctions.Core;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.Core;

/// <summary>
/// 内置方法注册表 - 单例模式，管理基本类型方法到全局函数的映射
/// </summary>
/// <remarks>
/// 该注册表用于将基本类型的方法（如 list.Add(), string.Length() 等）
/// 映射到对应的全局函数实现，实现解释器、编译器、类型推断三个模式的统一处理。
/// </remarks>
public sealed class BuiltinMethodRegistry
{
    private static readonly Lazy<BuiltinMethodRegistry> _instance =
        new(() => new BuiltinMethodRegistry());

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static BuiltinMethodRegistry Instance => _instance.Value;

    /// <summary>
    /// 映射: (解释器类型, 方法名) -> 全局函数
    /// </summary>
    private readonly Dictionary<(Type, string), IGlobalFunction> _interpreterMethods = new();

    /// <summary>
    /// 映射: (编译器类型, 方法名) -> 全局函数
    /// </summary>
    private readonly Dictionary<(Type, string), IGlobalFunction> _compilerMethods = new();

    /// <summary>
    /// 注册锁，确保线程安全
    /// </summary>
    private readonly Lock _registerLock = new();

    /// <summary>
    /// 私有构造函数，防止外部实例化
    /// </summary>
    private BuiltinMethodRegistry()
    {
    }

    /// <summary>
    /// 注册一个内置方法（同时注册解释器类型和编译器类型）
    /// </summary>
    /// <param name="interpreterType">解释器模式下的类型（如 ListLangValue）</param>
    /// <param name="compilerType">编译器模式下的类型（如 List&lt;object?&gt;）</param>
    /// <param name="methodName">方法名称</param>
    /// <param name="function">全局函数实现</param>
    public void Register(Type interpreterType, Type compilerType, string methodName, IGlobalFunction function)
    {
        lock (_registerLock)
        {
            _interpreterMethods[(interpreterType, methodName)] = function;
            _compilerMethods[(compilerType, methodName)] = function;
        }
    }

    /// <summary>
    /// 注册一个内置方法（仅解释器类型）
    /// </summary>
    /// <param name="interpreterType">解释器模式下的类型</param>
    /// <param name="methodName">方法名称</param>
    /// <param name="function">全局函数实现</param>
    public void RegisterInterpreter(Type interpreterType, string methodName, IGlobalFunction function)
    {
        lock (_registerLock)
        {
            _interpreterMethods[(interpreterType, methodName)] = function;
        }
    }

    /// <summary>
    /// 注册一个内置方法（仅编译器类型）
    /// </summary>
    /// <param name="compilerType">编译器模式下的类型</param>
    /// <param name="methodName">方法名称</param>
    /// <param name="function">全局函数实现</param>
    public void RegisterCompiler(Type compilerType, string methodName, IGlobalFunction function)
    {
        lock (_registerLock)
        {
            _compilerMethods[(compilerType, methodName)] = function;
        }
    }

    /// <summary>
    /// 查找内置方法（解释器模式）
    /// </summary>
    /// <param name="valueType">值类型（LangValueType 的子类）</param>
    /// <param name="methodName">方法名称</param>
    /// <returns>找到的全局函数，如果不存在返回 null</returns>
    public IGlobalFunction? TryGetMethod(Type valueType, string methodName)
    {
        lock (_registerLock)
        {
            _interpreterMethods.TryGetValue((valueType, methodName), out var function);
            return function;
        }
    }

    /// <summary>
    /// 查找内置方法（解释器模式，通过 LangValueType 实例）
    /// </summary>
    /// <param name="value">LangValueType 实例</param>
    /// <param name="methodName">方法名称</param>
    /// <returns>找到的全局函数，如果不存在返回 null</returns>
    public IGlobalFunction? TryGetMethod(LangValueType value, string methodName)
    {
        return TryGetMethod(value.GetType(), methodName);
    }

    /// <summary>
    /// 查找内置方法（编译器模式）
    /// </summary>
    /// <param name="compilerType">编译器类型（.NET 类型）</param>
    /// <param name="methodName">方法名称</param>
    /// <returns>找到的全局函数，如果不存在返回 null</returns>
    public IGlobalFunction? TryGetMethodForCompiler(Type compilerType, string methodName)
    {
        lock (_registerLock)
        {
            // 首先尝试精确匹配
            if (_compilerMethods.TryGetValue((compilerType, methodName), out var function))
            {
                return function;
            }

            // 对于泛型类型，尝试匹配泛型定义
            if (compilerType.IsGenericType)
            {
                var genericDef = compilerType.GetGenericTypeDefinition();
                foreach (var ((type, name), func) in _compilerMethods)
                {
                    if (name != methodName) continue;

                    // 检查是否是相同的泛型定义
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == genericDef)
                    {
                        return func;
                    }

                    // 检查是否是泛型定义本身
                    if (type == genericDef)
                    {
                        return func;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// 检查是否存在指定的内置方法（解释器模式）
    /// </summary>
    /// <param name="valueType">值类型</param>
    /// <param name="methodName">方法名称</param>
    /// <returns>如果存在返回 true，否则返回 false</returns>
    public bool HasMethod(Type valueType, string methodName)
    {
        lock (_registerLock)
        {
            return _interpreterMethods.ContainsKey((valueType, methodName));
        }
    }

    /// <summary>
    /// 检查是否存在指定的内置方法（编译器模式）
    /// </summary>
    /// <param name="compilerType">编译器类型</param>
    /// <param name="methodName">方法名称</param>
    /// <returns>如果存在返回 true，否则返回 false</returns>
    public bool HasMethodForCompiler(Type compilerType, string methodName)
    {
        return TryGetMethodForCompiler(compilerType, methodName) is not null;
    }

    /// <summary>
    /// 获取所有已注册的方法名称（解释器模式）
    /// </summary>
    /// <returns>方法名称列表</returns>
    public IEnumerable<(Type Type, string MethodName)> GetAllInterpreterMethods()
    {
        lock (_registerLock)
        {
            return _interpreterMethods.Keys.ToList();
        }
    }

    /// <summary>
    /// 获取所有已注册的方法名称（编译器模式）
    /// </summary>
    /// <returns>方法名称列表</returns>
    public IEnumerable<(Type Type, string MethodName)> GetAllCompilerMethods()
    {
        lock (_registerLock)
        {
            return _compilerMethods.Keys.ToList();
        }
    }

    /// <summary>
    /// 清空所有注册的方法（主要用于测试）
    /// </summary>
    public void Clear()
    {
        lock (_registerLock)
        {
            _interpreterMethods.Clear();
            _compilerMethods.Clear();
        }
    }
}
