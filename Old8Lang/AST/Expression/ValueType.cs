using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.LangParser;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 表示值
/// </summary>
/// <remarks>
/// 构造函数
/// </remarks>
/// <param name="position">位置信息</param>
public abstract class ValueType(SourcePosition position = default) : OldExpr(position)
{
    public override string ToString() => GetValue().ToString()!;

    #region intOper

    public virtual ValueType Plus(ValueType otherValueType) => throw new InvalidOperationError(this, $"不支持类型 '{GetType().Name}' 和 '{otherValueType.GetType().Name}' 的加法操作");
    public virtual ValueType Minus(ValueType otherValueType) => throw new InvalidOperationError(this, $"不支持类型 '{GetType().Name}' 和 '{otherValueType.GetType().Name}' 的减法操作");
    public virtual ValueType Times(ValueType otherValueType) => throw new InvalidOperationError(this, $"不支持类型 '{GetType().Name}' 和 '{otherValueType.GetType().Name}' 的乘法操作");
    public virtual ValueType Divide(ValueType otherValueType) => throw new InvalidOperationError(this, $"不支持类型 '{GetType().Name}' 和 '{otherValueType.GetType().Name}' 的除法操作");

    #endregion

    public virtual ValueType Dot(OldExpr dotExpr)
    {
        if (dotExpr is OldId id)
        {
            if (id.IdName == "XAUAT")
                return new StringValue("西建大还我血汗钱我要回家");
            throw new AttributeError(this, id.IdName, GetType().Name);
        }

        if (dotExpr is Instance instance)
        {
            return instance.FromClassToResult(this);
        }

        throw new InvalidOperationError(this, $"不支持类型 '{GetType().Name}' 的点操作");
    }

    #region boolOper

    public virtual bool Equal(ValueType? otherValueType) => false;
    public virtual bool Less(ValueType? otherValue) => throw new InvalidOperationError(this, "不支持Less操作");
    public virtual bool Greater(ValueType? otherValue) => throw new InvalidOperationError(this, "不支持Greater操作");
    public virtual bool LessEqual(ValueType? otherValue) => throw new InvalidOperationError(this, "不支持LessEqual操作");
    public virtual bool GreaterEqual(ValueType? otherValue) => throw new InvalidOperationError(this, "不支持GreaterEqual操作");

    #endregion

    public virtual ValueType Converse(ValueType otherValueType, VariateManager manager) => throw new InvalidOperationError(this, $"不支持类型 '{GetType().Name}' 转换为 '{otherValueType.GetType().Name}'");

    public override ValueType Run(VariateManager manager) => this;

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

    public T GetValue<T>()
    {
        return (T)GetValue();
    }

    public static ValueType ObjToValue(object? value)
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
            long a => new IntValue((int)a),
            _ => throw new InvalidOperationError(new SourcePosition(), $"不支持将类型 '{value.GetType().Name}' 转换为Old8Lang值")
        };
    }
}