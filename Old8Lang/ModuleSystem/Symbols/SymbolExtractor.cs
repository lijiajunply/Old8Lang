using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.ModuleSystem.Symbols;

/// <summary>
/// 符号提取器 - 从变量管理器中提取模块导出的符号
/// </summary>
public class SymbolExtractor
{
    /// <summary>
    /// 从变量管理器中提取所有导出的符号
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="moduleBaseName">模块基础名称（用于过滤）</param>
    /// <param name="selectedSymbols">选择性导入的符号列表（如果为空或null则导出所有）</param>
    /// <returns>符号字典</returns>
    public Dictionary<string, LangValueType> ExtractSymbols(
        VariateManager manager,
        string? moduleBaseName = null,
        List<string>? selectedSymbols = null)
    {
        var symbols = new Dictionary<string, LangValueType>();
        var isSelective = selectedSymbols is { Count: > 0 };

        // 1. 从作用域中提取变量和常量
        ExtractFromScopes(manager, symbols, moduleBaseName, selectedSymbols, isSelective);

        // 2. 从 ImportInfos 中提取函数和类
        ExtractFromImportInfos(manager, symbols, moduleBaseName, selectedSymbols, isSelective);

        return symbols;
    }

    /// <summary>
    /// 从作用域提取符号
    /// </summary>
    private void ExtractFromScopes(
        VariateManager manager,
        Dictionary<string, LangValueType> symbols,
        string? moduleBaseName,
        List<string>? selectedSymbols,
        bool isSelective)
    {
        foreach (var scope in manager.Scopes)
        {
            foreach (var (symbolName, symbolValue) in scope)
            {
                // 跳过模块自身引用
                if (!string.IsNullOrEmpty(moduleBaseName) &&
                    string.Equals(symbolName, moduleBaseName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // 跳过其他模块对象
                if (symbolValue is AST.Expression.ModuleObjects.IModuleObject)
                {
                    continue;
                }

                // 如果是选择性导入，只添加指定的符号
                if (isSelective)
                {
                    if (selectedSymbols!.Contains(symbolName))
                    {
                        symbols[symbolName] = symbolValue;
                    }
                }
                else
                {
                    symbols[symbolName] = symbolValue;
                }
            }
        }
    }

    /// <summary>
    /// 从 ImportInfos 提取符号
    /// </summary>
    private void ExtractFromImportInfos(
        VariateManager manager,
        Dictionary<string, LangValueType> symbols,
        string? moduleBaseName,
        List<string>? selectedSymbols,
        bool isSelective)
    {
        foreach (var importInfo in manager.ImportInfos)
        {
            string? symbolName = GetSymbolName(importInfo);

            if (symbolName != null)
            {
                // 跳过模块自身引用
                if (!string.IsNullOrEmpty(moduleBaseName) &&
                    string.Equals(symbolName, moduleBaseName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // 如果是选择性导入，只添加指定的符号
                if (isSelective)
                {
                    if (selectedSymbols!.Contains(symbolName))
                    {
                        symbols[symbolName] = importInfo;
                    }
                }
                else
                {
                    symbols[symbolName] = importInfo;
                }
            }
        }
    }

    /// <summary>
    /// 从 ImportInfo 获取符号名称
    /// </summary>
    private string? GetSymbolName(ImportInfo importInfo)
    {
        return importInfo switch
        {
            FuncLangValue { Id: not null } func => func.Id.IdName,
            AsyncFuncLangValue { Id: not null } asyncFunc => asyncFunc.Id.IdName,
            TypeTemplate template => template.ClassName,
            NativeAnyLangValue nativeAny => nativeAny.RegisterName,
            NativeStaticAny staticAny => staticAny.ClassName,
            ConstantLangValue constant => constant.Name,
            AST.Expression.ModuleObjects.UnifiedModule module => module.ModuleName,
            _ => null
        };
    }

    /// <summary>
    /// 提取指定符号（用于命名导入）
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="symbolNames">要提取的符号名称列表</param>
    /// <param name="moduleName">模块名称（用于错误提示）</param>
    /// <param name="scopedImportInfos">限定范围的 ImportInfos（如果为 null 则使用全局 ImportInfos）</param>
    /// <returns>符号字典</returns>
    /// <exception cref="Error.ImportError">当指定的符号不存在时抛出</exception>
    public Dictionary<string, LangValueType> ExtractSpecificSymbols(
        VariateManager manager,
        IEnumerable<string> symbolNames,
        string? moduleName = null,
        IEnumerable<ImportInfo>? scopedImportInfos = null)
    {
        var symbols = new Dictionary<string, LangValueType>();
        var currentScope = manager.Scopes.Count > 0 ? manager.Scopes[^1] : null;
        var notFoundSymbols = new List<string>();

        // 使用限定范围的 ImportInfos（如果提供），否则使用全局的
        var importInfosToSearch = scopedImportInfos ?? manager.ImportInfos;

        foreach (var symbolName in symbolNames)
        {
            // 1. 从当前作用域查找
            if (currentScope != null && currentScope.TryGetValue(symbolName, out var value))
            {
                symbols[symbolName] = value;
                continue;
            }

            // 2. 从限定范围的 ImportInfos 查找
            var importInfo = importInfosToSearch.FirstOrDefault(info =>
            {
                var name = GetSymbolName(info);
                return name == symbolName;
            });

            if (importInfo != null)
            {
                symbols[symbolName] = importInfo;
                continue;
            }

            // 3. 仅在没有提供 scopedImportInfos 时才从全局作用域查找
            // 如果提供了 scopedImportInfos，说明我们要限定在特定模块的符号中查找
            if (scopedImportInfos == null)
            {
                var globalValue = manager.GetValue(new LangId(symbolName));
                if (globalValue != null)
                {
                    symbols[symbolName] = globalValue;
                    continue;
                }
            }

            // 如果所有地方都找不到，记录下来
            notFoundSymbols.Add(symbolName);
        }

        // 如果有符号找不到，抛出 ImportError
        if (notFoundSymbols.Count > 0)
        {
            var missingSymbolsStr = string.Join(", ", notFoundSymbols);
            var moduleInfo = string.IsNullOrEmpty(moduleName) ? "module" : $"module '{moduleName}'";
            throw new Error.ImportError(
                default,
                moduleName ?? "unknown",
                $"Cannot import symbols [{missingSymbolsStr}] from {moduleInfo}: symbols do not exist");
        }

        return symbols;
    }

    /// <summary>
    /// 将作用域中的常量包装为 ConstantLangValue 并添加到 ImportInfos
    /// 用于通配符导入，将所有可导出内容（包括常量）添加到 ImportInfos 中
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="newImportInfos">新增的 ImportInfo 列表（输出参数）</param>
    public void WrapConstantsAsImportInfo(VariateManager manager, List<ImportInfo> newImportInfos)
    {
        var currentScope = manager.Scopes.Count > 0 ? manager.Scopes[^1] : null;
        if (currentScope == null) return;

        foreach (var (symbolName, symbolValue) in currentScope)
        {
            // 跳过已经是 ImportInfo 的项（函数、类等）
            if (symbolValue is ImportInfo)
            {
                continue;
            }

            // 跳过模块对象
            if (symbolValue is AST.Expression.ModuleObjects.IModuleObject)
            {
                continue;
            }

            // 将常量包装为 ConstantLangValue 并添加到 ImportInfos
            var constantValue = new ConstantLangValue(symbolName, symbolValue);
            manager.AddClassAndFunc(constantValue);
            newImportInfos.Add(constantValue);
        }
    }
}
