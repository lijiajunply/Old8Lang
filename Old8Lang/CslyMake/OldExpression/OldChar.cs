namespace Old8Lang.CslyMake.OldExpression;

public class OldChar : OldValue
{
    new char Value { get; set; }
    public OldChar(char value) => Value = value;
}