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
            var assembly = libInfo.GetAssembly();

            // 根据导入模式加载
            var symbols = libInfo.ImportMode switch
            {
                StandardLibraryImportMode.Assembly => LoadFromAssembly(assembly, libInfo, manager),
                StandardLibraryImportMode.Types => LoadFromTypes(libInfo, manager),
                _ => throw new InvalidOperationException($"未知的导入模式: {libInfo.ImportMode}")
            };

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
    /// 验证标准库是否可用（用于编译时检查）
    /// 不会实际加载符号，只检查程序集和类是否存在
    /// </summary>
    /// <param name="libraryName">库名称</param>
    /// <param name="errorMessage">如果验证失败，返回错误信息</param>
    /// <returns>是否验证成功</returns>
    public static bool ValidateStandardLibrary(string libraryName, out string? errorMessage)
    {
        errorMessage = null;

        // 检查是否为标准库
        if (!StandardLibraryRegistry.IsStandardLibrary(libraryName))
        {
            errorMessage = $"'{libraryName}' 不是已注册的标准库";
            return false;
        }

        try
        {
            var libInfo = StandardLibraryRegistry.GetLibraryInfo(libraryName)!;

            // 尝试加载程序集（不抛出异常，只验证）
            libInfo.GetAssembly();

            // 验证类型是否存在
            if (libInfo is { ImportMode: StandardLibraryImportMode.Types, TypeConfigs: not null })
            {
                if (libInfo.TypeConfigs.Select(typeConfig => typeConfig.Type).Any(type => type is null))
                {
                    errorMessage = $"标准库 '{libraryName}' 中找不到指定的类型";
                    return false;
                }
            }

            return true;
        }
        catch (FileNotFoundException ex)
        {
            errorMessage = $"标准库 '{libraryName}' 的程序集不存在: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = $"验证标准库 '{libraryName}' 时发生错误: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// 获取或加载程序集（公开方法，供 StandardLibraryInfo 使用）
    /// </summary>
    public static Assembly GetOrLoadAssembly(string assemblyName)
    {
        lock (AssemblyLock)
        {
            if (LoadedAssemblies.TryGetValue(assemblyName, out var cached))
                return cached;

            // 尝试从已加载的程序集中查找
            var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);

            if (loadedAssembly is not null)
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
    /// 从程序集导入（全局导入模式）
    /// 支持使用 a.b 来访问子命名空间
    /// </summary>
    private static Dictionary<string, LangValueType> LoadFromAssembly(
        Assembly assembly,
        StandardLibraryInfo libInfo,
        VariateManager manager)
    {
        var symbols = new Dictionary<string, LangValueType>();
        var rootNamespace = libInfo.RootNamespace ?? assembly.GetName().Name;

        // 获取程序集中所有公共类型
        var types = assembly.GetExportedTypes()
            .Where(t => t.Namespace is not null && t.Namespace.StartsWith(rootNamespace!))
            .ToList();

        if (libInfo.EnableSubNamespaceImport)
        {
            // 支持子命名空间导入：创建命名空间层次结构
            var namespaceGroups = types.GroupBy(t => t.Namespace!);

            foreach (var group in namespaceGroups)
            {
                var ns = group.Key;
                // 移除根命名空间前缀
                var relativePath = ns.Substring(rootNamespace!.Length).TrimStart('.');

                if (string.IsNullOrEmpty(relativePath))
                {
                    // 根命名空间下的类型，直接添加
                    foreach (var type in group)
                    {
                        AddTypeToSymbols(type, symbols, manager);
                    }
                }
                else
                {
                    // 子命名空间下的类型，创建命名空间模块
                    var subNamespaceSymbols = new Dictionary<string, LangValueType>();
                    foreach (var type in group)
                    {
                        AddTypeToSymbols(type, subNamespaceSymbols, manager);
                    }

                    // 创建命名空间模块
                    var subModule = ModuleFactory.CreateModuleFromSymbols(
                        relativePath.Replace('.', '_'),
                        subNamespaceSymbols
                    );

                    // 支持 a.b.c 的路径访问
                    var parts = relativePath.Split('.');
                    var currentDict = symbols;

                    for (int i = 0; i < parts.Length; i++)
                    {
                        var part = parts[i];

                        if (i == parts.Length - 1)
                        {
                            // 最后一层，添加模块
                            currentDict[part] = subModule;
                        }
                        else
                        {
                            // 中间层，创建或获取嵌套字典
                            if (!currentDict.ContainsKey(part))
                            {
                                var nestedSymbols = new Dictionary<string, LangValueType>();
                                currentDict[part] = ModuleFactory.CreateModuleFromSymbols(part, nestedSymbols);
                            }

                            // 这里需要获取嵌套模块的符号字典，暂时简化处理
                            // 实际应该支持多级嵌套
                        }
                    }
                }
            }
        }
        else
        {
            // 不支持子命名空间导入，直接加载所有类型
            foreach (var type in types)
            {
                AddTypeToSymbols(type, symbols, manager);
            }
        }

        return symbols;
    }

    /// <summary>
    /// 从类型列表导入
    /// </summary>
    private static Dictionary<string, LangValueType> LoadFromTypes(StandardLibraryInfo libInfo,
        VariateManager manager)
    {
        var symbols = new Dictionary<string, LangValueType>();

        if (libInfo.TypeConfigs is null)
            return symbols;

        foreach (var typeConfig in libInfo.TypeConfigs)
        {
            var type = typeConfig.Type;

            if (typeConfig.ImportStaticMembers)
            {
                // 静态类：导入静态成员
                if (typeConfig.UseNativeStaticAny)
                {
                    // 使用 NativeStaticAny 包装
                    var nativeStaticAny = new NativeStaticAny(type.Name, type);
                    symbols[type.Name] = nativeStaticAny;
                }
                else
                {
                    // 直接导入静态成员
                    LoadStaticMembers(type, symbols, manager);
                }
            }
            else
            {
                // 普通类：导入为 NativeAnyLangValue
                // 使用类型的 Assembly.Location 和完整的类型名（包括命名空间）
                var assemblyLocation = type.Assembly.Location;
                var assemblyName = type.Assembly.GetName().Name ?? "Unknown";

                // 使用完整的类型名（Namespace.ClassName）而不仅仅是 ClassName
                var fullTypeName = type.FullName ?? type.Name;

                var nativeAny = new NativeAnyLangValue(assemblyName, fullTypeName,
                    assemblyLocation, type.Name);

                // 执行初始化
                nativeAny.Run(manager);
                symbols[type.Name] = nativeAny;
            }
        }

        return symbols;
    }

    /// <summary>
    /// 将类型添加到符号表
    /// </summary>
    private static void AddTypeToSymbols(Type type, Dictionary<string, LangValueType> symbols, VariateManager manager)
    {
        // 判断是否为静态类
        if (type is { IsAbstract: true, IsSealed: true })
        {
            // 静态类：导入静态成员
            LoadStaticMembers(type, symbols, manager);
        }
        else
        {
            // 普通类：导入为 NativeAnyLangValue
            var assemblyLocation = type.Assembly.Location;
            var assemblyName = type.Assembly.GetName().Name ?? "Unknown";

            // 使用完整的类型名（Namespace.ClassName）而不仅仅是 ClassName
            var fullTypeName = type.FullName ?? type.Name;

            var nativeAny = new NativeAnyLangValue(assemblyName, fullTypeName,
                assemblyLocation, type.Name);

            // 执行初始化
            nativeAny.Run(manager);
            symbols[type.Name] = nativeAny;
        }
    }

    /// <summary>
    /// 加载静态成员
    /// </summary>
    private static void LoadStaticMembers(Type type, Dictionary<string, LangValueType> symbols, VariateManager manager)
    {
        // 获取所有公共静态方法（排除泛型方法）
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => !IsObjectMethod(m) && !m.ContainsGenericParameters)
            .ToList();

        // 为每个方法创建 FuncLangValue 并注册
        foreach (var method in methods)
        {
            var funcValue = new FuncLangValue(method.Name, method);
            symbols[method.Name] = funcValue;

            // 同时注册到变量管理器（用于全局访问）
            manager.AddClassAndFunc(funcValue);
        }

        // 获取所有公共静态属性
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Static);
        foreach (var property in properties)
        {
            if (property.CanRead)
            {
                var value = property.GetValue(null);
                symbols[property.Name] = LangValueType.ObjToValue(value);
            }
        }

        // 获取所有公共静态字段
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
        foreach (var field in fields)
        {
            var value = field.GetValue(null);
            symbols[field.Name] = LangValueType.ObjToValue(value);
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