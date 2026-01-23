using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 字符串
/// </summary>
/// <param name="context"></param>
/// <param name="position"></param>
public partial class StringLangValue(string context = "", SourcePosition position = default)
    : LangValueType(position), ILangList, IPoolable
{
    public string Value { get; private set; } = context.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\r", "\r")
        .Replace(@"\\", "\\");


    public override string ToString() => $"\"{Value}\""; // 带引号的字符串，符合 Old8Lang 语法
    public override string ToDisplayString() => Value; // 不带引号的字符串，用于显示和打印

    public override LangValueType Plus(LangValueType otherLangValueType) =>
        Create(Value + otherLangValueType.ToDisplayString());

    public override bool Equal(LangValueType? otherValueType)
    {
        if (otherValueType is StringLangValue b)
            return Value == b.Value;
        return false;
    }

    public override LangValueType Times(LangValueType otherLangValueType)
    {
        if (otherLangValueType is IntLangValue value)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < value.Value; i++)
                sb.Append(Value);
            return Create(sb.ToString());
        }

        throw new InvalidOperationError(this, $"不支持字符串与类型 '{otherLangValueType.GetType().Name}' 的乘法操作");
    }

    public override bool Less(LangValueType? otherValue)
    {
        if (otherValue is StringLangValue b)
            return Value.Length < b.Value.Length;

        throw new InvalidOperationError(this, $"不支持字符串与类型 '{otherValue?.GetType().Name}' 的比较操作");
    }

    public override bool LessEqual(LangValueType? otherValue)
    {
        if (otherValue is StringLangValue b)
            return Value.Length <= b.Value.Length;

        throw new InvalidOperationError(this, $"不支持字符串与类型 '{otherValue?.GetType().Name}' 的比较操作");
    }

    public override bool Greater(LangValueType? otherValue)
    {
        if (otherValue is StringLangValue b)
            return Value.Length > b.Value.Length;

        throw new InvalidOperationError(this, $"不支持字符串与类型 '{otherValue?.GetType().Name}' 的比较操作");
    }

    public override bool GreaterEqual(LangValueType? otherValue)
    {
        if (otherValue is StringLangValue b)
            return Value.Length >= b.Value.Length;

        throw new InvalidOperationError(this, $"不支持字符串与类型 '{otherValue?.GetType().Name}' 的比较操作");
    }

    public override LangValueType Minus(LangValueType otherLangValueType)
    {
        if (otherLangValueType is StringLangValue b)
            return Create(Value.Replace(b.Value, ""));

        throw new InvalidOperationError(this, $"不支持字符串与类型 '{otherLangValueType.GetType().Name}' 的减法操作");
    }

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
    {
        if (otherLangValueType is not TypeLangValue value)
            throw new TypeError(this, "TypeValue", otherLangValueType.GetType().Name);

        switch (value.Value)
        {
            case "Int" or "int":
                try
                {
                    return IntLangValue.Create(int.Parse(Value));
                }
                catch (FormatException)
                {
                    throw new FormatError(this, $"无法将字符串 '{Value}' 转换为整数，字符串不是有效的数字格式");
                }
            case "Bool" or "bool":
                if (Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                    return BoolLangValue.Create(true);
                if (Value.Equals("false", StringComparison.OrdinalIgnoreCase))
                    return BoolLangValue.Create(false);
                throw new FormatError(this, "无法将字符串转换为布尔值，字符串不是有效的布尔格式（true/false）");
            case "String" or "string":
                return this;
            case "char" or "Char":
                return Value.Length == 0 ? CharLangValue.Create('\0') : CharLangValue.Create(Value[0]);
            case "Double" or "double":
                try
                {
                    return DoubleLangValue.Create(double.Parse(Value));
                }
                catch (FormatException)
                {
                    throw new FormatError(this, $"无法将字符串 '{Value}' 转换为浮点数，字符串不是有效的数字格式");
                }
            default:
                throw new TypeError(this, $"不支持的类型转换: {GetType().Name} 到 {value.Value}");
        }
    }

    public override object GetValue() => Value;
    public IEnumerable<LangValueType> GetItems() => Value.Select(item => ObjToValue(item));

    public int GetLength() => Value.Length;

    public LangValueType Get(IntLangValue index)
    {
        var i = index.Value;
        if (i < 0)
            i = Value.Length + i;
        if (i < 0 || i >= Value.Length)
            throw new IndexError(this, i, Value.Length);
        return CharLangValue.Create(Value[i]);
    }

    public LangValueType Slice(int start, int end, int step)
    {
        var length = Value.Length;
        var result = new StringBuilder();

        if (step > 0)
        {
            // 正向切片
            // 处理负数索引
            if (start < 0) start += length;
            if (end < 0) end += length;

            start = Math.Max(0, Math.Min(start, length));
            end = Math.Max(0, Math.Min(end, length));

            for (int i = start; i < end; i += step)
            {
                result.Append(Value[i]);
            }
        }
        else if (step < 0)
        {
            // 反向切片
            // 处理负数索引，但保留 -1 作为特殊值（表示"到开头之前"）
            if (start < -1) start += length;
            if (end < -1) end += length;

            // 设置边界
            if (start >= length) start = length - 1;
            if (start < -1) start = -1;
            if (end >= length) end = length - 1;
            // end 可以是 -1，表示切片到开头之前（即包含索引 0）

            for (int i = start; i > end; i += step)
            {
                result.Append(Value[i]);
            }
        }
        else
        {
            throw new InvalidOperationError(this, "切片步长不能为0");
        }

        return Create(result.ToString());
    }

    public void Set(LangValueType index, LangValueType value)
    {
        throw new InvalidOperationError(this, "字符串索引修改不可使用");
    }

    public void SetSlice(int start, int end, IEnumerable<LangValueType> values)
    {
        throw new InvalidOperationError(this, "字符串是不可变类型，不支持切片赋值操作");
    }

    public bool In(LangValueType value)
    {
        return Value.Contains(value.GetValue<char>());
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        ilGenerator.Emit(OpCodes.Ldstr, Value);
    }

    public override Type OutputType(LocalManager local) => Value.GetType();

    /// <summary>
    /// 重置对象状态，使其可以被复用
    /// </summary>
    public void Reset()
    {
        Value = string.Empty;
        // Position是只读属性，无法修改
    }

    /// <summary>
    /// 从对象池获取StringLangValue实例
    /// </summary>
    /// <param name="value">字符串值</param>
    /// <param name="position">源码位置</param>
    /// <returns>StringLangValue实例</returns>
    public static StringLangValue Create(string value, SourcePosition position = default)
    {
        var instance = ObjectPoolManager.Instance.StringPool.Get();
        instance.Value = value.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\r", "\r")
            .Replace(@"\\", "\\");
        instance.Position = position;
        return instance;
    }

    /// <summary>
    /// 将实例归还到对象池
    /// </summary>
    public void ReturnToPool()
    {
        ObjectPoolManager.Instance.StringPool.Return(this);
    }
}