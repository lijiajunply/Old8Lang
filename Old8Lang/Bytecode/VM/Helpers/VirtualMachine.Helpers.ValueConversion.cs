using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

/// <summary>
/// VirtualMachine - 值转换
/// </summary>
public partial class VirtualMachine
{
    /// <summary>
    /// 把求值栈上的原生值无损地包成 <see cref="LangValueType"/>（异步任务结果、生成器 yield 值共用）。
    /// </summary>
    /// <remarks>
    /// 原先这里用的是只认基本类型的转换，集合、类实例、函数值等会静默变成 <c>VoidLangValue</c>，
    /// 表现为 <c>async func f() -&gt; list { return await g() }</c> 报「期望 list，但得到 VoidLangValue」、
    /// <c>yield {1, 2}</c> 之后迭代到空值。null 仍表示「没有值」。
    /// </remarks>
    private LangValueType ConvertResultToLangValue(object? value)
    {
        return value is null ? VoidLangValue.Instance : ToStaticClassArgument(value);
    }

    /// <summary>
    /// 把 <see cref="ConvertResultToLangValue"/> 包出来的值还原成虚拟机栈上的原生表示（逆操作）。
    /// </summary>
    /// <remarks>
    /// 只还原虚拟机以原生形式存储的类型；<c>TaskLangValue</c> / <c>VMThreadLangValue</c> /
    /// 类型值 / 函数值等本身就是栈上的表示，原样保留。
    /// </remarks>
    internal static object? UnwrapToVmValue(object? value)
    {
        return value switch
        {
            null => null,
            VoidLangValue => VoidLangValue.Instance,
            NullLangValue => null,
            NativeAnyLangValue native => native.GetNativeObject(),
            IntLangValue intValue => intValue.Value,
            DoubleLangValue doubleValue => doubleValue.Value,
            BoolLangValue boolValue => boolValue.Value,
            CharLangValue charValue => charValue.Value,
            StringLangValue stringValue => stringValue.Value,
            ListLangValue list => list.Values.Select(UnwrapToVmValue).ToList(),
            ArrayLangValue array => array.RunResult.Select(UnwrapToVmValue).ToArray(),
            DictionaryLangValue dict => UnwrapDictionary(dict),
            TupleLangValue tuple => BuildVmTuple(tuple.ItemValues),
            _ => value
        };
    }

    private static Dictionary<object, object?> UnwrapDictionary(DictionaryLangValue dict)
    {
        var result = new Dictionary<object, object?>();
        foreach (var (key, value) in dict.Value)
        {
            // 字典键不能为 null，与解释器 ValueToObj 的兜底一致
            result[UnwrapToVmValue(key) ?? new object()] = UnwrapToVmValue(value);
        }

        return result;
    }

    /// <summary>
    /// 把元素列表编成虚拟机的嵌套元组表示（与 <c>OpCode.NewTuple</c> 的编码一致）
    /// </summary>
    private static object BuildVmTuple(List<LangValueType> items)
    {
        var elements = items.Select(UnwrapToVmValue).ToArray();
        return elements.Length switch
        {
            0 => new Tuple<object?, object?>(null, null),
            1 => new Tuple<object?, object?>(elements[0], null),
            2 => new Tuple<object?, object?>(elements[0], elements[1]),
            _ => BuildVmTupleChain(elements)
        };
    }

    private static object BuildVmTupleChain(object?[] elements)
    {
        object current = new Tuple<object?, object?>(elements[^2], elements[^1]);
        for (var i = elements.Length - 3; i >= 0; i--)
        {
            current = new Tuple<object?, object?>(elements[i], current);
        }

        return current;
    }

    /// <summary>
    /// 展平元组为列表（用于 match 表达式的元组解构）
    /// 例如：((1, 2), 3) -> [1, 2, 3]
    /// </summary>

    private List<object?> FlattenTupleHelper(TupleLangValue tuple)
    {
        return tuple.GetItems().Cast<object?>().ToList();
    }

    /// <summary>
    /// 尝试调用 BytecodeObjectInstance 的运算符重载方法
    /// </summary>

    private object? TryCallOperatorMethod(BytecodeObjectInstance obj, string methodName, object? operand)
    {
        // 查找类的元数据
        var classMetadata = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == obj.ClassName);
        if (classMetadata == null)
            return null;

        // 查找方法
        var method = classMetadata.Methods.FirstOrDefault(m => m.Name == methodName);
        if (method == null)
            return null;

        // 调用方法（第一个参数是 this，第二个参数是操作数）
        var args = new object?[] { obj, operand };
        return ExecuteFunctionAndGetResult(method.Function, args);
    }

    /// <summary>
    /// 将虚拟机栈上的值转换为 LangValueType
    /// </summary>

    private LangValueType ConvertToLangValueType(object? value)
    {
        return value switch
        {
            null => new NullLangValue(),
            int i => new IntLangValue(i),
            long l => l is >= int.MinValue and <= int.MaxValue
                ? new IntLangValue((int)l)
                : new DoubleLangValue(l),
            short s => new IntLangValue(s),
            byte b => new IntLangValue(b),
            sbyte sb => new IntLangValue(sb),
            ushort us => new IntLangValue(us),
            uint ui => ui <= int.MaxValue
                ? new IntLangValue((int)ui)
                : new DoubleLangValue(ui),
            ulong ul => ul <= int.MaxValue
                ? new IntLangValue((int)ul)
                : new DoubleLangValue(ul),
            float f => new DoubleLangValue(f),
            double d => new DoubleLangValue(d),
            string s => new StringLangValue(s),
            bool b => new BoolLangValue(b),
            char c => new CharLangValue(c),
            LangValueType lvt => lvt,
            List<object?> list => ConvertListToLangValueType(list),
            object[] array => ConvertArrayToLangValueType(array),
            Dictionary<object, object?> dict => ConvertDictToLangValueType(dict),
            _ => throw new CastError(new SourcePosition(), value.GetType().Name, "LangValueType")
        };
    }

    /// <summary>
    /// 将 List&lt;object?&gt; 转换为 ListLangValue
    /// </summary>
    private ListLangValue ConvertListToLangValueType(List<object?> list)
    {
        var items = new List<LangValueType>();
        foreach (var item in list)
        {
            items.Add(ConvertToLangValueType(item));
        }
        return new ListLangValue(items);
    }

    /// <summary>
    /// 将 object[] 转换为 ArrayLangValue
    /// </summary>
    private ArrayLangValue ConvertArrayToLangValueType(object[] array)
    {
        var items = new List<LangValueType>();
        foreach (var item in array)
        {
            items.Add(ConvertToLangValueType(item));
        }
        return new ArrayLangValue(items);
    }

    /// <summary>
    /// 将 Dictionary&lt;object, object?&gt; 转换为 DictionaryLangValue
    /// </summary>
    private DictionaryLangValue ConvertDictToLangValueType(Dictionary<object, object?> dict)
    {
        var result = new DictionaryLangValue();
        foreach (var (key, value) in dict)
        {
            var keyLangValue = ConvertToLangValueType(key);
            var valueLangValue = ConvertToLangValueType(value);
            result.Value.Add((keyLangValue, valueLangValue));
        }
        return result;
    }
}
