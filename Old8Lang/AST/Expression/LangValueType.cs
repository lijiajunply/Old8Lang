using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 表示值
/// </summary>
/// <remarks>
/// 构造函数
/// </remarks>
/// <param name="position">位置信息</param>
public abstract class LangValueType(SourcePosition position = default) : LangExpression(position)
{
    public override string ToString() => GetValue().ToString()!;
    
    public abstract override T Accept<T>(IVisitor<T> visitor);

    #region intOper

    public virtual LangValueType Plus(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的加法操作");

    public virtual LangValueType Minus(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的减法操作");

    public virtual LangValueType Times(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的乘法操作");

    public virtual LangValueType Divide(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的除法操作");

    public virtual LangValueType Mod(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的取模操作");

    public virtual LangValueType Power(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的幂运算");

    #endregion

    public virtual LangValueType Dot(LangExpression dotExpression)
    {
        if (dotExpression is LangId id)
        {
            if (id.IdName == "XAUAT")
                return new StringLangValue("西建大还我血汗钱我要回家");
            throw new AttributeError(this, id.IdName, GetType().Name);
        }

        if (dotExpression is Instance instance)
        {
            return instance.FromClassToResult(this);
        }

        throw new InvalidOperationError(this, $"不支持类型 '{GetType().Name}' 的点操作");
    }

    #region boolOper

    public virtual bool Equal(LangValueType? otherValueType) => false;
    public virtual bool Less(LangValueType? otherValue) => throw new InvalidOperationError(this, "不支持Less操作");
    public virtual bool Greater(LangValueType? otherValue) => throw new InvalidOperationError(this, "不支持Greater操作");
    public virtual bool LessEqual(LangValueType? otherValue) => throw new InvalidOperationError(this, "不支持LessEqual操作");

    public virtual bool GreaterEqual(LangValueType? otherValue) =>
        throw new InvalidOperationError(this, "不支持GreaterEqual操作");

    #endregion

    public virtual LangValueType Converse(LangValueType otherLangValueType, VariateManager manager) =>
        throw new InvalidOperationError(this, $"不支持类型 '{GetType().Name}' 转换为 '{otherLangValueType.GetType().Name}'");

    public override LangValueType Run(VariateManager manager) => this;

    public string TypeToString()
    {
        return this switch
        {
            AnyLangValue a => a.Id.IdName,
            ArrayLangValue => "Array",
            BoolLangValue => "Bool",
            CharLangValue => "Char",
            DictionaryLangValue => "Dictionary",
            DoubleLangValue => "Double",
            FuncLangValue func => $"Function {func.Id}({Apis.ListToString(func.Ids)})",
            Instance instance => $"Instance {instance}",
            IntLangValue => "Int",
            NullLangValue => "Null",
            LangListItem item => $"Item {item}",
            ListLangValue => "List",
            StringLangValue => "String",
            TypeLangValue => "Type",
            TupleLangValue => "Tuple",
            _ => "Value"
        };
    }

    public virtual object GetValue() => new();

    public T GetValue<T>()
    {
        return (T)GetValue();
    }

    public virtual string ToDisplayString() => ToString(); // 默认使用ToString()，子类可以重写

    public static LangValueType ObjToValue(object? value)
    {
        if (value == null)
        {
            return new NullLangValue();
        }

        return value switch
        {
            int a => new IntLangValue(a),
            string a => new StringLangValue(a),
            double a => new DoubleLangValue(a),
            char a => new CharLangValue(a),
            List<object> a => new ListLangValue(a),
            List<string> a => new ListLangValue(a.Cast<object>().ToList()),
            List<int> a => new ListLangValue(a.Cast<object>().ToList()),
            List<double> a => new ListLangValue(a.Cast<object>().ToList()),
            object[] a => new ArrayLangValue(a.ToList()),
            long a => new IntLangValue((int)a),
            bool a => new BoolLangValue(a),
            Dictionary<object, object> a => new DictionaryLangValue(a.Select(x =>
            {
                var key = ObjToValue(x.Key);
                var val = ObjToValue(x.Value);
                return new KeyValuePair<LangExpression, LangExpression>(key, val);
            }).ToList()),
            Tuple<object, object> a => new TupleLangValue(ObjToValue(a.Item1), ObjToValue(a.Item2)),
            ValueTuple<object, object> a => new TupleLangValue(ObjToValue(a.Item1), ObjToValue(a.Item2)),
            _ => throw new InvalidOperationError(new SourcePosition(), $"不支持将类型 '{value.GetType().Name}' 转换为Old8Lang值")
        };
    }
}