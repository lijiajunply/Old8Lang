using System.Text;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class DictionaryValue(List<TupleValue> tuples) : ValueType
{
    public List<(ValueType Key, ValueType Value)> Value { get; } = [];
    private List<TupleValue> Tuples { get; } = tuples;

    public override ValueType Run(ref VariateManager Manager)
    {
        foreach (var tuple in Tuples)
        {
            tuple.Run(ref Manager);
            Value.Add(tuple.Value);
        }

        return this;
    }

    public override ValueType Dot(OldExpr dotExpr)
    {
        return dotExpr is not Instance a ? new VoidValue() : a.FromClassToResult(this);
    }

    public ValueType Get(ValueType key)
    {
        var a = Value.Where(x => x.Key.Equal(key)).ToList();
        return a[0].Value;
    }

    public void Update(ValueType key, ValueType valueType)
    {
        Get(key);
        var b = Value.FindLastIndex(x => key.Equal(x.Key));
        Value[b] = (key, valueType);
    }

    public override string ToString()
    {
        if (Value.Count == 0) return "{" + Apis.ListToString(Tuples) + "}";
        var sb = new StringBuilder();
        foreach (var valueTuple in Value)
            sb.Append($"key:{valueTuple.Key},value:{valueTuple.Value}");

        return "{" + sb + "}";
    }
    
    public override ValueType Converse(ValueType otherValueType,ref VariateManager Manager)
    {
        if (otherValueType is not AnyValue typeAny) return new VoidValue();
        
        foreach (var a in Value)
        {
            var aKey = a.Key.Run(ref Manager);
            var aValue = a.Value.Run(ref Manager);
            if (aKey is not StringValue s) continue;
            typeAny.Set(new OldID(s.Value), aValue);
        }

        return typeAny;
    }
}

public static class DictionaryValueFuncStatic
{
    public static TupleValue Add(this DictionaryValue value,ValueType value1, ValueType value2)
    {
        value.Value.Add((value1, value2));
        return new TupleValue(value1, value2);
    }
}