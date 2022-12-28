using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldCompare : OldLangTree
{
    public string Location { get; set; }
    public OldTokenGeneric Compare { get; set; }
    public bool Value { get; set; }
    public OldCompare(OldTokenGeneric compare) => Compare = compare;
}