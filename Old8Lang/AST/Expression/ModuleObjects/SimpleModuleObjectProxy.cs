using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 简单模块代理对象，用于处理带别名的模块导入
/// 这是SimpleModuleObject的现代化替代品
/// </summary>
public class SimpleModuleObjectProxy : LangValueType, IModuleObject
{
    private readonly string _moduleName;
    private readonly VariateManager _sourceManager;
    private readonly SourcePosition _position;
    private readonly Dictionary<string, LangValueType> _symbolCache = new();
    private bool _initialized = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="position">源码位置</param>
    public SimpleModuleObjectProxy(string moduleName, VariateManager manager, SourcePosition position = default)
        : base(position)
    {
        _moduleName = moduleName;
        _sourceManager = manager;
        _position = position;
    }

    #region IModuleObject Implementation

    /// <summary>
    /// 模块名称
    /// </summary>
    public string ModuleName => _moduleName;

    /// <summary>
    /// 模块是否已加载
    /// </summary>
    public bool IsLoaded => _initialized;

    /// <summary>
    /// 模块加载状态
    /// </summary>
    public ModuleLoadingState LoadingState => _initialized ? ModuleLoadingState.Loaded : ModuleLoadingState.NotLoaded;

    /// <summary>
    /// 获取模块中的符号
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <returns>符号值，如果不存在返回null</returns>
    public LangValueType? GetSymbol(string symbolName)
    {
        EnsureInitialized();
        _symbolCache.TryGetValue(symbolName, out var symbol);
        return symbol;
    }

    /// <summary>
    /// 检查模块是否包含指定符号
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <returns>是否包含符号</returns>
    public bool HasSymbol(string symbolName)
    {
        EnsureInitialized();
        return _symbolCache.ContainsKey(symbolName);
    }

    /// <summary>
    /// 获取模块中所有的导出符号名称
    /// </summary>
    /// <returns>符号名称列表</returns>
    public IEnumerable<string> GetExportedSymbols()
    {
        EnsureInitialized();
        return _symbolCache.Keys;
    }

    /// <summary>
    /// 强制加载模块
    /// </summary>
    /// <param name="manager">变量管理器</param>
    public void EnsureLoaded(VariateManager manager)
    {
        EnsureInitialized();
    }

    #endregion

    #region LangValueType Overrides

    /// <summary>
    /// 处理模块成员访问
    /// </summary>
    /// <param name="dotExpression">点表达式</param>
    /// <param name="currentManager">当前变量管理器</param>
    /// <returns>符号值</returns>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager currentManager)
    {
        if (dotExpression is Instance { Id: { } langId } instance)
        {
            // 处理函数调用：module.function(args)
            var functionName = langId.IdName;

            // 1. 尝试从缓存中获取函数
            if (GetSymbol(functionName) is FuncLangValue funcValue)
            {
                return funcValue.Run(currentManager, instance.Ids);
            }

            // 2. 尝试大小写不敏感的查找
            var upperCaseName = char.ToUpper(functionName[0]) + functionName.Substring(1);
            if (GetSymbol(upperCaseName) is FuncLangValue upperFuncValue)
            {
                return upperFuncValue.Run(currentManager, instance.Ids);
            }

            // 3. 尝试从全局作用域中获取函数（兼容旧行为）
            if (_sourceManager.GetValue(new LangId(functionName)) is FuncLangValue globalFuncValue)
            {
                // 缓存结果以提高后续访问性能
                _symbolCache[functionName] = globalFuncValue;
                return globalFuncValue.Run(currentManager, instance.Ids);
            }

            if (_sourceManager.GetValue(new LangId(upperCaseName)) is FuncLangValue globalUpperFuncValue)
            {
                _symbolCache[upperCaseName] = globalUpperFuncValue;
                return globalUpperFuncValue.Run(currentManager, instance.Ids);
            }

            throw new AttributeError(this, functionName, _moduleName);
        }
        else if (dotExpression is LangId simpleLangId)
        {
            // 处理属性访问：module.property
            var propertyName = simpleLangId.IdName;

            // 1. 尝试从缓存中获取
            if (GetSymbol(propertyName) != null)
            {
                return GetSymbol(propertyName)!;
            }

            // 2. 尝试从全局作用域中获取
            var globalValue = _sourceManager.GetValue(simpleLangId);
            if (globalValue != null)
            {
                _symbolCache[propertyName] = globalValue;
                return globalValue;
            }

            throw new AttributeError(this, propertyName, _moduleName);
        }

        throw new AttributeError(this, dotExpression.ToString(), _moduleName);
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        var status = _initialized ? $"proxy" : "uninitialized";
        return $"<module proxy {_moduleName} ({status})>";
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 确保代理已初始化
    /// </summary>
    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            _initialized = true;
            // 这里可以添加预加载逻辑，如果需要的话
        }
    }

    #endregion
}