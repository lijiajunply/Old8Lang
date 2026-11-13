using System.Globalization;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.InstanceMethods.Implementations.Array;

/// <summary>
/// Array 排序类实例方法（QuickSort/HeapSort/SelectionSort/InsertionSort/MergeSort/BubbleSort）
/// 在 VM 模式下的共享支持逻辑。
///
/// VM 中数组字面量 `[3, 1, 2]` 的运行时类型是原生 <c>object?[]</c>，元素是装箱的原生值
/// （int/double/string/bool/char/…），因此这里提供：
/// <list type="bullet">
///   <item>把接收者归一化成 <c>List&lt;object?&gt;</c>（兼容 object?[]、List&lt;object?&gt;、ILangList/ArrayLangValue）；</item>
///   <item>原生比较（数值按数值、string 按长度再按序号、bool 按 false&lt;true、char 按字符序，其它类型安全兜底）；</item>
///   <item>交换等小工具。</item>
/// </list>
/// 排序结果统一以 <c>object?[]</c> 返回，保持「数组进、数组出」。
/// </summary>
/// <remarks>
/// 字符串按 <c>Value.Length</c> 比较，是为了与解释器/IL 模式下
/// <c>StringLangValue.Less/Greater</c> 的语义保持一致（本项目把跨模式语义分歧视为待修问题）。
/// </remarks>
internal static class ArraySortVmSupport
{
    /// <summary>
    /// 把 VM 接收者归一化成可排序的 <see cref="List{T}"/>（元素保持原生装箱值）。
    /// </summary>
    /// <exception cref="ArgumentException">接收者既不是数组也不是列表时抛出。</exception>
    public static List<object?> Normalize(object? instance)
    {
        switch (instance)
        {
            case object?[] array:
                return [.. array];
            case List<object?> list:
                return [.. list];
            case ILangList langList:
                // ArrayLangValue / ListLangValue 等走这里，元素是 LangValueType
                return langList.GetItems().Cast<object?>().ToList();
            case null:
                return [];
            default:
                throw new ArgumentException(
                    $"实例必须是数组或列表类型，实际类型为 {instance.GetType().Name}");
        }
    }

    /// <summary>归一化后排序，并以 <c>object?[]</c> 返回。</summary>
    public static object?[] SortAndReturn(object? instance, Action<List<object?>> sort)
    {
        var items = Normalize(instance);
        sort(items);
        return [.. items];
    }

    public static bool Less(object? a, object? b) => Compare(a, b) < 0;

    public static void Swap(List<object?> items, int i, int j)
    {
        if (i == j) return;
        (items[i], items[j]) = (items[j], items[i]);
    }

    /// <summary>
    /// 原生比较，返回负/零/正。null 排最前；无法比较时退化为字符串比较，保证不抛异常。
    /// </summary>
    public static int Compare(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return 0;
        if (a is null) return -1;
        if (b is null) return 1;

        // 数值：整数之间用 decimal（覆盖 long/ulong 精度），涉及浮点时用 double
        if (IsNumeric(a) && IsNumeric(b))
        {
            if (IsIntegral(a) && IsIntegral(b))
                return Convert.ToDecimal(a, CultureInfo.InvariantCulture)
                    .CompareTo(Convert.ToDecimal(b, CultureInfo.InvariantCulture));

            return Convert.ToDouble(a, CultureInfo.InvariantCulture)
                .CompareTo(Convert.ToDouble(b, CultureInfo.InvariantCulture));
        }

        // bool：false < true
        if (a is bool ba && b is bool bb) return ba.CompareTo(bb);

        // char：按字符序（对齐 CharLangValue.Less/Greater）
        if (a is char ca && b is char cb) return ca.CompareTo(cb);

        // string：按长度比较（对齐 StringLangValue.Less/Greater 的解释器语义，跨模式一致）
        if (a is string sa && b is string sb) return CompareString(sa, sb);

        // char 与 string 混合：把 char 当单字符字符串，同样按长度
        if (a is char c1 && b is string sb2) return CompareString(c1.ToString(), sb2);
        if (a is string sa2 && b is char c2) return CompareString(sa2, c2.ToString());

        // LangValueType 字符串：同样按长度，保证与原生字符串路径一致
        if (a is StringLangValue lsa && b is StringLangValue lsb)
            return CompareString(lsa.Value, lsb.Value);

        return FallbackCompare(a, b);
    }

    /// <summary>
    /// 字符串比较：先比长度（与解释器/IL 的 <c>StringLangValue.Less</c> 语义一致），
    /// 长度相同时再按序号比较，保证排序结果确定（不会因算法不同而顺序不同）。
    /// </summary>
    private static int CompareString(string x, string y)
    {
        var byLength = x.Length.CompareTo(y.Length);
        return byLength != 0 ? byLength : string.CompareOrdinal(x, y);
    }

    /// <summary>
    /// 其它类型兜底：先尝试 LangValueType.ObjToValue(...) 后比 Less/Greater，
    /// 再退化为字符串比较。任何异常都被吞掉，只保证「不抛异常、可比较」。
    /// </summary>
    private static int FallbackCompare(object a, object b)
    {
        try
        {
            var la = a as LangValueType ?? LangValueType.ObjToValue(a);
            var lb = b as LangValueType ?? LangValueType.ObjToValue(b);

            if (la.Less(lb)) return -1;
            if (la.Greater(lb)) return 1;
            return 0;
        }
        catch
        {
            // 忽略，走字符串兜底
        }

        try
        {
            var sa = Convert.ToString(a, CultureInfo.InvariantCulture) ?? string.Empty;
            var sb = Convert.ToString(b, CultureInfo.InvariantCulture) ?? string.Empty;
            return string.CompareOrdinal(sa, sb);
        }
        catch
        {
            return 0;
        }
    }

    private static bool IsNumeric(object value) => value
        is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static bool IsIntegral(object value) => value
        is byte or sbyte or short or ushort or int or uint or long or ulong;
}
