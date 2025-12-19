using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 统一的模块工厂，负责根据导入配置创建合适的模块对象
/// 提供了清晰的创建策略，支持纯模块对象和模块值对象两种模式
/// </summary>
public static class UnifiedModuleFactory
{
    /// <summary>
    /// 创建纯模块对象（不继承 LangValueType）
    /// 适用于内部模块管理，不需要作为值使用的场景
    /// 暂时返回简化实现，使用现有的 BaseModuleObject
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="importSpecifiers">导入指定项</param>
    /// <param name="fromClause">是否为from子句</param>
    /// <param name="moduleAlias">模块别名</param>
    /// <param name="isLazy">是否懒加载</param>
    /// <param name="isSelective">是否选择性导入</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>创建的纯模块对象</returns>
    public static IModuleObject CreatePureModule(
        string moduleName,
        List<ImportItem>? importSpecifiers,
        bool fromClause,
        string? moduleAlias,
        bool isLazy,
        bool isSelective,
        VariateManager manager)
    {
        // 暂时使用现有的工厂实现
        return ModuleObjectFactory.CreateModuleObject(
            moduleName,
            importSpecifiers,
            fromClause,
            moduleAlias,
            isLazy,
            isSelective,
            manager,
            default);
    }

    /// <summary>
    /// 创建模块值对象（继承 LangValueType）
    /// 适用于需要作为值使用的场景，如变量赋值、函数参数等
    /// 直接创建基于当前变量管理器状态的模块对象
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="importSpecifiers">导入指定项</param>
    /// <param name="fromClause">是否为from子句</param>
    /// <param name="moduleAlias">模块别名</param>
    /// <param name="isLazy">是否懒加载</param>
    /// <param name="isSelective">是否选择性导入</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="position">源码位置</param>
    /// <returns>创建的模块值对象</returns>
    public static IModuleValueType CreateModuleValue(
        string moduleName,
        List<ImportItem>? importSpecifiers,
        bool fromClause,
        string? moduleAlias,
        bool isLazy,
        bool isSelective,
        VariateManager manager,
        SourcePosition position = default)
    {
        // 对于懒加载，我们仍然需要使用现有的工厂
        if (isLazy)
        {
            var moduleObject = ModuleObjectFactory.CreateModuleObject(
                moduleName,
                importSpecifiers,
                fromClause,
                moduleAlias,
                isLazy: true,
                isSelective,
                manager,
                position);

            if (moduleObject is IModuleValueType moduleValue)
            {
                return moduleValue;
            }

            return new ModuleValueAdapter(moduleObject, position);
        }

        // 对于非懒加载，我们假设模块已经被加载到当前作用域中
        // 创建一个基于当前管理器状态的模块对象
        return new LangModuleObject(manager, position);
    }
}

/// <summary>
/// 模块值适配器，将 IModuleObject 转换为 IModuleValueType
/// </summary>
public class ModuleValueAdapter : ModuleValueBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="moduleObject">被适配的模块对象</param>
    /// <param name="position">源码位置</param>
    public ModuleValueAdapter(IModuleObject moduleObject, SourcePosition position = default)
        : base(moduleObject, position)
    {
    }

    /// <summary>
    /// 处理模块成员访问，委托给被适配的模块对象
    /// </summary>
    /// <param name="dotExpression">点表达式</param>
    /// <param name="currentManager">当前变量管理器</param>
    /// <returns>符号值</returns>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager currentManager)
    {
        if (dotExpression is LangId langId)
        {
            var symbolName = langId.IdName;
            var symbol = GetSymbol(symbolName);

            if (symbol != null)
            {
                return symbol;
            }

            throw new AttributeError(this, symbolName, ModuleName);
        }

        if (dotExpression is Instance instance)
        {
            var functionName = instance.Id?.IdName;
            if (!string.IsNullOrEmpty(functionName))
            {
                var func = GetSymbol(functionName);

                if (func is FuncLangValue funcValue)
                {
                    return funcValue.Run(currentManager, instance.Ids);
                }

                throw new AttributeError(this, functionName, ModuleName);
            }
        }

        throw new AttributeError(this, dotExpression.ToString() ?? "", ModuleName);
    }
}
