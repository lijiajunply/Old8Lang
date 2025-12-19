using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 懒导入包装器，延迟加载模块直到首次使用
/// </summary>
public class LazyImportWrapper(string moduleName, VariateManager manager, SourcePosition position)
    : LangValueType(position), IModuleWrapper
{
    private bool Loaded;
    private LangValueType? LoadedModule;
    private readonly string _moduleName = moduleName;
    private readonly VariateManager _manager = manager;

    /// <summary>
    /// 模块名称
    /// </summary>
    public string ModuleName => _moduleName;

    /// <summary>
    /// 模块是否已加载
    /// </summary>
    public bool IsLoaded => Loaded;

    /// <summary>
    /// 模块加载状态
    /// </summary>
    public ModuleLoadingState LoadingState => Loaded ? ModuleLoadingState.Loaded : ModuleLoadingState.NotLoaded;

    /// <summary>
    /// 是否已加载（包装器特定）
    /// </summary>
    public bool IsWrapperLoaded => Loaded;

    /// <summary>
    /// 获取被包装的模块对象
    /// </summary>
    /// <returns>被包装的模块对象</returns>
    public IModuleObject? GetWrappedModule()
    {
        if (!Loaded) return null;

        // 如果加载的模块实现了模块对象接口，则返回
        if (LoadedModule is IModuleObject moduleObject)
        {
            return moduleObject;
        }

        // 否则返回 null
        return null;
    }

    /// <summary>
    /// 获取模块中的符号
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <returns>符号值</returns>
    public LangValueType? GetSymbol(string symbolName)
    {
        if (!Loaded)
        {
            LoadModule();
        }

        if (LoadedModule is IModuleObject moduleObj)
        {
            return moduleObj.GetSymbol(symbolName);
        }

        return null;
    }

    /// <summary>
    /// 检查模块是否包含指定符号
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <returns>是否包含符号</returns>
    public bool HasSymbol(string symbolName)
    {
        return GetSymbol(symbolName) != null;
    }

    /// <summary>
    /// 获取模块中所有的导出符号名称
    /// </summary>
    /// <returns>符号名称列表</returns>
    public IEnumerable<string> GetExportedSymbols()
    {
        if (!Loaded)
        {
            LoadModule();
        }

        if (LoadedModule is IModuleObject moduleObj)
        {
            return moduleObj.GetExportedSymbols();
        }

        return Enumerable.Empty<string>();
    }

    /// <summary>
    /// 强制加载模块
    /// </summary>
    /// <param name="manager">变量管理器</param>
    public void EnsureLoaded(VariateManager manager)
    {
        if (!Loaded)
        {
            LoadModule();
        }
    }

    /// <summary>
    /// 当访问模块属性时触发懒加载
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager currentManager)
    {
        if (!Loaded)
        {
            LoadModule();
        }

        if (LoadedModule != null)
        {
            return LoadedModule.Dot(dotExpression, currentManager);
        }

        throw new AttributeError(this, dotExpression.ToString(), "LazyModule");
    }

    /// <summary>
    /// 执行实际的模块加载
    /// </summary>
    private void LoadModule()
    {
        if (Loaded) return;

        try
        {
            // 执行实际的导入
            var importStatement = new ImportStatement(moduleName, Position);
            importStatement.Run(manager);

            // 查找导入的模块对象
            var moduleNameVar = Path.GetFileNameWithoutExtension(moduleName);
            if (manager.Scopes.Count > 0 && manager.Scopes[^1].TryGetValue(moduleNameVar, out var moduleObj))
            {
                LoadedModule = moduleObj;
            }
            else
            {
                LoadedModule = new StringLangValue("LazyModule");
            }

            Loaded = true;
        }
        catch (Exception ex)
        {
            throw new ImportError(this, moduleName, ex.Message);
        }
    }

    public override string ToString() => Loaded ? LoadedModule?.ToString() ?? "Loaded" : $"LazyModule({moduleName})";
}