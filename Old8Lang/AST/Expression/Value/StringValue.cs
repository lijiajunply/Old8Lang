using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class StringValue(string context, SourcePosition position = default) : ValueType(position), IOldList
{
    public readonly string Value = context.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\r", "\r")
        .Replace(@"\\", "\\");

    public override string ToString() => $"\"{Value}\""; // 带引号的字符串，符合 Old8Lang 语法
    public override string ToDisplayString() => Value; // 不带引号的字符串，用于显示和打印
    public override ValueType Plus(ValueType otherValueType) => new StringValue(Value + otherValueType.ToDisplayString());

    public override bool Equal(ValueType? otherValueType)
    {
        if (otherValueType is StringValue b)
            return Value == b.Value;
        return false;
    }

    public override ValueType Times(ValueType otherValueType)
    {
        if (otherValueType is IntValue value)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < value.Value; i++)
                sb.Append(Value);
            return new StringValue(sb.ToString());
        }

        throw new InvalidOperationError(this, $"不支持字符串与类型 '{otherValueType.GetType().Name}' 的乘法操作");
    }

    public override ValueType Converse(ValueType otherValueType, VariateManager manager)
    {
        if (otherValueType is not TypeValue value) throw new TypeError(this, "TypeValue", otherValueType.GetType().Name);

        switch (value.Value)
        {
            case "Int" or "int":
                return new IntValue(Value.Length);
            case "Bool" or "bool":
                throw new FormatError(this, "无法将字符串转换为布尔值，字符串不是有效的布尔格式（true/false）");
            case "String" or "string":
                return this;
            case "char" or "Char":
                return Value.Length == 0 ? new CharValue('\0') : new CharValue(Value[0]);
            case "Double" or "double":
                try
                {
                    return new DoubleValue(double.Parse(Value));
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
    public IEnumerable<ValueType> GetItems() => Value.Select(item => ObjToValue(item));

    public int GetLength() => Value.Length;

    public ValueType Slice(int start, int end)
    {
        if (start < 0) start += Value.Length;
        if (end < 0) end += Value.Length + 1;
        return new StringValue(Value[start..end]);
    }

    public Type GetChildType() => typeof(char);

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        ilGenerator.Emit(OpCodes.Ldstr, Value);
    }

    public override Type OutputType(LocalManager local) => Value.GetType();
}