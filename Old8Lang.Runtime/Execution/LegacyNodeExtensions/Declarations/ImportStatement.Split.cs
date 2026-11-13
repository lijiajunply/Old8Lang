using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.ModuleSystem.StandardLibrary;
using Old8Lang.ModuleSystem.Core;
using Old8Lang.ModuleSystem.Resolution;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// ImportStatement 的执行/编译辅助实现（原位于 AST 目录，阶段 1 迁出）。
/// </summary>
public partial class ImportStatement
{
    /// <summary>
    /// 处理懒导入
    /// </summary>
    /// <param name="manager">变量管理器</param>
    internal void HandleLazyImport(VariateManager manager)
    {
        // 检查是否启用新的统一工厂
        // 使用新的统一模块工厂创建模块对象
        UnifiedModule moduleValue;

        if (_isSelective && _importSpecifiers.Count > 0)
        {
            // 选择性导入
            var selectedSymbols = _importSpecifiers.Select(item => item.Alias).ToList();
            moduleValue = ModuleFactory.CreateSelectiveModule(_importString, selectedSymbols, manager, Position);
        }
        else
        {
            // 懒加载模块
            moduleValue = ModuleFactory.CreateLazyModule(_importString, manager, Position);
        }

        // 注册模块对象到变量管理器
        RegisterModuleValue(moduleValue, manager);
    }
    /// <summary>
    /// 注册模块值对象到变量管理器
    /// </summary>
    /// <param name="moduleValue">模块值对象</param>
    /// <param name="manager">变量管理器</param>
    internal void RegisterModuleValue(UnifiedModule moduleValue, VariateManager manager)
    {
        ArgumentNullException.ThrowIfNull(moduleValue);
        if (_moduleAlias is not null)
        {
            manager.Scopes[^1][_moduleAlias] = moduleValue; // 带别名的导入：使用别名注册
        }
        else if (_isSelective && _importSpecifiers.Count > 0)
        {
            // 选择性懒导入：创建符号代理，延迟加载到实际访问时
            if (_isLazy)
            {
                // 懒加载选择性导入：创建代理符号
                foreach (var specifier in _importSpecifiers)
                {
                    var symbolName = specifier.Alias;
                    var originalName = specifier.Name;

                    // 创建懒加载代理：使用 LazySymbolProxy
                    var proxy = new LazySymbolProxy(moduleValue, originalName, Position);
                    manager.Scopes[^1][symbolName] = proxy;
                }
            }
            else
            {
                // 即时加载选择性导入：直接获取符号
                foreach (var specifier in _importSpecifiers)
                {
                    var symbolName = specifier.Alias;
                    var symbol = moduleValue.GetSymbol(specifier.Name);
                    if (symbol is not null)
                    {
                        manager.Scopes[^1][symbolName] = symbol;
                    }
                    else
                    {
                        throw new ImportError(this, specifier.Name,
                            $"Symbol '{specifier.Name}' not found in module '{moduleValue.ModuleName}'");
                    }
                }
            }
        }
        else if (!_fromClause && _moduleAlias is null && _isLazy)
        {
            // 通配符懒导入：lazy import "module" (无别名、无 from 子句)
            // 将所有符号以代理形式注册到当前作用域
            // 由于无法在不加载模块的情况下知道有哪些符号，我们采用延迟策略：
            // 注册一个特殊的标记，当符号查找失败时，尝试从懒加载模块中查找
            manager.AddLazyWildcardModule(moduleValue);
        }
        else
        {
            // 普通导入：使用模块名注册
            var moduleName = Path.GetFileNameWithoutExtension(_importString.Trim('"'));
            manager.Scopes[^1][moduleName] = moduleValue;
        }
    }
    /// <summary>
    /// 处理动态导入
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <exception cref="ImportError">当动态导入失败时抛出</exception>
    internal void HandleDynamicImport(VariateManager manager)
    {
        if (_dynamicModuleExpression is null)
        {
            throw new ImportError(this, _importString, "Dynamic import expression is null");
        }

        try
        {
            // 运行动态模块表达式来获取模块名
            var dynamicResult = _dynamicModuleExpression.Accept(new InterpreterVisitor(manager));

            if (dynamicResult is StringLangValue stringModuleValue)
            {
                var actualModuleName = stringModuleValue.Value;

                // 使用新的统一模块工厂创建模块对象
                UnifiedModule unifiedModule;

                if (_isSelective && _importSpecifiers.Count > 0)
                {
                    // 选择性导入
                    var selectedSymbols = _importSpecifiers.Select(item => item.Alias).ToList();
                    unifiedModule =
                        ModuleFactory.CreateSelectiveModule(actualModuleName, selectedSymbols, manager, Position);
                }
                else
                {
                    // 动态导入创建即时加载模块（因为已经确定要导入）
                    unifiedModule = ModuleFactory.CreateEagerModule(actualModuleName, manager, Position);
                }

                // 注册模块对象到变量管理器
                RegisterModuleValue(unifiedModule, manager);
            }
            else
            {
                throw new ImportError(this, _importString,
                    $"Dynamic import expression must evaluate to a string, got {dynamicResult.GetType().Name}");
            }
        }
        catch (ImportError)
        {
            // 重新抛出导入错误
            throw;
        }
        catch (Exception ex)
        {
            // 包装其他异常为导入错误
            throw new ImportError(this, _importString, $"Dynamic import failed: {ex.Message}");
        }
    }
}
