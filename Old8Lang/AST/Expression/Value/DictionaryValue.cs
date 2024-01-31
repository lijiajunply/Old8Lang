using System.Text;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class DictionaryValue(List<TupleValue> tuples) : ValueType
{
    private List<(ValueType Key, ValueType Value)> Value { get; set; } = [];
    private List<TupleValue> Tuples { get; set; } = tuples;

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

    public void Update(ValueType key, ValueType valueType)
    {
        Get(key);
        var b = Value.FindLastIndex(x => key.Equal(x.Key));
        Value[b] = (key, valueType);
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