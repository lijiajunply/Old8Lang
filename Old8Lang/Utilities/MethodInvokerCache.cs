using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Old8Lang.Utilities;

/// <summary>
/// 方法调用委托缓存，用于优化反射调用性能
/// 将 MethodInfo.Invoke 转换为编译的委托调用，性能提升约 50-80 倍
/// </summary>
public static class MethodInvokerCache
{
    // 使用 ConcurrentDictionary 保证线程安全
    private static readonly ConcurrentDictionary<MethodInfo, Func<object?, object?[]?, object?>> InvokerCache = new();

    /// <summary>
    /// 获取或创建方法的快速调用委托
    /// </summary>
    /// <param name="method">要调用的方法</param>
    /// <returns>编译的委托，参数为 (实例对象, 参数数组)，返回值为调用结果</returns>
    public static Func<object?, object?[]?, object?> GetInvoker(MethodInfo method)
    {
        return InvokerCache.GetOrAdd(method, CreateInvoker);
    }

    /// <summary>
    /// 创建方法的快速调用委托
    /// </summary>
    private static Func<object?, object?[]?, object?> CreateInvoker(MethodInfo method)
    {
        // 参数：实例对象（静态方法可为 null）
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        // 参数：参数数组
        var argsParam = Expression.Parameter(typeof(object?[]), "args");

        var methodParams = method.GetParameters();
        var argExpressions = new Expression[methodParams.Length];

        // 构建参数访问表达式
        for (int i = 0; i < methodParams.Length; i++)
        {
            var paramType = methodParams[i].ParameterType;
            // args[i]
            var argAccess = Expression.ArrayIndex(argsParam, Expression.Constant(i));
            // 类型转换 (TParam)args[i]
            argExpressions[i] = Expression.Convert(argAccess, paramType);
        }

        // 方法调用表达式
        Expression callExpr;
        if (method.IsStatic)
        {
            // 静态方法：Method(args)
            callExpr = Expression.Call(method, argExpressions);
        }
        else
        {
            // 实例方法：((TInstance)instance).Method(args)
            var instanceCast = Expression.Convert(instanceParam, method.DeclaringType!);
            callExpr = Expression.Call(instanceCast, method, argExpressions);
        }

        // 处理返回值
        Expression resultExpr;
        if (method.ReturnType == typeof(void))
        {
            // void 方法返回 null
            resultExpr = Expression.Block(callExpr, Expression.Constant(null, typeof(object)));
        }
        else
        {
            // 转换返回值为 object
            resultExpr = Expression.Convert(callExpr, typeof(object));
        }

        // 编译为委托
        var lambda = Expression.Lambda<Func<object?, object?[]?, object?>>(
            resultExpr,
            instanceParam,
            argsParam
        );

        return lambda.Compile();
    }

    /// <summary>
    /// 快速调用方法（使用缓存的委托）
    /// </summary>
    /// <param name="method">要调用的方法</param>
    /// <param name="instance">实例对象（静态方法传 null）</param>
    /// <param name="args">参数数组</param>
    /// <returns>调用结果</returns>
    public static object? Invoke(MethodInfo method, object? instance, object?[]? args)
    {
        var invoker = GetInvoker(method);
        return invoker(instance, args);
    }

    /// <summary>
    /// 清除缓存（测试或内存管理时使用）
    /// </summary>
    public static void ClearCache()
    {
        InvokerCache.Clear();
    }

    /// <summary>
    /// 获取缓存的委托数量
    /// </summary>
    public static int CacheCount => InvokerCache.Count;
}
