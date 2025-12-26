using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 懒加载符号代理 - 用于延迟加载模块中的单个符号
/// 当符号被访问时才触发模块加载
/// </summary>
public class LazySymbolProxy(
    UnifiedModule module,
    string symbolName,
    SourcePosition position = default
) : LangValueType(position)
{
    private readonly UnifiedModule _module = module;
    private readonly string _symbolName = symbolName;
    private LangValueType? _cachedSymbol;
    private bool _isResolved;
    private readonly object _resolveLock = new();

    /// <summary>
    /// 解析并获取真实符号
    /// </summary>
    /// <returns>真实符号值</returns>
    private LangValueType ResolveSymbol()
    {
        if (!_isResolved)
        {
            lock (_resolveLock)
            {
                if (!_isResolved)
                {
                    _cachedSymbol = _module.GetSymbol(_symbolName);
                    if (_cachedSymbol == null)
                    {
                        throw new AttributeError(this, _symbolName, _module.ModuleName);
                    }
                    _isResolved = true;
                }
            }
        }
        return _cachedSymbol!;
    }

    /// <summary>
    /// 运行符号 - 对于常量直接返回值，对于函数返回函数对象
    /// </summary>
    public override LangValueType Run(VariateManager manager)
    {
        return ResolveSymbol();
    }

    /// <summary>
    /// 处理点访问 - 转发到真实符号
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        var realSymbol = ResolveSymbol();
        return realSymbol.Dot(dotExpression, manager);
    }

    /// <summary>
    /// 字符串表示
    /// </summary>
    public override string ToString()
    {
        if (_isResolved && _cachedSymbol != null)
        {
            return _cachedSymbol.ToString() ?? $"<lazy symbol proxy: {_symbolName} (resolved)>";
        }
        return $"<lazy symbol proxy: {_symbolName} from {_module.ModuleName}>";
    }

    /// <summary>
    /// Visitor 模式支持
    /// </summary>
    public override TResult Accept<TResult>(Visitor.IVisitor<TResult> visitor)
    {
        // 懒加载代理透明转发到真实符号
        var realSymbol = ResolveSymbol();
        return realSymbol.Accept(visitor);
    }
}
