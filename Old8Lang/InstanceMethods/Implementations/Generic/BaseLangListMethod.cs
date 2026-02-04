using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.InstanceMethods.Core;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList 通用实例方法基类
/// 所有实现 ILangList 接口的类型都可以使用这些方法
/// </summary>
public abstract class BaseLangListMethod : BaseInstanceMethod
{
    /// <summary>
    /// 目标类型为 ILangList 接口
    /// 子类可以重写此属性以指定更具体的类型
    /// </summary>
    public override Type TargetType => typeof(ILangList);

    /// <summary>
    /// 从 ILangList 获取元素列表
    /// </summary>
    protected List<LangValueType> GetItems(LangValueType instance)
    {
        if (instance is ILangList langList)
        {
            return langList.GetItems().ToList();
        }
        throw new ArgumentException($"实例必须实现 ILangList 接口，当前类型：{instance.GetType().Name}");
    }

    /// <summary>
    /// 从 ILangList 获取长度
    /// </summary>
    protected int GetLength(LangValueType instance)
    {
        if (instance is ILangList langList)
        {
            return langList.GetLength();
        }
        throw new ArgumentException($"实例必须实现 ILangList 接口，当前类型：{instance.GetType().Name}");
    }

    /// <summary>
    /// 检查实例是否实现 ILangList
    /// </summary>
    protected bool IsLangList(LangValueType instance)
    {
        return instance is ILangList;
    }
}
