using System.Globalization;
using Old8Lang.AST.Expression;

namespace Old8Lang.Runtime;

/// <summary>
/// 虚拟机模式下的值显示格式化器。
/// </summary>
/// <remarks>
/// 虚拟机里的集合是原生 <c>List&lt;object?&gt;</c> / <c>object[]</c> /
/// <c>Dictionary&lt;object, object?&gt;</c>，直接 <c>ToString()</c> 会把
/// <c>System.Collections.Generic.List`1[System.Object]</c> 这类 .NET 类型名打印给使用者。
/// 解释器侧走 <see cref="LangValueType.ToDisplayString"/>，这里给出同一口径的实现，
/// 供 <c>PrintLine</c> / <c>Print</c> / <c>Error</c> / <c>ToStr()</c> 共用，
/// 保证「直接打印」与「先 ToStr 再打印」结果一致。
/// </remarks>
public static class VMValueFormatter
{
    /// <summary>
    /// 把一个运行期值格式化成语言侧的显示文本
    /// </summary>
    public static string ToDisplayString(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        // 对于 double，如果是整数值，使用固定格式（不使用科学计数法）
        if (value is double d)
        {
            return Math.Abs(d - Math.Round(d)) < 0.0000001
                ? d.ToString("F0")
                : d.ToString(CultureInfo.InvariantCulture);
        }

        if (value is long l) return l.ToString();
        if (value is int i) return i.ToString();
        if (value is bool b) return b ? "true" : "false";
        if (value is char c) return c.ToString();
        if (value is string str) return str;

        // 集合与元组：递归格式化（元素里的字符串带引号，与解释器一致）
        if (value is Dictionary<object, object?> dict)
        {
            return "{" + string.Join(", ", dict.Select(kvp =>
                $"{FormatValueForDisplay(kvp.Key)}: {FormatValueForDisplay(kvp.Value)}")) + "}";
        }

        if (value is object?[] array)
        {
            return "[" + string.Join(", ", array.Select(FormatValueForDisplay)) + "]";
        }

        if (value is List<object?> list)
        {
            return "[" + string.Join(", ", list.Select(FormatValueForDisplay)) + "]";
        }

        if (IsTuple(value))
        {
            return FormatTuple((Tuple<object?, object?>)value);
        }

        // 对于 Task 类型（System.Threading.Tasks.Task）
        if (value is System.Threading.Tasks.Task task)
        {
            return task.Status switch
            {
                System.Threading.Tasks.TaskStatus.Created => "Task(Status: Created)",
                System.Threading.Tasks.TaskStatus.WaitingForActivation => "Task(Status: WaitingForActivation)",
                System.Threading.Tasks.TaskStatus.WaitingToRun => "Task(Status: WaitingToRun)",
                System.Threading.Tasks.TaskStatus.Running => "Task(Status: Running)",
                System.Threading.Tasks.TaskStatus.RanToCompletion => "Task(Status: Completed)",
                System.Threading.Tasks.TaskStatus.Canceled => "Task(Status: Canceled)",
                System.Threading.Tasks.TaskStatus.Faulted =>
                    $"Task(Status: Faulted, Exception: {task.Exception?.GetBaseException().Message ?? "Unknown"})",
                _ => "Task(Status: Unknown)"
            };
        }

        // 对于 Thread 类型（System.Threading.Thread）
        if (value is System.Threading.Thread thread)
        {
            var status = thread.IsAlive ? "Alive" : "Stopped";
            return $"Thread(Id: {thread.ManagedThreadId}, Status: {status})";
        }

        // 对于 CancellationToken 类型
        if (value is CancellationToken token)
        {
            return token.IsCancellationRequested
                ? "CancellationToken(Canceled)"
                : "CancellationToken(Active)";
        }

        // 对于 Enum 类型
        if (value.GetType().IsEnum)
        {
            return value.ToString() ?? "Enum";
        }

        // 对于 LangValueType，使用 ToDisplayString
        if (value is LangValueType langValue)
        {
            return langValue.ToDisplayString();
        }

        // 对于其他类型，使用默认的 ToString
        return value.ToString() ?? "null";
    }

    /// <summary>
    /// 格式化集合元素用于显示（递归处理嵌套集合）
    /// </summary>
    public static string FormatValueForDisplay(object? value)
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

        if (value is int i) return i.ToString();
        if (value is long l) return l.ToString();
        if (value is char c) return c.ToString();

        // double 格式化
        if (value is double d)
        {
            return Math.Abs(d - Math.Round(d)) < 0.0000001
                ? d.ToString("F0")
                : d.ToString(CultureInfo.InvariantCulture);
        }

        // 递归处理嵌套集合
        if (value is Dictionary<object, object?> || value is object?[] || value is List<object?> || IsTuple(value))
        {
            return ToDisplayString(value);
        }

        // LangValueType 使用 ToDisplayString
        if (value is LangValueType langValue)
        {
            return langValue.ToDisplayString();
        }

        return value.ToString() ?? "null";
    }

    /// <summary>
    /// 是否是虚拟机用来表示元组的嵌套 <c>Tuple&lt;object?, object?&gt;</c>
    /// </summary>
    private static bool IsTuple(object value)
    {
        var type = value.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Tuple<,>);
    }

    /// <summary>
    /// 格式化虚拟机元组。
    /// </summary>
    /// <remarks>
    /// 虚拟机把 <c>(1, 2, 3)</c> 编码成 <c>Tuple(1, Tuple(2, 3))</c>，
    /// 直接按两个字段打印会得到 <c>(1, (2, 3))</c>，与解释器的 <c>(1, 2, 3)</c> 不一致，
    /// 因此这里沿 Item2 展开成扁平形式（与 <c>Count()</c> / 切片的展平口径相同）。
    /// 代价是 <c>(1, (2, 3))</c> 与 <c>(1, 2, 3)</c> 在虚拟机里本就同构，无法区分。
    /// </remarks>
    private static string FormatTuple(Tuple<object?, object?> tuple)
    {
        var parts = new List<string>();
        object? current = tuple;
        while (current is not null && IsTuple(current))
        {
            var item = (Tuple<object?, object?>)current;
            parts.Add(FormatValueForDisplay(item.Item1));
            current = item.Item2;
        }

        if (current is not null)
        {
            parts.Add(FormatValueForDisplay(current));
        }

        return "(" + string.Join(", ", parts) + ")";
    }
}
