using System.Reflection;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.StandardLibrary;

/// <summary>
/// 标准库加载器，负责加载内置的标准库
/// </summary>
public static class StandardLibraryLoader
{
    /// <summary>
    /// 已加载的程序集缓存
    /// </summary>
    private static readonly Dictionary<string, Assembly> LoadedAssemblies = new();

    /// <summary>
    /// 程序集加载锁
    /// </summary>
    private static readonly Lock AssemblyLock = new();

    /// <summary>
    /// 尝试加载标准库
    /// </summary>
    /// <param name="libraryName">库名称</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="module">输出的模块对象</param>
    /// <returns>是否成功加载</returns>
    public static bool TryLoadStandardLibrary(
        string libraryName,
        VariateManager manager,
        out LangValueType module)
    {
        if (!StandardLibraryRegistry.IsStandardLibrary(libraryName))
        {
            module = new VoidLangValue();
            return false;
        }

        try
        {
            var libInfo = StandardLibraryRegistry.GetLibraryInfo(libraryName)!;
            var assembly = GetOrLoadAssembly(libInfo.AssemblyName);

            // 加载原生类和方法
            var symbols = LoadNativeMethods(assembly, libInfo, manager);
            module = ModuleFactory.CreateModuleFromSymbols(libraryName, symbols);

            return true;
        }
        catch (Exception ex)
        {
            throw new Old8Exception("STDLIB_LOAD_ERROR", $"加载标准库 '{libraryName}' 失败: {ex.Message}",
                new SourcePosition(), null, null, null, null, ex);
        }
    }

    /// <summary>
    /// 获取或加载程序集
    /// </summary>
    private static Assembly GetOrLoadAssembly(string assemblyName)
    {
        lock (AssemblyLock)
        {
            if (LoadedAssemblies.TryGetValue(assemblyName, out var cached))
                return cached;

            // 尝试从已加载的程序集中查找
            var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);

            if (loadedAssembly != null)
            {
                LoadedAssemblies[assemblyName] = loadedAssembly;
                return loadedAssembly;
            }

            // 尝试从运行时目录加载
            var runtimePath = Path.Combine(
                AppContext.BaseDirectory,
                $"{assemblyName}.dll"
            );

            if (!File.Exists(runtimePath))
            {
                throw new FileNotFoundException($"找不到标准库程序集: {assemblyName}.dll");
            }

            var assembly = Assembly.LoadFrom(runtimePath);
            LoadedAssemblies[assemblyName] = assembly;
            return assembly;
        }
    }

    /// <summary>
    /// 从程序集加载原生方法
    /// </summary>
    private static Dictionary<string, LangValueType> LoadNativeMethods(
        Assembly assembly,
        StandardLibraryInfo libInfo,
        VariateManager manager)
    {
        var symbols = new Dictionary<string, LangValueType>();

        if (libInfo.IsMultiClass)
        {
            // 多类库：加载多个类
            foreach (var className in libInfo.ClassNames!)
            {
                LoadClassMethods(assembly, libInfo.AssemblyName, className, symbols, manager);
            }
        }
        else if (libInfo.ClassName != null)
        {
            // 单类库：加载单个类
            LoadClassMethods(assembly, libInfo.AssemblyName, libInfo.ClassName, symbols, manager);
        }

        return symbols;
    }

    /// <summary>
    /// 加载类的所有公共静态方法
    /// </summary>
    private static void LoadClassMethods(
        Assembly assembly,
        string assemblyName,
        string className,
        Dictionary<string, LangValueType> symbols,
        VariateManager manager)
    {
        // 获取类型
        var fullTypeName = $"{assemblyName}.{className}";
        var type = assembly.GetType(fullTypeName);

        if (type == null)
        {
            throw new Old8Exception("TYPE_NOT_FOUND", $"在程序集 '{assemblyName}' 中找不到类型 '{className}'",
                new SourcePosition());
        }

        // 获取所有公共静态方法
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => !IsObjectMethod(m))
            .ToList();

        // 为每个方法创建 FuncLangValue 并注册
        foreach (var method in methods)
        {
            var funcValue = new FuncLangValue(method.Name, method);
            symbols[method.Name] = funcValue;

            // 同时注册到变量管理器（用于全局访问）
            manager.AddClassAndFunc(funcValue);
        }

        // 如果类本身有构造器，注册类构造器
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (constructors.Length > 0)
        {
            // 为构造器创建包装函数
            // 注意：这里简化处理，实际可能需要更复杂的类型包装
            // 暂时跳过类构造器注册，因为需要特殊的构造器包装类
        }
    }

    /// <summary>
    /// 检查是否为 Object 基类方法
    /// </summary>
    private static bool IsObjectMethod(MethodInfo method)
    {
        var objectMethods = new HashSet<string>
        {
            "ToString", "Equals", "GetHashCode", "GetType",
            "ReferenceEquals", "Finalize", "MemberwiseClone"
        };

        return objectMethods.Contains(method.Name);
    }

    /// <summary>
    /// 获取所有已加载的标准库名称
    /// </summary>
    public static IEnumerable<string> GetLoadedLibraries()
    {
        lock (AssemblyLock)
        {
            return LoadedAssemblies.Keys;
        }
    }

    /// <summary>
    /// 清除程序集缓存（主要用于测试）
    /// </summary>
    public static void ClearCache()
    {
        lock (AssemblyLock)
        {
            LoadedAssemblies.Clear();
        }
    }
}