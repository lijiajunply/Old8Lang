using System.Runtime.CompilerServices;
using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;
/// <summary>
/// 表示值
/// </summary>
public class OldValue : OldExpr
{
    public string Location { get; set; }
    public object Value { get; set; }

    public bool Compare(OldValue otherValue,OldTokenGeneric tokenGeneric)
    {
        if (this.GetType() == otherValue.GetType())
        {
            switch (tokenGeneric)
            {
                case OldTokenGeneric.EQUALS:
                    return (this.Value == otherValue.Value);
                case OldTokenGeneric.LESSER:
                    return ((double)this.Value < (double)otherValue.Value);
                case OldTokenGeneric.GREATER:
                    return ((double)this.Value > (double)otherValue.Value);
                case OldTokenGeneric.DIFFERENT:
                    return (this.Value != otherValue.Value);
                default:
                    return false;
            }
        }
        return false;
    }
}