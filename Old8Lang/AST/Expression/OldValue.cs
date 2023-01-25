using Old8Lang.AST.Expression.Value;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 表示值
/// </summary>
public class OldValue : OldExpr
{
    public object Value { get; set; }


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
            if (a.IdName == "XAUAT")
                return new OldString("西建大还我血汗钱我要回家");
        }
        return null;
    }

    #region boolOper

    public virtual bool Equal(OldValue    otherValue) => false;
    public virtual bool Less(OldValue?    otherValue) => false;
    public virtual bool Greater(OldValue? otherValue) => false;

    #endregion

    public override OldValue Run(ref VariateManager Manager) => this;

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
                   OldAny a             => a.Id.IdName,
                   OldArray             => "array",
                   OldBool              => "bool",
                   OldChar              => "char",
                   OldDictionary        => "Dictionary",
                   OldDouble            => "Double",
                   OldFunc func         => $"Function {func.Id} ({APIs.ListToString(func.Ids)})",
                   OldInstance instance => $"Instance {instance}",
                   OldInt               => "Int",
                   OldItem item         => $"Item {item}",
                   OldList              => "List",
                   OldString            => "String",
                   OldType              => "Type",
                   OldTuple             => "Tuple",
                   _                    => "Value"
               };
    }
    public override bool   Equals(object? obj) => Equal(obj as OldValue);
    public virtual  object GetValue()          => Value;
    public static OldValue ObjToValue(object value)
    {
        return value switch
               {
                   int a    => new OldInt(a),
                   string a => new OldString(a),
                   double a => new OldDouble(a),
                   char a   => new OldChar(a),
                   List<object> a => new OldList(a),
                   object[] a => new OldArray(a.ToList()),
                   _        => new OldValue()
               };
    }
}