using Old8Lang.AST.Expression.Value;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 表示值
/// </summary>
public class ValueType : OldExpr
{
    public object Value { get; set; }

    public override string ToString() => GetValue().ToString();

    #region intOper

    public virtual ValueType Plus(ValueType   otherValueType) => new ValueType();
    public virtual ValueType Minus(ValueType  otherValueType) => new ValueType();
    public virtual ValueType Times(ValueType  otherValueType) => new ValueType();
    public virtual ValueType Divide(ValueType otherValueType) => new ValueType();

    #endregion


    public virtual ValueType Dot(OldExpr dotExpr)
    {
        if (dotExpr is OldID id)
        {
            if (id.IdName == "XAUAT")
                return new StringValue("西建大还我血汗钱我要回家");
        }
        if (dotExpr is Instance instance)
        {
            if (instance.Id.IdName == "GetType")
                return new TypeValue(TypeToString());
            if (instance.Id.IdName == "ToInt")
                return new IntValue(Int32.Parse(ToString()));
            if (instance.Id.IdName == "ToString")
                return new StringValue(ToString());
        }
        return null;
    }

    #region boolOper

    public virtual bool Equal(ValueType    otherValueType) => false;
    public virtual bool Less(ValueType?    otherValue)     => false;
    public virtual bool Greater(ValueType? otherValue)     => false;

    #endregion

    public override ValueType Run(ref VariateManager Manager) => this;

    public virtual ValueType Clone()
    {
        var a = (ValueType)MemberwiseClone();
        a.Init();
        return a;
    }
    public virtual void Init() => Value = new object();
    public string TypeToString()
    {
        return this switch
               {
                   AnyValue a      => a.Id.IdName,
                   ArrayValue      => "Array",
                   BoolValue       => "Bool",
                   CharValue       => "Char",
                   DictionaryValue => "Dictionary",
                   DoubleValue     => "Double",
                   FuncValue func  => $"Function {func.Id} ({APIs.ListToString(func.Ids)})",
                   Instance instance   => $"Instance {instance}",
                   IntValue        => "Int",
                   OldItem item           => $"Item {item}",
                   ListValue                => "List",
                   StringValue              => "String",
                   TypeValue                => "Type",
                   TupleValue               => "Tuple",
                   _                   => "Value"
               };
    }
    public override bool   Equals(object? obj) => Equal(obj as ValueType);
    public virtual  object GetValue()          => Value;
    public static ValueType ObjToValue(object value)
    {
        return value switch
               {
                   int a          => new IntValue(a),
                   string a       => new StringValue(a),
                   double a       => new DoubleValue(a),
                   char a         => new CharValue(a),
                   List<object> a => new ListValue(a),
                   object[] a     => new ArrayValue(a.ToList()),
                   _              => new ValueType()
               };
    }
}