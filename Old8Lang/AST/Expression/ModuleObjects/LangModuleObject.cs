using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 简单模块对象，用于处理带别名的模块导入
/// </summary>
public class LangModuleObject(VariateManager manager, SourcePosition position = default) : LangValueType(position), IModuleValueType
{
    /// <summary>
    /// 模块名称（基于代理模式，使用默认值）
    /// </summary>
    public string ModuleName => "module_proxy";

    /// <summary>
    /// 模块是否已加载（代理模式默认为已加载）
    /// </summary>
    public bool IsLoaded => true;

    /// <summary>
    /// 模块加载状态（代理模式默认为已加载）
    /// </summary>
    public ModuleLoadingState LoadingState => ModuleLoadingState.Loaded;

    /// <summary>
    /// 获取模块中的符号（委托给管理器）
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <returns>符号值</returns>
    public LangValueType? GetSymbol(string symbolName)
    {
        // 从管理器中查找符号
        var result = manager.GetValue(new LangId(symbolName));
        if (result != null) return result;

        // 尝试大小写不敏感匹配
        var upperName = char.ToUpper(symbolName[0]) + symbolName.Substring(1);
        return manager.GetValue(new LangId(upperName));
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
        // 获取所有可用的符号名称
        var symbols = new List<string>();
        if (manager.Scopes.Count > 0)
        {
            var currentScope = manager.Scopes[^1];
            symbols.AddRange(currentScope.Keys);
        }
        return symbols;
    }

    /// <summary>
    /// 强制加载模块（代理模式无需加载）
    /// </summary>
    /// <param name="manager">变量管理器</param>
    public void EnsureLoaded(VariateManager manager)
    {
        // 代理模式无需实际加载
    }

    /// <summary>
    /// 处理方法调用，将其转发到全局作用域
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager1)
    {
        if (dotExpression is Instance { Id: { } langId } instance)
        {
            // 获取函数名，处理大小写不敏感匹配
            var functionName = langId.IdName;

            // 1. 尝试从全局作用域中获取函数（大小写不敏感）
            var func = manager.GetValue(new LangId(functionName));
            if (func == null)
            {
                // 尝试大写开头的函数名
                var upperCaseName = char.ToUpper(functionName[0]) + functionName.Substring(1);
                func = manager.GetValue(new LangId(upperCaseName));
            }

            if (func != null)
            {
                // 直接运行函数调用，返回结果
                if (func is FuncLangValue funcValue)
                {
                    return funcValue.Run(manager, instance.Ids);
                }
            }

            // 2. 如果没有找到函数，尝试从当前作用域中获取
            func = manager1.GetValue(new LangId(functionName));
            if (func == null)
            {
                // 尝试大写开头的函数名
                var upperCaseName = char.ToUpper(functionName[0]) + functionName.Substring(1);
                func = manager1.GetValue(new LangId(upperCaseName));
            }

            if (func != null)
            {
                // 直接运行函数调用，返回结果
                if (func is FuncLangValue funcValue)
                {
                    return funcValue.Run(manager1, instance.Ids);
                }
            }

            // 3. 如果还是没有找到，尝试从导入信息中获取
            foreach (var importInfo in manager.ImportInfos)
            {
                if (importInfo is FuncLangValue funcValue &&
                    (funcValue.Id?.IdName == functionName ||
                     funcValue.Id?.IdName == char.ToUpper(functionName[0]) + functionName.Substring(1)))
                {
                    // 直接运行函数调用，返回结果
                    return funcValue.Run(manager, instance.Ids);
                }
            }
        }
        else if (dotExpression is LangId simpleLangId)
        {
            // 获取函数名，处理大小写不敏感匹配
            var functionName = simpleLangId.IdName;

            // 1. 尝试从全局作用域中获取函数
            var func = manager.GetValue(simpleLangId);
            if (func == null)
            {
                // 尝试大写开头的函数名
                var upperCaseName = char.ToUpper(functionName[0]) + functionName.Substring(1);
                func = manager.GetValue(new LangId(upperCaseName));
            }

            if (func != null)
            {
                return func;
            }

            // 2. 如果没有找到函数，尝试从当前作用域中获取
            func = manager1.GetValue(simpleLangId);
            if (func == null)
            {
                // 尝试大写开头的函数名
                var upperCaseName = char.ToUpper(functionName[0]) + functionName.Substring(1);
                func = manager1.GetValue(new LangId(upperCaseName));
            }

            if (func != null)
            {
                return func;
            }

            // 3. 如果还是没有找到，尝试从导入信息中获取
            foreach (var importInfo in manager.ImportInfos)
            {
                if (importInfo is FuncLangValue funcValue &&
                    (funcValue.Id?.IdName == functionName ||
                     funcValue.Id?.IdName == char.ToUpper(functionName[0]) + functionName.Substring(1)))
                {
                    return funcValue;
                }
            }
        }

        // 如果还是没有找到，调用父类的 Dot 方法（会报错）
        return base.Dot(dotExpression, manager1);
    }

    public override string ToString()
    {
        return "<module>";
    }
}