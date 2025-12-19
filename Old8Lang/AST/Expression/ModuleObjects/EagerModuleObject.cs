using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 即时加载模块对象，在构造时立即加载模块内容
/// </summary>
public class EagerModuleObject : BaseModuleObject
{
    private readonly VariateManager SourceManager;
    private readonly SourcePosition _position;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="position">源码位置</param>
    public EagerModuleObject(string moduleName, VariateManager manager, SourcePosition position = default)
        : base(moduleName, position)
    {
        SourceManager = manager;
        _position = position;

        // 立即加载模块
        LoadModule(manager);
    }

    /// <summary>
    /// 从现有的符号字典创建模块对象
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="symbols">符号字典</param>
    /// <param name="position">源码位置</param>
    public EagerModuleObject(string moduleName, Dictionary<string, LangValueType> symbols, SourcePosition position = default)
        : base(moduleName, position)
    {
        AddSymbols(symbols);
        OnLoadSuccess();
    }

    /// <summary>
    /// 加载模块（实际在构造函数中已完成）
    /// </summary>
    /// <param name="manager">变量管理器</param>
    protected override void LoadModule(VariateManager manager)
    {
        if (IsLoaded) return; // 已经在构造函数中加载

        // 创建导入语句并执行
        var importStatement = new ImportStatement(ModuleName, _position);
        importStatement.Run(manager);

        // 提取模块中的所有符号
        ExtractModuleSymbols(manager);

        OnLoadSuccess();
    }

    /// <summary>
    /// 从变量管理器中提取模块符号
    /// </summary>
    /// <param name="manager">变量管理器</param>
    private void ExtractModuleSymbols(VariateManager manager)
    {
        if (manager.Scopes.Count == 0) return;

        var currentScope = manager.Scopes[^1];
        var moduleBaseName = Path.GetFileNameWithoutExtension(ModuleName);

        // 收集所有符号，但排除模块对象本身以避免循环引用
        foreach (var kvp in currentScope)
        {
            var symbolName = kvp.Key;
            var symbolValue = kvp.Value;

            // 跳过模块自身的引用
            if (string.Equals(symbolName, moduleBaseName, StringComparison.OrdinalIgnoreCase))
                continue;

            // 跳过其他模块对象
            if (symbolValue is IModuleObject)
                continue;

            AddSymbol(symbolName, symbolValue);
        }
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        var symbolCount = GetExportedSymbols().Count();
        return $"<module {ModuleName} ({symbolCount} symbols)>";
    }
}