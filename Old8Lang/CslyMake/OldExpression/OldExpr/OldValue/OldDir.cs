using System.Text;

namespace Old8Lang.CslyMake.OldExpression;

public class OldDir : OldValue
{
    new Dictionary<OldValue,OldValue> Value { get; set; }
    public OldID Id { get; set; }

    public OldDir(OldID id)
    {
        Id = id;
    }

    public (OldValue, OldValue) Add(OldValue value1, OldValue value2)
    {
        Value.Add(value1, value2);
        return (value1, value2);
    }

    public OldValue GetValue(OldValue value)
    {
        return Value[value];
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var VARIABLE in Value)
        {
            sb.Append($"key:{VARIABLE.Key},value:{VARIABLE.Value}");
        }

        return $"dir {Id} : {sb}";
    }
}