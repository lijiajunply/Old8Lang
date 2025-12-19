using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 懒加载模块对象，只在首次访问时加载模块内容
/// </summary>
public class LazyModuleObject : BaseModuleObject
{
    private readonly VariateManager SourceManager;
    private readonly SourcePosition _position;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="position">源码位置</param>
    public LazyModuleObject(string moduleName, VariateManager manager, SourcePosition position = default)
        : base(moduleName, position)
    {
        SourceManager = manager;
        _position = position;
        // 不在构造函数中加载模块
    }

    /// <summary>
    /// 加载模块（在首次访问时执行）
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
            var importStatement = new ImportStatement(ModuleName, _position);
            importStatement.Run(manager);

            // 提取模块中的所有符号
            ExtractModuleSymbols(manager);

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
    /// 从变量管理器中提取模块符号
    /// </summary>
    /// <param name="manager">变量管理器</param>
    private void ExtractModuleSymbols(VariateManager manager)
    {
        if (manager.Scopes.Count == 0) return;

        var currentScope = manager.Scopes[^1];
        var moduleBaseName = Path.GetFileNameWithoutExtension(ModuleName);

        // 收集所有符号
        foreach (var kvp in currentScope)
        {
            var symbolName = kvp.Key;
            var symbolValue = kvp.Value;

            // 跳过模块自身的引用
            if (string.Equals(symbolName, moduleBaseName, StringComparison.OrdinalIgnoreCase))
                continue;

            // 跳过其他模块对象（但保留当前模块的懒包装器）
            if (symbolValue is IModuleObject && symbolValue != this)
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
        if (IsLoaded)
        {
            var symbolCount = GetExportedSymbols().Count();
            return $"<module {ModuleName} ({symbolCount} symbols)>";
        }
        return $"<module {ModuleName} (lazy)>";
    }
}