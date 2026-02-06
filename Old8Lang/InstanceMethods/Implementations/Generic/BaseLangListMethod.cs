using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
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

    /// <summary>
    /// 从 VM 模式下的实例获取元素列表（支持 object[], List<object?>, ILangList, Tuple）
    /// </summary>
    protected List<object?> GetItemsForVM(object? instance)
    {
        if (instance is ILangList langList)
        {
            return langList.GetItems().Cast<object?>().ToList();
        }
        else if (instance is object[] arr)
        {
            return arr.ToList();
        }
        else if (instance is List<object?> list)
        {
            return list;
        }
        else if (instance is System.Collections.IList ilist)
        {
            return ilist.Cast<object?>().ToList();
        }
        // 支持 Tuple<object?, object?> (VM 模式下的元组表示)
        else if (instance?.GetType().IsGenericType == true &&
                 instance.GetType().GetGenericTypeDefinition() == typeof(Tuple<,>))
        {
            return FlattenTuple(instance);
        }
        throw new ArgumentException($"实例必须实现 ILangList 接口或是数组/列表/元组类型，当前类型：{instance?.GetType().Name}");
    }

    /// <summary>
    /// 从 VM 模式下的实例获取长度（支持 object[], List<object?>, ILangList, Tuple）
    /// </summary>
    protected int GetLengthForVM(object? instance)
    {
        if (instance is ILangList langList)
        {
            return langList.GetLength();
        }
        else if (instance is object[] arr)
        {
            return arr.Length;
        }
        else if (instance is List<object?> list)
        {
            return list.Count;
        }
        else if (instance is System.Collections.ICollection collection)
        {
            return collection.Count;
        }
        // 支持 Tuple<object?, object?> (VM 模式下的元组表示)
        else if (instance?.GetType().IsGenericType == true &&
                 instance.GetType().GetGenericTypeDefinition() == typeof(Tuple<,>))
        {
            return FlattenTuple(instance).Count;
        }
        throw new ArgumentException($"实例必须实现 ILangList 接口或是数组/列表/元组类型，当前类型：{instance?.GetType().Name}");
    }

    /// <summary>
    /// 将嵌套的 Tuple<object?, object?> 展平为列表
    /// VM 模式下，(1, 2, 3) 表示为 Tuple<object?, object?>(1, Tuple<object?, object?>(2, 3))
    /// </summary>
    private static List<object?> FlattenTuple(object tuple)
    {
        var result = new List<object?>();

        if (tuple is not Tuple<object?, object?> t)
        {
            return result;
        }

        // 添加第一个元素
        result.Add(t.Item1);

        // 如果第二个元素也是 Tuple，递归展平
        if (t.Item2?.GetType().IsGenericType == true &&
            t.Item2.GetType().GetGenericTypeDefinition() == typeof(Tuple<,>))
        {
            result.AddRange(FlattenTuple(t.Item2));
        }
        else if (t.Item2 != null)
        {
            // 否则直接添加第二个元素（如果不是 null）
            result.Add(t.Item2);
        }

        return result;
    }
}
