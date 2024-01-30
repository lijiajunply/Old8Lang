using System.Text;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class DictionaryValue : ValueType
{
    public new List<(ValueType Key, ValueType Value)> Value { get; set; }
    public List<TupleValue> Tuples { get; set; }

    public OldID Id { get; set; }

    public DictionaryValue(List<TupleValue> tuples)
    {
        Value = new List<(ValueType Key, ValueType Value)>();
        Tuples = tuples;
    }

    public override ValueType Run(ref VariateManager Manager)
    {
        foreach (var tuple in Tuples)
        {
            tuple.Run(ref Manager);
            Value.Add(tuple.Value);
        }

        return this;
    }

    public (ValueType, ValueType) Add(ValueType value1, ValueType value2)
    {
        Value.Add((value1, value2));
        return (value1, value2);
    }

    public ValueType Get(ValueType key)
    {
        var a = Value.Where(x => x.Key.Equal(key)).ToList();
        return a[0].Value;
    }

    public ValueType Post(ValueType key, ValueType valueType)
    {
        var a = Get(key);
        var b = Value.FindLastIndex(x => key.Equal(x.Key));
        Value[b] = (key, valueType);
        return a;
    }

    public override string ToString()
    {
        if (Value.Count != 0)
        {
            var sb = new StringBuilder();
            foreach (var valueTuple in Value)
                sb.Append($"key:{valueTuple.Key},value:{valueTuple.Value}");

            return "{" + sb + "}";
        }

        return "{" + Apis.ListToString(Tuples) + "}";
    }
}