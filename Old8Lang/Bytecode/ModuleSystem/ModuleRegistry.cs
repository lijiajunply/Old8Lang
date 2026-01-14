namespace Old8Lang.Bytecode.ModuleSystem;

/// <summary>
/// 模块注册表 - 管理已加载的模块
/// </summary>
public class ModuleRegistry
{
    private readonly Dictionary<string, LoadedModule> _modules = new();
    private readonly HashSet<string> _loadingModules = new();

    /// <summary>
    /// 注册模块
    /// </summary>
    public void RegisterModule(string moduleName, BytecodeFile bytecodeFile, Dictionary<string, object?> globals)
    {
        var loadedModule = new LoadedModule(moduleName, bytecodeFile, globals);
        _modules[moduleName] = loadedModule;
        _loadingModules.Remove(moduleName);
    }

    /// <summary>
    /// 获取已加载的模块
    /// </summary>
    public LoadedModule? GetModule(string moduleName)
    {
        return _modules.TryGetValue(moduleName, out var module) ? module : null;
    }

    /// <summary>
    /// 检查模块是否已加载
    /// </summary>
    public bool IsModuleLoaded(string moduleName)
    {
        return _modules.ContainsKey(moduleName);
    }

    /// <summary>
    /// 标记模块正在加载（用于循环依赖检测）
    /// </summary>
    public bool MarkModuleLoading(string moduleName)
    {
        if (_loadingModules.Contains(moduleName))
        {
            return false; // 循环依赖
        }
        _loadingModules.Add(moduleName);
        return true;
    }

    /// <summary>
    /// 获取模块的导出符号
    /// </summary>
    public object? GetModuleSymbol(string moduleName, string symbolName)
    {
        if (!_modules.TryGetValue(moduleName, out var module))
        {
            throw new Exception($"模块 '{moduleName}' 未加载");
        }

        return module.GetSymbol(symbolName);
    }

    /// <summary>
    /// 获取所有已加载的模块名称
    /// </summary>
    public IEnumerable<string> GetLoadedModuleNames()
    {
        return _modules.Keys;
    }

    /// <summary>
    /// 清空所有模块
    /// </summary>
    public void Clear()
    {
        _modules.Clear();
        _loadingModules.Clear();
    }
}

/// <summary>
/// 已加载的模块信息
/// </summary>
public class LoadedModule
{
    /// <summary>
    /// 模块名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 字节码文件
    /// </summary>
    public BytecodeFile BytecodeFile { get; }

    /// <summary>
    /// 模块的全局变量空间
    /// </summary>
    public Dictionary<string, object?> Globals { get; }

    /// <summary>
    /// 导出符号缓存
    /// </summary>
    private Dictionary<string, object?>? _exportCache;

    public LoadedModule(string name, BytecodeFile bytecodeFile, Dictionary<string, object?> globals)
    {
        Name = name;
        BytecodeFile = bytecodeFile;
        Globals = globals;
    }

    /// <summary>
    /// 获取导出的符号
    /// </summary>
    public object? GetSymbol(string symbolName)
    {
        // 构建导出缓存
        if (_exportCache == null)
        {
            _exportCache = new Dictionary<string, object?>();

            // 从Exports字段获取导出符号
            if (BytecodeFile.Exports != null)
            {
                foreach (var export in BytecodeFile.Exports)
                {
                    var symbol = export.Value;
                    object? value = null;

                    switch (symbol.Type)
                    {
                        case ExportedSymbolType.Function:
                            // 查找函数
                            if (symbol.MetadataIndex >= 0 && symbol.MetadataIndex < BytecodeFile.Functions.Count)
                            {
                                value = BytecodeFile.Functions[symbol.MetadataIndex];
                            }
                            break;

                        case ExportedSymbolType.Class:
                            // 查找类
                            if (symbol.MetadataIndex >= 0 && symbol.MetadataIndex < BytecodeFile.Classes.Count)
                            {
                                value = BytecodeFile.Classes[symbol.MetadataIndex];
                            }
                            break;

                        case ExportedSymbolType.Variable:
                            // 从全局变量中获取
                            if (Globals.TryGetValue(symbol.Name, out var globalValue))
                            {
                                value = globalValue;
                            }
                            break;

                        case ExportedSymbolType.Interface:
                            // 查找接口
                            if (symbol.MetadataIndex >= 0 && symbol.MetadataIndex < BytecodeFile.Interfaces.Count)
                            {
                                value = BytecodeFile.Interfaces[symbol.MetadataIndex];
                            }
                            break;

                        case ExportedSymbolType.Mixin:
                            // 查找Mixin
                            if (symbol.MetadataIndex >= 0 && symbol.MetadataIndex < BytecodeFile.Mixins.Count)
                            {
                                value = BytecodeFile.Mixins[symbol.MetadataIndex];
                            }
                            break;
                    }

                    _exportCache[symbol.Name] = value;
                }
            }
            else
            {
                // 如果没有显式导出，则导出所有顶层函数和类
                for (int i = 0; i < BytecodeFile.Functions.Count; i++)
                {
                    var func = BytecodeFile.Functions[i];
                    _exportCache[func.Name] = func;
                }

                for (int i = 0; i < BytecodeFile.Classes.Count; i++)
                {
                    var cls = BytecodeFile.Classes[i];
                    _exportCache[cls.Name] = cls;
                }
            }
        }

        return _exportCache.TryGetValue(symbolName, out var result) ? result : null;
    }

    /// <summary>
    /// 获取所有导出的符号名称
    /// </summary>
    public IEnumerable<string> GetExportedSymbolNames()
    {
        if (_exportCache == null)
        {
            GetSymbol(""); // 触发缓存构建
        }
        return _exportCache?.Keys ?? Enumerable.Empty<string>();
    }
}
