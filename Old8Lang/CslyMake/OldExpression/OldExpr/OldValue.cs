using System.Runtime.CompilerServices;
using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;
/// <summary>
/// 表示值
/// </summary>
public class OldValue : OldExpr
{
    public object Value { get; set; }
    public override bool Equals(object? value)
    {
        var a = value as OldValue;
        return Value.ToString() == a.Value.ToString();
    }

    public virtual OldValue PLUS(OldValue otherValue) => new OldValue();

    public virtual OldValue MINUS(OldValue otherValue) => new OldValue();

    public virtual OldValue TIMES(OldValue otherValue) => new OldValue();

    public virtual OldValue DIVIDE(OldValue otherValue) => new OldValue();
    public override string ToString() => Value.ToString();

    public virtual OldValue Dot(OldID DotID)
    {
        if (DotID.IdName == "toint")
        {
            return new OldInt((int)Value);
        }
        if (DotID.IdName == "tostring")
        {
            return new OldString((string)Value);
        }

        if (DotID.IdName == "tochar")
        {
            return new OldChar(((string)Value)[0]);
        }
        return null;
    }
    public virtual bool EQUAL(OldValue otherValue) => Value == otherValue.Value;
    public virtual bool LESS(OldValue otherValue) => false;
    public virtual bool GREATER(OldValue otherValue) => false;

}