using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 语言值类型抽象基类，所有Old8Lang表达式值都继承自此类
/// </summary>
/// <remarks>
/// 该类是Old8Lang表达式系统的核心，定义了所有值类型必须实现的基本操作
/// 包括算术运算、比较运算、类型转换、点操作等
/// </remarks>
/// <param name="position">源代码位置信息，用于错误报告</param>
public abstract class LangValueType(SourcePosition position = default) : LangExpression(position)
{
    /// <summary>
    /// 将值转换为字符串表示
    /// </summary>
    /// <returns>值的字符串表示</returns>
    public override string ToString() => GetValue().ToString()!;

    #region 算术运算

    /// <summary>
    /// 加法运算
    /// </summary>
    /// <param name="otherLangValueType">另一个操作数</param>
    /// <returns>加法运算结果</returns>
    /// <exception cref="InvalidOperationError">当不支持加法运算时抛出</exception>
    public virtual LangValueType Plus(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的加法操作");

    /// <summary>
    /// 减法运算
    /// </summary>
    /// <param name="otherLangValueType">另一个操作数</param>
    /// <returns>减法运算结果</returns>
    /// <exception cref="InvalidOperationError">当不支持减法运算时抛出</exception>
    public virtual LangValueType Minus(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的减法操作");

    /// <summary>
    /// 乘法运算
    /// </summary>
    /// <param name="otherLangValueType">另一个操作数</param>
    /// <returns>乘法运算结果</returns>
    /// <exception cref="InvalidOperationError">当不支持乘法运算时抛出</exception>
    public virtual LangValueType Times(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的乘法操作");

    /// <summary>
    /// 除法运算
    /// </summary>
    /// <param name="otherLangValueType">另一个操作数</param>
    /// <returns>除法运算结果</returns>
    /// <exception cref="InvalidOperationError">当不支持除法运算时抛出</exception>
    public virtual LangValueType Divide(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的除法操作");

    /// <summary>
    /// 取模运算
    /// </summary>
    /// <param name="otherLangValueType">另一个操作数</param>
    /// <returns>取模运算结果</returns>
    /// <exception cref="InvalidOperationError">当不支持取模运算时抛出</exception>
    public virtual LangValueType Mod(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的取模操作");

    /// <summary>
    /// 幂运算
    /// </summary>
    /// <param name="otherLangValueType">另一个操作数</param>
    /// <returns>幂运算结果</returns>
    /// <exception cref="InvalidOperationError">当不支持幂运算时抛出</exception>
    public virtual LangValueType Power(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的幂运算");

    #endregion

    /// <summary>
    /// 点操作，用于访问对象的属性或方法
    /// </summary>
    /// <param name="dotExpression">点后面的表达式</param>
    /// <returns>点操作结果</returns>
    /// <exception cref="AttributeError">当访问不存在的属性时抛出</exception>
    /// <exception cref="InvalidOperationError">当不支持点操作时抛出</exception>
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

    #region 比较运算

    /// <summary>
    /// 相等比较
    /// </summary>
    /// <param name="otherValueType">另一个值</param>
    /// <returns>比较结果，相等返回true，否则返回false</returns>
    public virtual bool Equal(LangValueType? otherValueType) => false;
    
    /// <summary>
    /// 小于比较
    /// </summary>
    /// <param name="otherValue">另一个值</param>
    /// <returns>比较结果，小于返回true，否则返回false</returns>
    /// <exception cref="InvalidOperationError">当不支持小于比较时抛出</exception>
    public virtual bool Less(LangValueType? otherValue) => throw new InvalidOperationError(this, "不支持Less操作");
    
    /// <summary>
    /// 大于比较
    /// </summary>
    /// <param name="otherValue">另一个值</param>
    /// <returns>比较结果，大于返回true，否则返回false</returns>
    /// <exception cref="InvalidOperationError">当不支持大于比较时抛出</exception>
    public virtual bool Greater(LangValueType? otherValue) => throw new InvalidOperationError(this, "不支持Greater操作");
    
    /// <summary>
    /// 小于等于比较
    /// </summary>
    /// <param name="otherValue">另一个值</param>
    /// <returns>比较结果，小于等于返回true，否则返回false</returns>
    /// <exception cref="InvalidOperationError">当不支持小于等于比较时抛出</exception>
    public virtual bool LessEqual(LangValueType? otherValue) => throw new InvalidOperationError(this, "不支持LessEqual操作");

    /// <summary>
    /// 大于等于比较
    /// </summary>
    /// <param name="otherValue">另一个值</param>
    /// <returns>比较结果，大于等于返回true，否则返回false</returns>
    /// <exception cref="InvalidOperationError">当不支持大于等于比较时抛出</exception>
    public virtual bool GreaterEqual(LangValueType? otherValue) =>
        throw new InvalidOperationError(this, "不支持GreaterEqual操作");

    #endregion

    /// <summary>
    /// 类型转换
    /// </summary>
    /// <param name="otherLangValueType">目标类型值</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>转换后的结果</returns>
    /// <exception cref="InvalidOperationError">当不支持类型转换时抛出</exception>
    public virtual LangValueType Converse(LangValueType otherLangValueType, VariateManager manager) =>
        throw new InvalidOperationError(this, $"不支持类型 '{GetType().Name}' 转换为 '{otherLangValueType.GetType().Name}'");

    /// <summary>
    /// 执行表达式，返回自身
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <returns>自身实例</returns>
    public override LangValueType Run(VariateManager manager) => this;

    /// <summary>
    /// 将值类型转换为字符串表示
    /// </summary>
    /// <returns>值类型的字符串表示</returns>
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

    /// <summary>
    /// 获取值的实际.NET对象
    /// </summary>
    /// <returns>值对应的.NET对象</returns>
    public virtual object GetValue() => new();

    /// <summary>
    /// 获取值的实际.NET对象，并转换为指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>转换后的.NET对象</returns>
    /// <exception cref="InvalidCastException">当无法转换为指定类型时抛出</exception>
    public T GetValue<T>()
    {
        return (T)GetValue();
    }

    /// <summary>
    /// 获取值的显示字符串，用于调试和输出
    /// </summary>
    /// <returns>值的显示字符串</returns>
    public virtual string ToDisplayString() => ToString(); // 默认使用ToString()，子类可以重写

    /// <summary>
    /// 将.NET对象转换为Old8Lang值类型
    /// </summary>
    /// <param name="value">要转换的.NET对象</param>
    /// <returns>对应的Old8Lang值类型</returns>
    /// <exception cref="InvalidOperationError">当不支持转换的类型时抛出</exception>
    /// <remarks>
    /// 支持的转换类型包括：
    /// - int → IntLangValue
    /// - string → StringLangValue
    /// - double → DoubleLangValue
    /// - char → CharLangValue
    /// - List&lt;object&gt; → ListLangValue
    /// - List&lt;string&gt; → ListLangValue
    /// - List&lt;int&gt; → ListLangValue
    /// - List&lt;double&gt; → ListLangValue
    /// - object[] → ArrayLangValue
    /// - long → IntLangValue
    /// - bool → BoolLangValue
    /// - Dictionary&lt;object, object&gt; → DictionaryLangValue
    /// - Tuple&lt;object, object&gt; → TupleLangValue
    /// - ValueTuple&lt;object, object&gt; → TupleLangValue
    /// </remarks>
    public static LangValueType ObjToValue(object? value)
    {
        if (value == null)
        {
            return new NullLangValue();
        }

        return value switch
        {
            int a => IntLangValue.Create(a),
            string a => StringLangValue.Create(a),
            double a => DoubleLangValue.Create(a),
            char a => CharLangValue.Create(a),
            List<object> a => new ListLangValue(a),
            List<string> a => new ListLangValue(a.Cast<object>().ToList()),
            List<int> a => new ListLangValue(a.Cast<object>().ToList()),
            List<double> a => new ListLangValue(a.Cast<object>().ToList()),
            object[] a => new ArrayLangValue(a.ToList()),
            long a => IntLangValue.Create((int)a),
            bool a => BoolLangValue.Create(a),
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