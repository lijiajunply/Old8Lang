using System.Text;
using Old8Lang.AST.Expression.Value;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Old8Lang.AST.Expression;

public static class AnyValueFuncStatic
{
    public static StringValue ToJson(this AnyValue type)
    {
        var builder = new StringBuilder();
        builder.Append('{');
        for (var i = 0; i < type.Variates.Count; i++)
        {
            var variable = type.Variates.ElementAt(i);
            if (variable.Value is FuncValue or Instance or NativeAnyValue or NativeStaticAny or VoidValue) continue;
            builder.Append($"{(i == 0 ? "" : ",")}\"{variable.Key}\":{variable.Value}");
        }

        builder.Append('}');
        return new StringValue(builder.ToString());
    }

    public static AnyValue ToObj(this StringValue json)
    {
        var jsonObject = JsonSerializer.Deserialize<Dictionary<string, object>>(json.Value) ??
                         new Dictionary<string, object>();
        
        return new AnyValue(jsonObject.ToDictionary<KeyValuePair<string, object>, OldID, OldExpr>
            (
                variable => new OldID(variable.Key),
                variable => ValueType.ObjToValue(variable.Value)
            )
        );
    }
}

public static class ValueTypeFuncStatic
{
    public static IntValue ToInt(this ValueType type)
    {
        if (type is IntValue intValue)
        {
            return intValue;
        }

        if (type is DoubleValue doubleValue)
        {
            return new IntValue(Convert.ToInt32(doubleValue.Value));
        }

        if (type is CharValue charValue)
        {
            return new IntValue(Convert.ToInt32(charValue.Value));
        }

        return new IntValue(int.Parse(type.ToString()));
    }

    public static TypeValue ToType(this ValueType type)
    {
        return new TypeValue(type.TypeToString());
    }
    
    public static StringValue ToStr(this ValueType type)
    {
        return new StringValue(type.ToString());
    }
}

public static class DictionaryValueFuncStatic
{
    public static TupleValue Add(this DictionaryValue value, ValueType value1, ValueType value2)
    {
        value.Value.Add((value1, value2));
        return new TupleValue(value1, value2);
    }
}

public static class ListValueFuncStatic
{
    public static ValueType Add(this ListValue value, ValueType valueType)
    {
        value.Values.Add(valueType);
        return valueType;
    }

    private static ValueType Remove(this ListValue value, IntValue num)
    {
        var a = value.Values[num.Value];
        value.Values.RemoveAt(num.Value);
        return a;
    }

    private static VoidValue AddList(this ListValue value, ListValue otherValue)
    {
        value.Values.AddRange(otherValue.Values);
        return new VoidValue();
    }

    private static ListValue Sort(this ListValue value)
    {
        value.Values.Sort();
        return value;
    }
}