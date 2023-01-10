using System.Text;

namespace Old8Lang.CslyMake.OldExpression;

public class OldList : OldValue
{
    public new List<OldValue> Value { get; set; }
    public OldID ID { get; set; }
    public OldList(OldID id)
    {
        ID = id;
    }

    public OldList(OldID id, List<OldValue> values)
    {
        ID = id;
        Value = values;
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

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var VARIABLE in Value)
        {
            sb.Append(VARIABLE + " ");
        }
        return $"list {ID} : {sb} ";
    }

    public override OldValue Dot(OldID DotID)
    {
        return new OldValue();
    }

    public override bool EQUAL(OldValue otherValue) => false;
    
}