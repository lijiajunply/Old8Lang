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

    public virtual OldValue Dot(OldExpr dotExpr)
    {
        if (dotExpr is OldID)
        {
            var a = dotExpr as OldID;
            if (a.IdName == "toint")
            {
                return new OldInt(Int32.Parse(ToString()));
            }
            if (a.IdName == "tostring")
            {
                return new OldString(ToString());
            }

            if (a.IdName == "tochar")
            {
                return new OldChar(ToString()[0]);
            }
        }
        return null;
    }
    public virtual bool EQUAL(OldValue otherValue) => (this.Value == otherValue.Value);
    public virtual bool LESS(OldValue otherValue) => false;
    public virtual bool GREATER(OldValue otherValue) => false;
    public override OldValue Run(ref VariateManager Manager) => this;

    public virtual OldValue Clone()
    {
        var a = (OldValue)MemberwiseClone();
        a.Init();
        return a;
    }
    public virtual void Init()
    {
        Value = new object();
    }
}