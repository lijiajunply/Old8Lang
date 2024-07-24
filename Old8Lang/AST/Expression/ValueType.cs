using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 表示值
/// </summary>
public abstract class ValueType : OldExpr
{

    public override string ToString() => GetValue().ToString()!;

    #region intOper

    public virtual ValueType Plus(ValueType otherValueType) => new VoidValue();
    public virtual ValueType Minus(ValueType otherValueType) => new VoidValue();
    public virtual ValueType Times(ValueType otherValueType) => new VoidValue();
    public virtual ValueType Divide(ValueType otherValueType) => new VoidValue();

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
            return instance.FromClassToResult(this,"Old8Lang.AST.Expression.ValueType");
        }

        return new VoidValue();
    }

    #region boolOper

    public virtual bool Equal(ValueType? otherValueType) => false;
    public virtual bool Less(ValueType? otherValue) => false;
    public virtual bool Greater(ValueType? otherValue) => false;

    #endregion

    public override ValueType Run(ref VariateManager Manager) => this;

    public string TypeToString()
    {
        return this switch
        {
            AnyValue a => a.Id.IdName,
            ArrayValue => "Array",
            BoolValue => "Bool",
            CharValue => "Char",
            DictionaryValue => "Dictionary",
            DoubleValue => "Double",
            FuncValue func => $"Function {func.Id}({Apis.ListToString(func.Ids)})",
            Instance instance => $"Instance {instance}",
            IntValue => "Int",
            OldItem item => $"Item {item}",
            ListValue => "List",
            StringValue => "String",
            TypeValue => "Type",
            TupleValue => "Tuple",
            _ => "Value"
        };
    }

    public virtual object GetValue() => new();

    protected static ValueType ObjToValue(object? value)
    {
        if (value == null)
        {
            return new VoidValue();
        }

        return value switch
        {
            int a => new IntValue(a),
            string a => new StringValue(a),
            double a => new DoubleValue(a),
            char a => new CharValue(a),
            List<object> a => new ListValue(a),
            object[] a => new ArrayValue(a.ToList()),
            _ => new VoidValue()
        };
    }
}

public static class ValueTypeFuncStatic
{
    public static IntValue ToInt(this ValueType type)
    {
        if (type is IntValue intValue)
        {
            return intValue;
        }

        if (type is DoubleValue doubleValue)
        {
            return new IntValue(Convert.ToInt32(doubleValue.Value));
        }

        if (type is CharValue charValue)
        {
            return new IntValue(Convert.ToInt32(charValue.Value));
        }
        
        return new IntValue(int.Parse(type.ToString()));
    }

    public static TypeValue GetType(this ValueType type)
    {
        return new TypeValue(type.TypeToString());
    }

    public static StringValue ToStr(this ValueType type)
    {
        return new StringValue(type.ToString());
    }
}