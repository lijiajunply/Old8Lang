using Old8Lang.AST.Expression;

namespace Old8Lang.ModuleSystem.Symbols;

/// <summary>
/// 导出控制器 - 管理模块的导出规则
/// 为将来的 export 关键字功能预留接口
/// </summary>
public class ExportController
{
    private readonly HashSet<string> _exportedSymbols = [];
    private bool _exportAll = true; // 默认导出所有符号

    /// <summary>
    /// 标记符号为导出
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    public void MarkAsExported(string symbolName)
    {
        _exportedSymbols.Add(symbolName);
        _exportAll = false; // 一旦有显式导出，就不再自动导出所有
    }

    /// <summary>
    /// 标记多个符号为导出
    /// </summary>
    /// <param name="symbolNames">符号名称列表</param>
    public void MarkAsExported(IEnumerable<string> symbolNames)
    {
        foreach (var symbolName in symbolNames)
        {
            _exportedSymbols.Add(symbolName);
        }

        _exportAll = false;
    }

    /// <summary>
    /// 检查符号是否被导出
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <returns>是否被导出</returns>
    public bool IsExported(string symbolName)
    {
        // 如果导出所有，返回 true
        if (_exportAll)
        {
            return true;
        }

        // 否则检查是否在导出列表中
        return _exportedSymbols.Contains(symbolName);
    }

    /// <summary>
    /// 过滤符号，只保留导出的符号
    /// </summary>
    /// <param name="allSymbols">所有符号</param>
    /// <returns>导出的符号</returns>
    public Dictionary<string, LangValueType> FilterExportedSymbols(Dictionary<string, LangValueType> allSymbols)
    {
        // 如果导出所有，直接返回
        if (_exportAll)
        {
            return allSymbols;
        }

        // 否则只返回导出的符号
        return allSymbols
            .Where(kvp => IsExported(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// 设置是否导出所有符号
    /// </summary>
    /// <param name="exportAll">是否导出所有</param>
    public void SetExportAll(bool exportAll)
    {
        _exportAll = exportAll;

        if (exportAll)
        {
            _exportedSymbols.Clear(); // 清除显式导出列表
        }
    }

    /// <summary>
    /// 获取所有导出的符号名称
    /// </summary>
    /// <returns>导出的符号名称集合</returns>
    public IEnumerable<string> GetExportedSymbolNames()
    {
        return _exportedSymbols;
    }

    /// <summary>
    /// 是否导出所有符号
    /// </summary>
    public bool ExportAll => _exportAll;

    /// <summary>
    /// 导出的符号数量
    /// </summary>
    public int ExportedCount => _exportAll ? -1 : _exportedSymbols.Count;

    /// <summary>
    /// 清除所有导出规则
    /// </summary>
    public void Clear()
    {
        _exportedSymbols.Clear();
        _exportAll = true;
    }

    /// <summary>
    /// 取消导出指定符号
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    public void UnmarkAsExported(string symbolName)
    {
        _exportedSymbols.Remove(symbolName);
    }
}
