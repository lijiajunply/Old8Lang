using Old8Lang.AST.Expression.Value;
using Old8Lang.InstanceMethods.Implementations.Generic;

namespace Old8Lang.InstanceMethods.Implementations.Tuple;

// 基础方法
public class TupleCountMethod : LangListCountMethod { public override Type TargetType => typeof(TupleLangValue); }

// 查询和访问方法
public class TupleFirstMethod : LangListFirstMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleFirstOrDefaultMethod : LangListFirstOrDefaultMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleLastMethod : LangListLastMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleLastOrDefaultMethod : LangListLastOrDefaultMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleSkipMethod : LangListSkipMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleTakeMethod : LangListTakeMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleDistinctMethod : LangListDistinctMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleFindMethod : LangListFindMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleConcatMethod : LangListConcatMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleIndexOfMethod : LangListIndexOfMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleElementAtMethod : LangListElementAtMethod { public override Type TargetType => typeof(TupleLangValue); }

// 聚合方法
public class TupleSumMethod : LangListSumMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleAverageMethod : LangListAverageMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleMinMethod : LangListMinMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleMaxMethod : LangListMaxMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleReduceMethod : LangListReduceMethod { public override Type TargetType => typeof(TupleLangValue); }

// 迭代方法
public class TupleForEachMethod : LangListForEachMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleJoinMethod : LangListJoinMethod { public override Type TargetType => typeof(TupleLangValue); }

// 集合操作方法
public class TupleUnionMethod : LangListUnionMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleIntersectMethod : LangListIntersectMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleExceptMethod : LangListExceptMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleZipMethod : LangListZipMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleGroupByMethod : LangListGroupByMethod { public override Type TargetType => typeof(TupleLangValue); }

// 排序和其他方法
public class TupleSortMethod : LangListSortMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleIsSortedMethod : LangListIsSortedMethod { public override Type TargetType => typeof(TupleLangValue); }

/// <summary>
/// 元组的 ToStr 方法 - 覆盖以返回元组格式 (item1, item2)
/// </summary>
public class TupleToStrMethod : LangListToStrMethod
{
    public override Type TargetType => typeof(TupleLangValue);

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 对于 Tuple<object?, object?> 类型，格式化为 (item1, item2)
        if (instance?.GetType().IsGenericType == true &&
            instance.GetType().GetGenericTypeDefinition() == typeof(Tuple<,>))
        {
            var item1 = instance.GetType().GetProperty("Item1")?.GetValue(instance);
            var item2 = instance.GetType().GetProperty("Item2")?.GetValue(instance);
            var item1Str = FormatValueForDisplay(item1);
            var item2Str = FormatValueForDisplay(item2);
            return $"({item1Str}, {item2Str})";
        }

        // 使用基类的辅助方法获取元素列表（支持 object[], List<object?>, ILangList）
        var items = GetItemsForVM(instance);
        var strings = items.Select(item => FormatValueForDisplay(item));
        return "(" + string.Join(", ", strings) + ")";
    }

    /// <summary>
    /// 格式化值用于显示
    /// </summary>
    private static string FormatValueForDisplay(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        // 字符串需要加引号
        if (value is string str)
        {
            return $"\"{str}\"";
        }

        // bool 使用小写
        if (value is bool b)
        {
            return b ? "true" : "false";
        }

        // 其他类型使用 ToString
        return value.ToString() ?? "null";
    }
}

// 高级组合方法
public class TupleZip3Method : LangListZip3Method { public override Type TargetType => typeof(TupleLangValue); }
public class TupleSelectManyMethod : LangListSelectManyMethod { public override Type TargetType => typeof(TupleLangValue); }

// 带选择器的聚合方法
public class TupleSortByMethod : LangListSortByMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleSumWithSelectorMethod : LangListSumWithSelectorMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleAverageWithSelectorMethod : LangListAverageWithSelectorMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleMinWithSelectorMethod : LangListMinWithSelectorMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleMaxWithSelectorMethod : LangListMaxWithSelectorMethod { public override Type TargetType => typeof(TupleLangValue); }
