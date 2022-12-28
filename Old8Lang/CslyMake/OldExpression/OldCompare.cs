using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;
/// <summary>
/// 比较符号
/// </summary>
public class OldCompare : OldLangTree
{
    public string Location { get; set; }
    public OldTokenGeneric Compare { get; set; }
    public bool Value { get; set; }
    public OldCompare(OldTokenGeneric compare) => Compare = compare;
}