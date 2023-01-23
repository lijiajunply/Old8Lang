using Old8Lang.AST.Expression.Value;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression;

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

    #region intOper

    public virtual OldValue Plus(OldValue   otherValue) => new OldValue();
    public virtual OldValue Minus(OldValue  otherValue) => new OldValue();
    public virtual OldValue Times(OldValue  otherValue) => new OldValue();
    public virtual OldValue Divide(OldValue otherValue) => new OldValue();

    #endregion
    

    public virtual OldValue Dot(OldExpr dotExpr)
    {
        if (dotExpr is OldID)
        {
            var a = dotExpr as OldID;
            if (a.IdName == "toint")
                return new OldInt(Int32.Parse(ToString()));
            if (a.IdName == "tostring")
                return new OldString(ToString());
            if (a.IdName == "tochar")
                return new OldChar(ToString()[0]);
            if (a.IdName == "XAUAT_Str")
                return new OldString("西建大还我血汗钱我要回家");
        }
        return null;
    }

    #region boolOper

    public virtual bool Equal(OldValue?   otherValue) => Value == otherValue.Value;
    public virtual bool Less(OldValue?    otherValue) => false;
    public virtual bool Greater(OldValue? otherValue) => false;

    #endregion
    
    public override OldValue Run(ref VariateManager Manager)    => this;

    public virtual OldValue Clone()
    {
        var a = (OldValue)MemberwiseClone();
        a.Init();
        return a;
    }
    public virtual void Init() => Value = new object();
    public string TypeToString()
    {
        return this switch
               {
                   OldAny a      => a.Id.IdName,
                   OldArray      => "array",
                   OldBool       => "bool",
                   OldChar       => "char",
                   OldDictionary => "Dictionary",
                   OldDouble     => "Double",
                   OldFunc       => "Function",
                   OldInstance   => "Instance",
                   OldInt        => "Int",
                   OldItem       => "Item",
                   OldList       => "List",
                   OldString     => "String",
                   OldType       => "Type",
                   _             => "value"
               };
    }
}