using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 懒加载特定项目的包装器，用于选择导入的懒加载
/// </summary>
public class LazyItemWrapper(string moduleName, string itemName, VariateManager manager, SourcePosition position)
    : LangValueType(position)
{
    private readonly SourcePosition position = position;
    private bool loaded = false;
    private LangValueType? loadedItem;

    /// <summary>
    /// 当作为函数调用时触发懒加载
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager currentManager)
    {
        if (!loaded)
        {
            LoadItem();
        }

        if (loadedItem != null)
        {
            return loadedItem.Dot(dotExpression, currentManager);
        }

        throw new AttributeError(this, dotExpression.ToString(), "LazyItem");
    }

    /// <summary>
    /// 当作为值使用时触发懒加载
    /// </summary>
    public override bool Equal(LangValueType? otherValueType)
    {
        if (!loaded)
        {
            LoadItem();
        }

        return loadedItem?.Equal(otherValueType) ?? false;
    }

    /// <summary>
    /// 执行实际的项目加载
    /// </summary>
    private void LoadItem()
    {
        if (loaded) return;

        try
        {
            // 执行实际的导入
            var importItems = new List<ImportItem> { new ImportItem(itemName) };
            var importStatement = new ImportStatement(moduleName, position, importItems, fromClause: true);
            importStatement.Run(manager);

            // 查找导入的项目
            if (manager.Scopes.Count > 0 && manager.Scopes[^1].TryGetValue(itemName, out var item))
            {
                loadedItem = item;
            }
            else
            {
                throw new ImportError(this, $"{moduleName}.{itemName}", $"Item {itemName} not found");
            }

            loaded = true;
        }
        catch (Exception ex)
        {
            throw new ImportError(this, $"{moduleName}.{itemName}", ex.Message);
        }
    }

    public override string ToString() =>
        loaded ? loadedItem?.ToString() ?? "Loaded" : $"LazyItem({itemName} from {moduleName})";
}