using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 选择性导入模块对象，只导入指定的符号
/// </summary>
public class SelectiveModuleObject : BaseModuleObject
{
    private readonly string SourceModuleName;
    private readonly List<ImportItem> SelectedItems;
    private readonly bool IsLazy;
    private readonly VariateManager SourceManager;
    private readonly SourcePosition _position;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="sourceModuleName">源模块名称</param>
    /// <param name="selectedItems">选择的导入项</param>
    /// <param name="isLazy">是否懒加载</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="position">源码位置</param>
    public SelectiveModuleObject(
        string sourceModuleName,
        List<ImportItem> selectedItems,
        bool isLazy,
        VariateManager manager,
        SourcePosition position = default)
        : base(GenerateModuleName(sourceModuleName, selectedItems), position)
    {
        SourceModuleName = sourceModuleName;
        SelectedItems = selectedItems;
        IsLazy = isLazy;
        SourceManager = manager;
        _position = position;

        if (!IsLazy)
        {
            LoadModule(manager);
        }
    }

    /// <summary>
    /// 生成模块名称
    /// </summary>
    /// <param name="sourceModuleName">源模块名称</param>
    /// <param name="selectedItems">选择的导入项</param>
    /// <returns>生成的模块名称</returns>
    private static string GenerateModuleName(string sourceModuleName, List<ImportItem> selectedItems)
    {
        var itemNames = string.Join(", ", selectedItems.Select(item => item.Alias ?? item.Name));
        return $"{sourceModuleName}[{itemNames}]";
    }

    /// <summary>
    /// 加载模块
    /// </summary>
    /// <param name="manager">变量管理器</param>
    protected override void LoadModule(VariateManager manager)
    {
        // 创建新的作用域用于模块加载
        var newScope = new Dictionary<string, LangValueType>();
        manager.Scopes.Add(newScope);

        try
        {
            // 创建导入语句并执行
            var importStatement = new ImportStatement(SourceModuleName, _position);
            importStatement.Run(manager);

            // 提取选定的符号
            ExtractSelectedSymbols(manager);

            OnLoadSuccess();
        }
        finally
        {
            // 移除临时作用域
            if (manager.Scopes.Count > 1)
            {
                manager.Scopes.RemoveAt(manager.Scopes.Count - 1);
            }
        }
    }

    /// <summary>
    /// 从变量管理器中提取选定的符号
    /// </summary>
    /// <param name="manager">变量管理器</param>
    private void ExtractSelectedSymbols(VariateManager manager)
    {
        if (manager.Scopes.Count == 0) return;

        var currentScope = manager.Scopes[^1];
        var moduleBaseName = Path.GetFileNameWithoutExtension(SourceModuleName);

        foreach (var importItem in SelectedItems)
        {
            var sourceName = importItem.Name;
            var targetName = importItem.Alias ?? sourceName;

            // 查找源符号
            if (currentScope.TryGetValue(sourceName, out var symbolValue))
            {
                AddSymbol(targetName, symbolValue);
            }
            else
            {
                throw new ImportError(this, $"{SourceModuleName}.{sourceName}",
                    $"Symbol '{sourceName}' not found in module '{SourceModuleName}'");
            }
        }
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        var status = IsLazy && !IsLoaded ? "lazy" : "loaded";
        var itemCount = SelectedItems.Count;
        return $"<selective {Path.GetFileNameWithoutExtension(SourceModuleName)} ({itemCount} items, {status})>";
    }
}