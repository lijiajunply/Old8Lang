using System.Globalization;
using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;
using TaskStatus = System.Threading.Tasks.TaskStatus;

namespace Old8Lang.InstanceMethods.Implementations.ValueType;

/// <summary>
/// LangValueType.ToStr() - 转换为字符串表示
/// </summary>
public class ValueTypeToStrMethod : BaseValueTypeMethod
{
    public override string[] Names => ["ToStr", "toStr", "ToString", "toString"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;
    public override Type? DeclaredReturnType => typeof(StringLangValue);

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        return new StringLangValue(instance.ToDisplayString());
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ValueTypeToStrMethod).GetMethod(nameof(ConvertToStrHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static string ConvertToStrHelper(LangValueType type)
    {
        return type.ToDisplayString();
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 处理 null
        if (instance == null)
        {
            return "null";
        }

        // 对于 double，如果是整数值，使用固定格式（不使用科学计数法）
        if (instance is double d)
        {
            if (Math.Abs(d - Math.Round(d)) < 0.0000001)
            {
                return d.ToString("F0");
            }
            return d.ToString(CultureInfo.InvariantCulture);
        }

        // 对于 long，直接转换为字符串
        if (instance is long l)
        {
            return l.ToString();
        }

        // 对于 int，直接转换为字符串
        if (instance is int i)
        {
            return i.ToString();
        }

        // 对于 bool，返回小写字符串
        if (instance is bool b)
        {
            return b ? "true" : "false";
        }

        // 对于 char，直接转换为字符串
        if (instance is char c)
        {
            return c.ToString();
        }

        // 对于 string，直接返回
        if (instance is string str)
        {
            return str;
        }

        // 对于字典类型，格式化为 {key1: value1, key2: value2}
        if (instance is Dictionary<object, object?> dict)
        {
            if (dict.Count == 0)
            {
                return "{}";
            }

            var pairs = dict.Select(kvp =>
            {
                var keyStr = FormatValueForDisplay(kvp.Key);
                var valueStr = FormatValueForDisplay(kvp.Value);
                return $"{keyStr}: {valueStr}";
            });
            return "{" + string.Join(", ", pairs) + "}";
        }

        // 对于数组类型，格式化为 [item1, item2, item3]
        if (instance is object?[] array)
        {
            if (array.Length == 0)
            {
                return "[]";
            }

            var items = array.Select(FormatValueForDisplay);
            return "[" + string.Join(", ", items) + "]";
        }

        // 对于列表类型，格式化为 [item1, item2, item3]
        if (instance is List<object?> list)
        {
            if (list.Count == 0)
            {
                return "[]";
            }

            var items = list.Select(FormatValueForDisplay);
            return "[" + string.Join(", ", items) + "]";
        }

        // 对于元组类型，格式化为 (item1, item2)
        if (instance.GetType().IsGenericType &&
            instance.GetType().GetGenericTypeDefinition() == typeof(Tuple<,>))
        {
            var item1 = instance.GetType().GetProperty("Item1")?.GetValue(instance);
            var item2 = instance.GetType().GetProperty("Item2")?.GetValue(instance);
            var item1Str = FormatValueForDisplay(item1);
            var item2Str = FormatValueForDisplay(item2);
            return $"({item1Str}, {item2Str})";
        }

        // 对于 Task 类型（System.Threading.Tasks.Task）
        if (instance is System.Threading.Tasks.Task task)
        {
            return task.Status switch
            {
                TaskStatus.Created => "Task(Status: Created)",
                TaskStatus.WaitingForActivation => "Task(Status: WaitingForActivation)",
                TaskStatus.WaitingToRun => "Task(Status: WaitingToRun)",
                TaskStatus.Running => "Task(Status: Running)",
                TaskStatus.RanToCompletion => "Task(Status: Completed)",
                TaskStatus.Canceled => "Task(Status: Canceled)",
                TaskStatus.Faulted => $"Task(Status: Faulted, Exception: {task.Exception?.GetBaseException().Message ?? "Unknown"})",
                _ => "Task(Status: Unknown)"
            };
        }

        // 对于 Thread 类型（System.Threading.Thread）
        if (instance is System.Threading.Thread thread)
        {
            var status = thread.IsAlive ? "Alive" : "Stopped";
            return $"Thread(Id: {thread.ManagedThreadId}, Status: {status})";
        }

        // 对于 CancellationToken 类型
        if (instance is CancellationToken token)
        {
            return token.IsCancellationRequested
                ? "CancellationToken(Canceled)"
                : "CancellationToken(Active)";
        }

        // 对于 Enum 类型
        if (instance.GetType().IsEnum)
        {
            return instance.ToString() ?? "Enum";
        }

        // 对于 LangValueType，使用 ToDisplayString
        if (instance is LangValueType langValue)
        {
            return langValue.ToDisplayString();
        }

        // 对于其他类型，使用默认的 ToString
        return instance.ToString() ?? "null";
    }

    /// <summary>
    /// 格式化值用于显示（递归处理嵌套集合）
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

        // int 直接转换
        if (value is int i)
        {
            return i.ToString();
        }

        // long 直接转换
        if (value is long l)
        {
            return l.ToString();
        }

        // char 直接转换
        if (value is char c)
        {
            return c.ToString();
        }

        // double 格式化
        if (value is double d)
        {
            if (Math.Abs(d - Math.Round(d)) < 0.0000001)
            {
                return d.ToString("F0");
            }
            return d.ToString(CultureInfo.InvariantCulture);
        }

        // 递归处理嵌套字典
        if (value is Dictionary<object, object?> dict)
        {
            if (dict.Count == 0)
            {
                return "{}";
            }

            var pairs = dict.Select(kvp =>
            {
                var keyStr = FormatValueForDisplay(kvp.Key);
                var valueStr = FormatValueForDisplay(kvp.Value);
                return $"{keyStr}: {valueStr}";
            });
            return "{" + string.Join(", ", pairs) + "}";
        }

        // 递归处理嵌套数组
        if (value is object?[] array)
        {
            if (array.Length == 0)
            {
                return "[]";
            }

            var items = array.Select(FormatValueForDisplay);
            return "[" + string.Join(", ", items) + "]";
        }

        // 递归处理嵌套列表
        if (value is List<object?> list)
        {
            if (list.Count == 0)
            {
                return "[]";
            }

            var items = list.Select(FormatValueForDisplay);
            return "[" + string.Join(", ", items) + "]";
        }

        // 递归处理元组
        if (value.GetType().IsGenericType &&
            value.GetType().GetGenericTypeDefinition() == typeof(Tuple<,>))
        {
            var item1 = value.GetType().GetProperty("Item1")?.GetValue(value);
            var item2 = value.GetType().GetProperty("Item2")?.GetValue(value);
            var item1Str = FormatValueForDisplay(item1);
            var item2Str = FormatValueForDisplay(item2);
            return $"({item1Str}, {item2Str})";
        }

        // LangValueType 使用 ToDisplayString
        if (value is LangValueType langValue)
        {
            return langValue.ToDisplayString();
        }

        return value.ToString() ?? "null";
    }
}
