using System.Collections.Concurrent;
using System.Reflection;

namespace Old8Lang.Utilities;

/// <summary>
/// 程序集和类型缓存，用于优化外部程序集加载和类型查询性能
/// 缓存已加载的程序集、类型和方法信息
/// </summary>
public static class AssemblyTypeCache
{
    // 程序集缓存：key = assemblyName
    private static readonly ConcurrentDictionary<string, Assembly> AssemblyCache = new();

    // 类型缓存：key = "AssemblyName.ClassName"
    private static readonly ConcurrentDictionary<string, Type?> TypeCache = new();

    // 方法缓存：key = "TypeFullName.MethodName" 或 "TypeFullName.MethodName(ParamTypes)"
    private static readonly ConcurrentDictionary<string, MethodInfo?> MethodCache = new();

    /// <summary>
    /// 获取或加载程序集
    /// </summary>
    /// <param name="assemblyName">程序集名称或路径</param>
    /// <returns>加载的程序集</returns>
    public static Assembly GetOrLoadAssembly(string assemblyName)
    {
        return AssemblyCache.GetOrAdd(assemblyName, LoadAssemblyInternal);
    }

    /// <summary>
    /// 在程序集中查找类型
    /// </summary>
    /// <param name="assembly">程序集</param>
    /// <param name="className">类名</param>
    /// <returns>找到的类型，如果找不到则返回 null</returns>
    public static Type? FindType(Assembly assembly, string className)
    {
        var key = $"{assembly.GetName().Name}.{className}";
        return TypeCache.GetOrAdd(key, _ => FindTypeInternal(assembly, className));
    }

    /// <summary>
    /// 在类型中查找方法（无参数类型）
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="methodName">方法名</param>
    /// <returns>找到的方法，如果找不到则返回 null</returns>
    public static MethodInfo? FindMethod(Type type, string methodName)
    {
        var key = $"{type.FullName}.{methodName}";
        return MethodCache.GetOrAdd(key, _ =>
            type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance));
    }

    /// <summary>
    /// 在类型中查找方法（带参数类型）
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="methodName">方法名</param>
    /// <param name="paramTypes">参数类型数组</param>
    /// <returns>找到的方法，如果找不到则返回 null</returns>
    public static MethodInfo? FindMethod(Type type, string methodName, Type[]? paramTypes)
    {
        if (paramTypes == null || paramTypes.Length == 0)
        {
            return FindMethod(type, methodName);
        }

        var paramTypesStr = string.Join(",", paramTypes.Select(t => t.FullName ?? t.Name));
        var key = $"{type.FullName}.{methodName}({paramTypesStr})";
        return MethodCache.GetOrAdd(key, _ =>
            type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance, null, paramTypes, null));
    }

    /// <summary>
    /// 内部方法：加载程序集
    /// </summary>
    private static Assembly LoadAssemblyInternal(string assemblyName)
    {
        // 尝试作为文件路径加载
        if (File.Exists(assemblyName))
        {
            return Assembly.LoadFrom(assemblyName);
        }

        // 尝试从已加载的程序集中查找
        var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name?.Equals(assemblyName, StringComparison.OrdinalIgnoreCase) == true);

        if (loadedAssembly != null)
        {
            return loadedAssembly;
        }

        // 尝试作为程序集名称加载
        try
        {
            return Assembly.Load(assemblyName);
        }
        catch
        {
            // 尝试加载 System.Runtime 等标准程序集
            try
            {
                return Assembly.Load($"{assemblyName}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            }
            catch
            {
                // 最后尝试从 GAC 加载
                return Assembly.Load(new AssemblyName(assemblyName));
            }
        }
    }

    /// <summary>
    /// 内部方法：在程序集中查找类型
    /// </summary>
    private static Type? FindTypeInternal(Assembly assembly, string className)
    {
        // 直接查找
        var type = assembly.GetType(className);
        if (type != null) return type;

        // 尝试在所有命名空间中查找
        try
        {
            type = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == className || t.FullName == className);
            if (type != null) return type;
        }
        catch
        {
            // 忽略无法访问的类型
        }

        // 尝试从所有已加载的程序集中查找
        foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                type = loadedAssembly.GetType(className);
                if (type != null) return type;

                type = loadedAssembly.GetTypes()
                    .FirstOrDefault(t => t.Name == className || t.FullName == className);
                if (type != null) return type;
            }
            catch
            {
                // 忽略无法访问的程序集
            }
        }

        return null;
    }

    /// <summary>
    /// 清除所有缓存（测试或内存管理时使用）
    /// </summary>
    public static void ClearCache()
    {
        AssemblyCache.Clear();
        TypeCache.Clear();
        MethodCache.Clear();
    }

    /// <summary>
    /// 获取缓存的程序集数量
    /// </summary>
    public static int AssemblyCacheCount => AssemblyCache.Count;

    /// <summary>
    /// 获取缓存的类型数量
    /// </summary>
    public static int TypeCacheCount => TypeCache.Count;

    /// <summary>
    /// 获取缓存的方法数量
    /// </summary>
    public static int MethodCacheCount => MethodCache.Count;

    /// <summary>
    /// 获取总缓存数量
    /// </summary>
    public static int TotalCacheCount => AssemblyCacheCount + TypeCacheCount + MethodCacheCount;
}
