using System.Reflection.Emit;

namespace Old8Lang.Compiler.CodeGeneration;

/// <summary>
/// IL 代码生成缓存，用于优化重复 IL 代码生成
/// 缓存常用的 IL 模式，避免重复生成
/// </summary>
public class ILGenerationCache
{
    /// <summary>
    /// MethodInfo 缓存（方法反射信息）
    /// Key: 方法签名字符串，Value: MethodInfo
    /// </summary>
    private readonly Dictionary<string, System.Reflection.MethodInfo> _methodInfoCache = new();

    /// <summary>
    /// ConstructorInfo 缓存（构造函数反射信息）
    /// Key: 构造函数签名字符串，Value: ConstructorInfo
    /// </summary>
    private readonly Dictionary<string, System.Reflection.ConstructorInfo> _constructorInfoCache = new();

    /// <summary>
    /// OpCode 序列缓存（常用 IL 指令序列）
    /// Key: 序列名称，Value: OpCode 列表
    /// </summary>
    private readonly Dictionary<string, List<OpCode>> _opCodeSequenceCache = new();

    /// <summary>
    /// 类型转换 IL 模式缓存
    /// Key: "源类型->目标类型"，Value: 生成 IL 的委托
    /// </summary>
    private readonly Dictionary<string, Action<ILGenerator>> _typeConversionCache = new();

    /// <summary>
    /// 静态构造函数，初始化常用 IL 模式
    /// </summary>
    static ILGenerationCache()
    {
    }

    /// <summary>
    /// 获取或缓存 MethodInfo
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="methodName">方法名</param>
    /// <param name="types">参数类型</param>
    /// <returns>MethodInfo 实例</returns>
    public System.Reflection.MethodInfo GetOrCacheMethodInfo(Type type, string methodName, Type[] types)
    {
        var key = $"{type.FullName}::{methodName}({string.Join(",", types.Select(t => t.FullName))})";

        if (_methodInfoCache.TryGetValue(key, out var cachedMethod))
        {
            return cachedMethod;
        }

        var method = type.GetMethod(methodName, types);
        if (method is not null)
        {
            _methodInfoCache[key] = method;
        }

        return method!;
    }

    /// <summary>
    /// 获取或缓存 ConstructorInfo
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="types">参数类型</param>
    /// <returns>ConstructorInfo 实例</returns>
    public System.Reflection.ConstructorInfo GetOrCacheConstructorInfo(Type type, Type[] types)
    {
        var key = $"{type.FullName}::.ctor({string.Join(",", types.Select(t => t.FullName))})";

        if (_constructorInfoCache.TryGetValue(key, out var cachedCtor))
        {
            return cachedCtor;
        }

        var ctor = type.GetConstructor(types);
        if (ctor is not null)
        {
            _constructorInfoCache[key] = ctor;
        }

        return ctor!;
    }

    /// <summary>
    /// 生成类型转换 IL（使用缓存）
    /// </summary>
    /// <param name="ilGenerator">IL 生成器</param>
    /// <param name="sourceType">源类型</param>
    /// <param name="targetType">目标类型</param>
    public void EmitTypeConversion(ILGenerator ilGenerator, Type sourceType, Type targetType)
    {
        var key = $"{sourceType.FullName}->{targetType.FullName}";

        if (_typeConversionCache.TryGetValue(key, out var cachedAction))
        {
            cachedAction(ilGenerator);
            return;
        }

        // 构建类型转换 IL 并缓存
        var action = BuildTypeConversionAction(sourceType, targetType);
        _typeConversionCache[key] = action;
        action(ilGenerator);
    }

    /// <summary>
    /// 构建类型转换 IL 生成委托
    /// </summary>
    /// <param name="sourceType">源类型</param>
    /// <param name="targetType">目标类型</param>
    /// <returns>生成 IL 的委托</returns>
    private Action<ILGenerator> BuildTypeConversionAction(Type sourceType, Type targetType)
    {
        return (ilGenerator) =>
        {
            // 类型相同，无需转换
            if (sourceType == targetType)
            {
                return;
            }

            // 装箱/拆箱转换
            if (sourceType.IsValueType && !targetType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, sourceType);
            }
            else if (!sourceType.IsValueType && targetType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, targetType);
            }
            // 数值类型转换
            else if (IsNumericType(sourceType) && IsNumericType(targetType))
            {
                EmitNumericConversion(ilGenerator, sourceType, targetType);
            }
            // 引用类型转换
            else if (!sourceType.IsValueType && !targetType.IsValueType)
            {
                if (targetType != typeof(object))
                {
                    ilGenerator.Emit(OpCodes.Castclass, targetType);
                }
            }
        };
    }

    /// <summary>
    /// 判断是否为数值类型
    /// </summary>
    private bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
               type == typeof(double) || type == typeof(float) || type == typeof(decimal) ||
               type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte);
    }

    /// <summary>
    /// 生成数值类型转换 IL
    /// </summary>
    private void EmitNumericConversion(ILGenerator ilGenerator, Type sourceType, Type targetType)
    {
        // int -> double
        if (sourceType == typeof(int) && targetType == typeof(double))
        {
            ilGenerator.Emit(OpCodes.Conv_R8);
        }
        // double -> int
        else if (sourceType == typeof(double) && targetType == typeof(int))
        {
            ilGenerator.Emit(OpCodes.Conv_I4);
        }
        // int -> long
        else if (sourceType == typeof(int) && targetType == typeof(long))
        {
            ilGenerator.Emit(OpCodes.Conv_I8);
        }
        // long -> int
        else if (sourceType == typeof(long) && targetType == typeof(int))
        {
            ilGenerator.Emit(OpCodes.Conv_I4);
        }
        // float -> double
        else if (sourceType == typeof(float) && targetType == typeof(double))
        {
            ilGenerator.Emit(OpCodes.Conv_R8);
        }
        // double -> float
        else if (sourceType == typeof(double) && targetType == typeof(float))
        {
            ilGenerator.Emit(OpCodes.Conv_R4);
        }
    }

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    public void ClearCache()
    {
        _methodInfoCache.Clear();
        _constructorInfoCache.Clear();
        _opCodeSequenceCache.Clear();
        _typeConversionCache.Clear();
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <returns>缓存统计字符串</returns>
    public string GetCacheStats()
    {
        return $"MethodInfo 缓存: {_methodInfoCache.Count}, " +
               $"ConstructorInfo 缓存: {_constructorInfoCache.Count}, " +
               $"OpCode 序列缓存: {_opCodeSequenceCache.Count}, " +
               $"类型转换缓存: {_typeConversionCache.Count}";
    }
}
