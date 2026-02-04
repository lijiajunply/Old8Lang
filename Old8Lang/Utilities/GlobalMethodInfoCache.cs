using System.Collections.Concurrent;
using System.Reflection;

namespace Old8Lang.Utilities;

/// <summary>
/// 全局方法信息缓存，用于优化 IL 生成时的反射调用性能
/// 缓存 MethodInfo 和 PropertyInfo，避免重复的 GetMethod/GetProperty 调用
/// </summary>
public static class GlobalMethodInfoCache
{
    // 方法缓存：key = "TypeFullName.MethodName" 或 "TypeFullName.MethodName(ParamType1,ParamType2)"
    private static readonly ConcurrentDictionary<string, MethodInfo> MethodCache = new();

    // 属性 Getter 缓存：key = "TypeFullName.PropertyName"
    private static readonly ConcurrentDictionary<string, MethodInfo> PropertyGetterCache = new();

    // 属性 Setter 缓存：key = "TypeFullName.PropertyName"
    private static readonly ConcurrentDictionary<string, MethodInfo> PropertySetterCache = new();

    /// <summary>
    /// 获取方法信息（无参数类型）
    /// </summary>
    /// <param name="type">包含方法的类型</param>
    /// <param name="methodName">方法名称</param>
    /// <returns>方法信息，如果找不到则返回 null</returns>
    public static MethodInfo GetMethod(Type type, string methodName)
    {
        var key = $"{type.FullName}.{methodName}";
        return MethodCache.GetOrAdd(key, _ => type.GetMethod(methodName)!);
    }

    /// <summary>
    /// 获取方法信息（带参数类型）
    /// </summary>
    /// <param name="type">包含方法的类型</param>
    /// <param name="methodName">方法名称</param>
    /// <param name="parameterTypes">参数类型数组</param>
    /// <returns>方法信息，如果找不到则返回 null</returns>
    public static MethodInfo GetMethod(Type type, string methodName, Type[] parameterTypes)
    {
        var paramTypesStr = string.Join(",", parameterTypes.Select(t => t.FullName ?? t.Name));
        var key = $"{type.FullName}.{methodName}({paramTypesStr})";
        return MethodCache.GetOrAdd(key, _ => type.GetMethod(methodName, parameterTypes)!);
    }

    /// <summary>
    /// 获取方法信息（带绑定标志）
    /// </summary>
    /// <param name="type">包含方法的类型</param>
    /// <param name="methodName">方法名称</param>
    /// <param name="bindingFlags">绑定标志</param>
    /// <returns>方法信息，如果找不到则返回 null</returns>
    public static MethodInfo GetMethod(Type type, string methodName, BindingFlags bindingFlags)
    {
        var key = $"{type.FullName}.{methodName}#{(int)bindingFlags}";
        return MethodCache.GetOrAdd(key, _ => type.GetMethod(methodName, bindingFlags)!);
    }

    /// <summary>
    /// 获取方法信息（带绑定标志和参数类型）
    /// </summary>
    /// <param name="type">包含方法的类型</param>
    /// <param name="methodName">方法名称</param>
    /// <param name="bindingFlags">绑定标志</param>
    /// <param name="parameterTypes">参数类型数组</param>
    /// <returns>方法信息，如果找不到则返回 null</returns>
    public static MethodInfo GetMethod(Type type, string methodName, BindingFlags bindingFlags, Type[] parameterTypes)
    {
        var paramTypesStr = string.Join(",", parameterTypes.Select(t => t.FullName ?? t.Name));
        var key = $"{type.FullName}.{methodName}#{(int)bindingFlags}({paramTypesStr})";
        return MethodCache.GetOrAdd(key, _ => type.GetMethod(methodName, bindingFlags, null, parameterTypes, null)!);
    }

    /// <summary>
    /// 获取属性的 Getter 方法
    /// </summary>
    /// <param name="type">包含属性的类型</param>
    /// <param name="propertyName">属性名称</param>
    /// <returns>Getter 方法信息，如果找不到则返回 null</returns>
    public static MethodInfo GetPropertyGetter(Type type, string propertyName)
    {
        var key = $"{type.FullName}.{propertyName}";
        return PropertyGetterCache.GetOrAdd(key, _ =>
        {
            var property = type.GetProperty(propertyName);
            return property?.GetGetMethod()!;
        });
    }

    /// <summary>
    /// 获取属性的 Setter 方法
    /// </summary>
    /// <param name="type">包含属性的类型</param>
    /// <param name="propertyName">属性名称</param>
    /// <returns>Setter 方法信息，如果找不到则返回 null</returns>
    public static MethodInfo GetPropertySetter(Type type, string propertyName)
    {
        var key = $"{type.FullName}.{propertyName}";
        return PropertySetterCache.GetOrAdd(key, _ =>
        {
            var property = type.GetProperty(propertyName);
            return property?.GetSetMethod()!;
        });
    }

    /// <summary>
    /// 清除所有缓存（测试或内存管理时使用）
    /// </summary>
    public static void ClearCache()
    {
        MethodCache.Clear();
        PropertyGetterCache.Clear();
        PropertySetterCache.Clear();
    }

    /// <summary>
    /// 获取缓存的方法数量
    /// </summary>
    public static int MethodCacheCount => MethodCache.Count;

    /// <summary>
    /// 获取缓存的属性 Getter 数量
    /// </summary>
    public static int PropertyGetterCacheCount => PropertyGetterCache.Count;

    /// <summary>
    /// 获取缓存的属性 Setter 数量
    /// </summary>
    public static int PropertySetterCacheCount => PropertySetterCache.Count;

    /// <summary>
    /// 获取总缓存数量
    /// </summary>
    public static int TotalCacheCount => MethodCacheCount + PropertyGetterCacheCount + PropertySetterCacheCount;
}
