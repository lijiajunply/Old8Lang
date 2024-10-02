using System.Text;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class StringValue : ValueType, IOldList
{
    public string Value { get; }

    public StringValue(string context)
    {
        Value = "";
        var a = context.Split("\\n");
        for (var i = 0; i < a.Length; i++)
            Value += a[i] + (i == a.Length - 1 ? "" : "\n");
    }

    public override string ToString() => Value;
    public override ValueType Plus(ValueType otherValueType) => new StringValue(Value + otherValueType);

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

        return new VoidValue();
    }

    public override ValueType Converse(ValueType otherValueType, ref VariateManager _)
    {
        if (otherValueType is not TypeValue value) throw new Exception("the value is not a type");

        return value.Value switch
        {
            "Int" or "int" => new IntValue(Value.Length),
            "Bool" or "bool" => throw new Exception("can not convert string to bool"),
            "String" or "string" => this,
            "char" or "Char" => Value.Length == 0 ? new CharValue('\0') : new CharValue(Value[0]),
            "Double" or "double" => throw new Exception("can not convert string to double"),
            _ => throw new Exception("not fount the type: " + value.Value)
        };
    }

    public override object GetValue() => Value;
    public IEnumerable<ValueType> GetItems() => Value.Select(item => ObjToValue(item));

    public int GetLength() => Value.Length;
}