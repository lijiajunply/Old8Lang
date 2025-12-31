using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Old8Lang.Utilities;

/// <summary>
/// 属性访问委托缓存
/// 使用 Expression.Lambda 编译 PropertyInfo 的 Get/Set 操作为委托，显著提升属性访问性能
/// </summary>
public static class PropertyAccessorCache
{
    // 属性 Getter 缓存: PropertyInfo -> Func<object?, object?>
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object?, object?>> GetterCache = new();

    // 属性 Setter 缓存: PropertyInfo -> Action<object?, object?>
    private static readonly ConcurrentDictionary<PropertyInfo, Action<object?, object?>> SetterCache = new();

    /// <summary>
    /// 获取缓存的 Getter 数量
    /// </summary>
    public static int GetterCacheCount => GetterCache.Count;

    /// <summary>
    /// 获取缓存的 Setter 数量
    /// </summary>
    public static int SetterCacheCount => SetterCache.Count;

    /// <summary>
    /// 使用编译委托获取属性值（替代 PropertyInfo.GetValue）
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <param name="instance">实例对象（静态属性传 null）</param>
    /// <returns>属性值</returns>
    public static object? GetValue(PropertyInfo property, object? instance)
    {
        var getter = GetterCache.GetOrAdd(property, CompileGetter);
        return getter(instance);
    }

    /// <summary>
    /// 编译属性 Getter 为委托
    /// </summary>
    private static Func<object?, object?> CompileGetter(PropertyInfo property)
    {
        if (!property.CanRead)
        {
            throw new InvalidOperationException($"属性 {property.Name} 不可读");
        }

        var getMethod = property.GetGetMethod(true);
        if (getMethod == null)
        {
            throw new InvalidOperationException($"无法获取属性 {property.Name} 的 Get 方法");
        }

        // 参数: object? instance
        var instanceParam = Expression.Parameter(typeof(object), "instance");

        // 转换实例类型: (TDeclaringType)instance
        var instanceCast = property.DeclaringType!.IsValueType
            ? Expression.Convert(instanceParam, property.DeclaringType)
            : Expression.TypeAs(instanceParam, property.DeclaringType);

        // 调用 Get 方法: instance.Property
        Expression propertyAccess = getMethod.IsStatic
            ? Expression.Property(null, property)
            : Expression.Property(instanceCast, property);

        // 转换返回值为 object?: (object?)result
        var resultCast = Expression.Convert(propertyAccess, typeof(object));

        // 编译 Lambda: (object? instance) => (object?)instance.Property
        var lambda = Expression.Lambda<Func<object?, object?>>(resultCast, instanceParam);
        return lambda.Compile();
    }

    /// <summary>
    /// 使用编译委托设置属性值（替代 PropertyInfo.SetValue）
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <param name="instance">实例对象（静态属性传 null）</param>
    /// <param name="value">要设置的值</param>
    public static void SetValue(PropertyInfo property, object? instance, object? value)
    {
        var setter = SetterCache.GetOrAdd(property, CompileSetter);
        setter(instance, value);
    }

    /// <summary>
    /// 编译属性 Setter 为委托
    /// </summary>
    private static Action<object?, object?> CompileSetter(PropertyInfo property)
    {
        if (!property.CanWrite)
        {
            throw new InvalidOperationException($"属性 {property.Name} 不可写");
        }

        var setMethod = property.GetSetMethod(true);
        if (setMethod == null)
        {
            throw new InvalidOperationException($"无法获取属性 {property.Name} 的 Set 方法");
        }

        // 参数: object? instance, object? value
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var valueParam = Expression.Parameter(typeof(object), "value");

        // 转换实例类型: (TDeclaringType)instance
        var instanceCast = property.DeclaringType!.IsValueType
            ? Expression.Convert(instanceParam, property.DeclaringType)
            : Expression.TypeAs(instanceParam, property.DeclaringType);

        // 转换值类型: (TPropertyType)value
        var valueCast = Expression.Convert(valueParam, property.PropertyType);

        // 调用 Set 方法: instance.Property = value
        Expression propertyAssign = setMethod.IsStatic
            ? Expression.Call(setMethod, valueCast)
            : Expression.Call(instanceCast, setMethod, valueCast);

        // 编译 Lambda: (object? instance, object? value) => instance.Property = value
        var lambda = Expression.Lambda<Action<object?, object?>>(propertyAssign, instanceParam, valueParam);
        return lambda.Compile();
    }
}
