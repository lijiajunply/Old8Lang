namespace Old8Lang.CslyMake.OldExpression;

public class OldList : OldValue
{
    new List<OldValue> Value { get; set; }
    public OldID ID { get; set; }
    public OldList(OldID id)
    {
        ID = id;
    }

    public OldValue Add(OldValue value)
    {
        Value.Add(value);
        return value;
    }

    public OldValue Remove(OldInt num)
    {
        var a = Value[(int)num.Value];
        Value.RemoveAt((int)num.Value);
        return a;
    }
}