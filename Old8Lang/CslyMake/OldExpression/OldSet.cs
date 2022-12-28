namespace Old8Lang.CslyMake.OldExpression;

public class OldSet : OldStatement
{
    public OldID Id { get; set; }
    public OldValue Value { get; set; }

    public OldSet(OldID id, OldValue value)
    {
        Id = id;
        Value = value;
    }
}