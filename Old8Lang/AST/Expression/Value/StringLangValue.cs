using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 字符串
/// </summary>
/// <param name="context"></param>
/// <param name="position"></param>
public class StringLangValue(string context, SourcePosition position = default) : LangValueType(position), ILangList
{
    public string Value = context.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\r", "\r")
        .Replace(@"\\", "\\");
    
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override string ToString() => $"\"{Value}\""; // 带引号的字符串，符合 Old8Lang 语法
    public override string ToDisplayString() => Value; // 不带引号的字符串，用于显示和打印

    public override LangValueType Plus(LangValueType otherLangValueType) =>
        new StringLangValue(Value + otherLangValueType.ToDisplayString());

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
            return new StringLangValue(sb.ToString());
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
            return new StringLangValue(Value.Replace(b.Value, ""));

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
                    return new IntLangValue(int.Parse(Value));
                }
                catch (FormatException)
                {
                    throw new FormatError(this, $"无法将字符串 '{Value}' 转换为整数，字符串不是有效的数字格式");
                }
            case "Bool" or "bool":
                if (Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                    return new BoolLangValue(true);
                if (Value.Equals("false", StringComparison.OrdinalIgnoreCase))
                    return new BoolLangValue(false);
                throw new FormatError(this, "无法将字符串转换为布尔值，字符串不是有效的布尔格式（true/false）");
            case "String" or "string":
                return this;
            case "char" or "Char":
                return Value.Length == 0 ? new CharLangValue('\0') : new CharLangValue(Value[0]);
            case "Double" or "double":
                try
                {
                    return new DoubleLangValue(double.Parse(Value));
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
        return new CharLangValue(Value[i]);
    }

    public LangValueType Slice(int start, int end)
    {
        if (start < 0) start += Value.Length;
        if (end < 0) end += Value.Length + 1;
        return new StringLangValue(Value[start..end]);
    }

    public Type GetChildType() => typeof(char);

    public void Set(LangValueType index, LangValueType value)
    {
        throw new InvalidOperationError(this, "字符串索引修改不可使用");
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        ilGenerator.Emit(OpCodes.Ldstr, Value);
    }

    public override Type OutputType(LocalManager local) => Value.GetType();
}