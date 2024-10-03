using System.Text;
using System.Text.Json;
using Old8Lang.AST.Expression.Value;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Old8Lang.AST.Expression;

public static class AnyValueFuncStatic
{
    private static ValueType GetJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => new StringValue(element.GetString() ?? ""),
            JsonValueKind.Number => new IntValue(element.GetInt32()),
            JsonValueKind.True => new BoolValue(true),
            JsonValueKind.False => new BoolValue(false),
            JsonValueKind.Null => new VoidValue(),
            JsonValueKind.Array => new ArrayValue(element.EnumerateArray().Select(GetJsonElement).ToList()),
            JsonValueKind.Undefined => new VoidValue(),
            JsonValueKind.Object => ToObj(new StringValue(element.ToString())),
            _ => throw new Exception()
        };
    }

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
            variable =>
            {
                if (variable.Value is JsonElement element)
                {
                    return GetJsonElement(element);
                }

                return ValueType.ObjToValue(variable.Value);
            }));
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

    public static ValueType GetValue(this DictionaryValue value, ValueType key)
    {
        return value.Value.First(x => x.Key.Equal(key)).Value;
    }

    public static ValueType Remove(this DictionaryValue value, ValueType key)
    {
        for (var i = 0; i < value.Value.Count; i++)
        {
            if (!value.Value[i].Key.Equal(key)) continue;
            var a = value.Value[i].Value;
            value.Value.RemoveAt(i);
            return a;
        }

        throw new Exception("not found");
    }
}

public static class ListValueFuncStatic
{
    public static ValueType Add(this ListValue value, ValueType valueType)
    {
        value.Values.Add(valueType);
        return valueType;
    }

    public static ValueType Remove(this ListValue value, ValueType num)
    {
        for (var i = 0; i < value.Values.Count; i++)
        {
            if (!value.Values[i].Equal(num)) continue;
            var a = value.Values[i];
            value.Values.RemoveAt(i);
            return a;
        }

        throw new Exception("not found");
    }

    public static ValueType RemoveAt(this ListValue value, IntValue num)
    {
        var a = value.Values[num.Value];
        value.Values.RemoveAt(num.Value);
        return a;
    }

    public static VoidValue AddList(this ListValue value, ListValue otherValue)
    {
        value.Values.AddRange(otherValue.Values);
        return new VoidValue();
    }

    public static ListValue Sort(this ListValue value)
    {
        QuickSort(value.Values, 0, value.Values.Count - 1);
        return value;
    }

    private static void QuickSort(List<ValueType> nums, int left, int right)
    {
        while (true)
        {
            if (left < right)
            {
                int pivotIndex = Partition(nums, left, right);
                QuickSort(nums, left, pivotIndex - 1);
                left = pivotIndex + 1;
                continue;
            }

            break;
        }
    }

    private static int Partition(List<ValueType> nums, int left, int right)
    {
        var pivot = nums[right];
        var i = left - 1;

        for (var j = left; j < right; j++)
        {
            if (!nums[j].Less(pivot)) continue;
            i++;
            Swap(nums, i, j);
        }

        Swap(nums, i + 1, right);
        return i + 1;
    }

    private static void Swap(List<ValueType> nums, int i, int j)
    {
        (nums[i], nums[j]) = (nums[j], nums[i]);
    }
}