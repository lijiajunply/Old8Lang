using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;
/// <summary>
/// expr ::= id compare value 
/// </summary>
public class OldExpr : OldLangTree
{
    public string Location { get; set; }
    public OldCompare Compare { get; set; }
    public OldID ID { get; set; }
    public OldValue Value { get; set; }
    public bool BoolValue { get; set; }

    public OldExpr(OldID? id, OldCompare? compare, OldValue value)
    {
        ID = id;
        Compare = compare;
        Value = value;
        if (id == null && compare == null && value != null)
        {
            var oldboolvalue = value as OldBool;
            BoolValue = oldboolvalue.BoolValue;
        }
    }
}