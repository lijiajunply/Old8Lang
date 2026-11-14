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
/// ImportStatement 的模块加载辅助实现（阶段 2 从语法节点迁出）。
/// </summary>
public static class ImportStatementRuntimeExtensions
{
    /// <summary>静态模块系统服务实例（原位于 ImportStatement）。</summary>
    internal static readonly ModuleSystemService ModuleService = new();

    /// <summary>
    /// 处理懒导入
    /// </summary>
    /// <param name="manager">变量管理器</param>
    internal static void HandleLazyImport(this ImportStatement node, VariateManager manager)
    {
        // 检查是否启用新的统一工厂
        // 使用新的统一模块工厂创建模块对象
        UnifiedModule moduleValue;

        if (node.IsSelective && node.Specifiers.Count > 0)
        {
            // 选择性导入
            var selectedSymbols = node.Specifiers.Select(item => item.Alias).ToList();
            moduleValue = ModuleFactory.CreateSelectiveModule(node.ImportString, selectedSymbols, manager, node.Position);
        }
        else
        {
            // 懒加载模块
            moduleValue = ModuleFactory.CreateLazyModule(node.ImportString, manager, node.Position);
        }

        // 注册模块对象到变量管理器
        node.RegisterModuleValue(moduleValue, manager);
    }
    /// <summary>
    /// 注册模块值对象到变量管理器
    /// </summary>
    /// <param name="moduleValue">模块值对象</param>
    /// <param name="manager">变量管理器</param>
    internal static void RegisterModuleValue(this ImportStatement node, UnifiedModule moduleValue, VariateManager manager)
    {
        ArgumentNullException.ThrowIfNull(moduleValue);
        if (node.ModuleAlias is not null)
        {
            manager.Scopes[^1][node.ModuleAlias] = moduleValue; // 带别名的导入：使用别名注册
        }
        else if (node.IsSelective && node.Specifiers.Count > 0)
        {
            // 选择性懒导入：创建符号代理，延迟加载到实际访问时
            if (node.IsLazy)
            {
                // 懒加载选择性导入：创建代理符号
                foreach (var specifier in node.Specifiers)
                {
                    var symbolName = specifier.Alias;
                    var originalName = specifier.Name;

                    // 创建懒加载代理：使用 LazySymbolProxy
                    var proxy = new LazySymbolProxy(moduleValue, originalName, node.Position);
                    manager.Scopes[^1][symbolName] = proxy;
                }
            }
            else
            {
                // 即时加载选择性导入：直接获取符号
                foreach (var specifier in node.Specifiers)
                {
                    var symbolName = specifier.Alias;
                    var symbol = moduleValue.GetSymbol(specifier.Name);
                    if (symbol is not null)
                    {
                        manager.Scopes[^1][symbolName] = symbol;
                    }
                    else
                    {
                        throw new ImportError(node, specifier.Name,
                            $"Symbol '{specifier.Name}' not found in module '{moduleValue.ModuleName}'");
                    }
                }
            }
        }
        else if (!node.FromClause && node.ModuleAlias is null && node.IsLazy)
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
            var moduleName = Path.GetFileNameWithoutExtension(node.ImportString.Trim('"'));
            manager.Scopes[^1][moduleName] = moduleValue;
        }
    }
    /// <summary>
    /// 处理动态导入
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <exception cref="ImportError">当动态导入失败时抛出</exception>
    internal static void HandleDynamicImport(this ImportStatement node, VariateManager manager)
    {
        if (node.DynamicModuleExpression is null)
        {
            throw new ImportError(node, node.ImportString, "Dynamic import expression is null");
        }

        try
        {
            // 运行动态模块表达式来获取模块名
            var dynamicResult = node.DynamicModuleExpression.Accept(new InterpreterVisitor(manager));

            if (dynamicResult is StringLangValue stringModuleValue)
            {
                var actualModuleName = stringModuleValue.Value;

                // 使用新的统一模块工厂创建模块对象
                UnifiedModule unifiedModule;

                if (node.IsSelective && node.Specifiers.Count > 0)
                {
                    // 选择性导入
                    var selectedSymbols = node.Specifiers.Select(item => item.Alias).ToList();
                    unifiedModule =
                        ModuleFactory.CreateSelectiveModule(actualModuleName, selectedSymbols, manager, node.Position);
                }
                else
                {
                    // 动态导入创建即时加载模块（因为已经确定要导入）
                    unifiedModule = ModuleFactory.CreateEagerModule(actualModuleName, manager, node.Position);
                }

                // 注册模块对象到变量管理器
                node.RegisterModuleValue(unifiedModule, manager);
            }
            else
            {
                throw new ImportError(node, node.ImportString,
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
            throw new ImportError(node, node.ImportString, $"Dynamic import failed: {ex.Message}");
        }
    }
}
