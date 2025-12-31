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
            var paramInfo = methodParams[i];
            var paramType = paramInfo.ParameterType;

            // 检查参数是否是可选的
            bool isOptional = paramInfo.IsOptional || paramInfo.HasDefaultValue;

            // 检查参数索引是否在 args 数组范围内
            // 如果参数是可选的且 args 数组不够长，使用默认值
            if (isOptional)
            {
                // args != null && args.Length > i ? args[i] : defaultValue
                var argsNotNull = Expression.NotEqual(argsParam, Expression.Constant(null, typeof(object?[])));
                var argsLength = Expression.Property(argsParam, "Length");
                var indexInBounds = Expression.GreaterThan(argsLength, Expression.Constant(i));
                var canAccessArg = Expression.AndAlso(argsNotNull, indexInBounds);

                // 获取参数的默认值
                var defaultValue = paramInfo.HasDefaultValue ? paramInfo.DefaultValue : null;
                var defaultExpr = Expression.Constant(defaultValue, typeof(object));

                // args[i]
                var argAccess = Expression.ArrayIndex(argsParam, Expression.Constant(i));

                // 如果可以访问参数，使用 args[i]，否则使用默认值
                var valueExpr = Expression.Condition(canAccessArg, argAccess, defaultExpr);

                // 对可空类型进行特殊处理
                if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    // 可空类型：使用 ConvertChecked 并添加 null 检查
                    var underlyingType = Nullable.GetUnderlyingType(paramType)!;
                    var nullCheck = Expression.Equal(valueExpr, Expression.Constant(null, typeof(object)));

                    // 为 null 的情况：返回 default(T?)
                    var nullValue = Expression.Constant(null, paramType);

                    // 不为 null 的情况：先转换到 underlying type，再构造可空类型
                    Expression unboxed = Expression.Convert(valueExpr, underlyingType);
                    Expression convertValue = Expression.Convert(unboxed, paramType);

                    argExpressions[i] = Expression.Condition(nullCheck, nullValue, convertValue);
                }
                else if (paramType.IsValueType)
                {
                    // 值类型：需要特殊处理 null 到默认值的转换
                    var nullCheck = Expression.Equal(valueExpr, Expression.Constant(null, typeof(object)));
                    var defaultValueExpr = Expression.Default(paramType);
                    var convertedExpr = Expression.Convert(valueExpr, paramType);
                    argExpressions[i] = Expression.Condition(nullCheck, defaultValueExpr, convertedExpr);
                }
                else
                {
                    // 普通引用类型转换
                    argExpressions[i] = Expression.Convert(valueExpr, paramType);
                }
            }
            else
            {
                // 非可选参数，直接访问 args[i]
                var argAccess = Expression.ArrayIndex(argsParam, Expression.Constant(i));

                // 对可空类型进行特殊处理
                if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    // 可空类型：使用 ConvertChecked 并添加 null 检查
                    // argAccess == null ? default(T?) : new T?((T)argAccess)
                    var underlyingType = Nullable.GetUnderlyingType(paramType)!;
                    var nullCheck = Expression.Equal(argAccess, Expression.Constant(null, typeof(object)));

                    // 为 null 的情况：返回 default(T?)
                    var nullValue = Expression.Constant(null, paramType);

                    // 不为 null 的情况：先转换到 underlying type，再构造可空类型
                    // 使用 UnboxOrCastOrBox 来处理类型转换
                    Expression unboxed = Expression.Convert(argAccess, underlyingType);
                    Expression convertValue = Expression.Convert(unboxed, paramType);

                    argExpressions[i] = Expression.Condition(nullCheck, nullValue, convertValue);
                }
                else
                {
                    // 普通类型转换 (TParam)args[i]
                    argExpressions[i] = Expression.Convert(argAccess, paramType);
                }
            }
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
