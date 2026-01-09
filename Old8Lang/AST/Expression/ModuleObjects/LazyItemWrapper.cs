using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 懒加载特定项目的包装器，用于选择导入的懒加载
/// </summary>
public class LazyItemWrapper(string moduleNameItem, string itemName, VariateManager manager, SourcePosition position)
    : LangValueType(position), IModuleWrapper
{
    private bool Loaded;
    private LangValueType? LoadedItem;

    /// <summary>
    /// 模块名称
    /// </summary>
    public string ModuleName => $"{moduleNameItem}.{itemName}";

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
        // LazyItemWrapper 包装的是单个项目，不是完整的模块对象
        return null;
    }

    /// <summary>
    /// 获取模块中的符号（对于项目包装器，返回项目本身）
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <returns>符号值</returns>
    public LangValueType? GetSymbol(string symbolName)
    {
        if (!Loaded)
        {
            LoadItem();
        }

        // 如果请求的符号名称与项目名称匹配，返回加载的项目
        if (string.Equals(symbolName, itemName, StringComparison.OrdinalIgnoreCase))
        {
            return LoadedItem;
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
        return string.Equals(symbolName, itemName, StringComparison.OrdinalIgnoreCase) && GetSymbol(symbolName) is not null;
    }

    /// <summary>
    /// 获取模块中所有的导出符号名称
    /// </summary>
    /// <returns>符号名称列表</returns>
    public IEnumerable<string> GetExportedSymbols()
    {
        if (!Loaded)
        {
            LoadItem();
        }

        return LoadedItem is not null ? [itemName] : [];
    }

    /// <summary>
    /// 强制加载模块
    /// </summary>
    /// <param name="variateManager">变量管理器</param>
    public void EnsureLoaded(VariateManager variateManager)
    {
        if (!Loaded)
        {
            LoadItem();
        }
    }

    /// <summary>
    /// 当作为函数调用时触发懒加载
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager currentManager)
    {
        if (!Loaded)
        {
            LoadItem();
        }

        if (LoadedItem is not null)
        {
            return LoadedItem.Dot(dotExpression, currentManager);
        }

        throw new AttributeError(this, dotExpression + " is not callable", "LazyItem");
    }

    /// <summary>
    /// 当作为值使用时触发懒加载
    /// </summary>
    public override bool Equal(LangValueType? otherValueType)
    {
        if (!Loaded)
        {
            LoadItem();
        }

        return LoadedItem?.Equal(otherValueType) ?? false;
    }

    /// <summary>
    /// 执行实际的项目加载
    /// </summary>
    private void LoadItem()
    {
        if (Loaded) return;

        try
        {
            // 执行实际的导入
            var importItems = new List<ImportItem> { new(itemName) };
            var importStatement = new ImportStatement(moduleNameItem, Position, importItems, fromClause: true);
            importStatement.Run(manager);

            // 查找导入的项目
            if (manager.Scopes.Count > 0 && manager.Scopes[^1].TryGetValue(itemName, out var item))
            {
                LoadedItem = item;
            }
            else
            {
                throw new ImportError(this, $"{moduleNameItem}.{itemName}", $"Item {itemName} not found");
            }

            Loaded = true;
        }
        catch (Exception ex)
        {
            throw new ImportError(this, $"{moduleNameItem}.{itemName}", ex.Message);
        }
    }

    public override string ToString() =>
        Loaded ? LoadedItem?.ToString() ?? "Loaded" : $"LazyItem({itemName} from {moduleNameItem})";

    public override TResult Accept<TResult>(Visitor.IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("LazyItemWrapper 暂不支持 Visitor 模式访问");
    }
}