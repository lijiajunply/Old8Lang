using System.Text;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class OldDictionary : OldValue
{
    public new List<(OldValue Key,OldValue Value)> Value  { get; set; }
    public     List<OldTuple>                      Tuples { get; set; }

    public OldID Id { get; set; }

    public OldDictionary(List<OldTuple> tuples)
    {
        Value  = new List<(OldValue Key,OldValue Value)>();
        Tuples = tuples;
    }
    public override OldValue Run(ref VariateManager Manager)
    {
        foreach (var tuple in Tuples)
        {
            tuple.Run(ref Manager);
            Value.Add(tuple.Value);
        }
        return this;
    }

    public (OldValue,OldValue) Add(OldValue value1,OldValue value2)
    {
        Value.Add((value1,value2));
        return (value1,value2);
    }

    public OldValue Get(OldValue key)
    {
        var a = Value.Where(x => key.Equal(x.Key)).ToList();
        return a[0].Value;
    }
    public OldValue Post(OldValue key,OldValue value)
    {
        var a = Get(key);
        var b = Value.FindLastIndex(x => key.Equal(x.Key));
        Value[b] = (key,value);
        return a;
    }

    public override string ToString()
    {
        if (Value.Count != 0)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var valueTuple in Value)
                sb.Append($"key:{valueTuple.Key},value:{valueTuple.Value}");

            return "{"+sb+"}";
        }
        return "{"+OldLangTree.ListToString(Tuples)+"}";
    }
}