using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// Match 表达式的 case 分支
/// 支持多种模式：
/// 1. 值匹配: case 0 -> "zero"
/// 2. 变量绑定: case x -> "value is {x}"
/// 3. 通配符: case _ -> "default"
/// 4. 元组解构: case (x, 0) -> "on X-axis"
/// 5. 类型匹配: case x:int -> "int value"
/// 6. 范围匹配: case [0~12] -> "child"
/// 7. 守卫条件: case x:int if x > 0 -> "positive"
/// </summary>
public partial class MatchCase
{
    /// <summary>
    /// 模式类型
    /// </summary>
    public PatternType Type { get; }

    /// <summary>
    /// case 的模式表达式（用于值匹配）
    /// 如果是其他模式类型，此值为 null
    /// </summary>
    public LangExpression? Pattern { get; }

    /// <summary>
    /// 变量绑定名称（用于变量绑定、类型匹配模式）
    /// 如果不使用变量绑定，此值为 null
    /// </summary>
    public string? BindingVariable { get; }

    /// <summary>
    /// 类型注解（用于类型匹配模式）
    /// 例如：case x:int -> ...
    /// </summary>
    public string? TypeAnnotation { get; }

    /// <summary>
    /// 守卫条件表达式（可选）
    /// 例如：case x:int if x > 0 -> ...
    /// </summary>
    public LangExpression? GuardCondition { get; }

    /// <summary>
    /// 元组解构模式（用于元组匹配）
    /// </summary>
    public TuplePattern? TuplePattern { get; }

    /// <summary>
    /// 范围模式（用于范围匹配）
    /// </summary>
    public RangePattern? RangePattern { get; }

    /// <summary>
    /// case 匹配后执行的表达式
    /// </summary>
    public LangExpression ResultExpression { get; }

    /// <summary>
    /// 是否是通配符模式
    /// </summary>
    public bool IsWildcard => Type == PatternType.Wildcard;

    /// <summary>
    /// 是否是变量绑定模式
    /// </summary>
    public bool IsVariableBinding => Type == PatternType.VariableBinding;

    /// <summary>
    /// 构造函数 - 值匹配模式
    /// </summary>
    public MatchCase(LangExpression pattern, LangExpression resultExpression)
    {
        Type = PatternType.Value;
        Pattern = pattern;
        ResultExpression = resultExpression;
    }

    /// <summary>
    /// 构造函数 - 变量绑定或通配符模式
    /// </summary>
    public MatchCase(string bindingVariable, LangExpression resultExpression, bool isWildcard = false)
    {
        Type = isWildcard ? PatternType.Wildcard : PatternType.VariableBinding;
        BindingVariable = bindingVariable;
        ResultExpression = resultExpression;
    }

    /// <summary>
    /// 构造函数 - 元组解构模式
    /// </summary>
    public MatchCase(TuplePattern tuplePattern, LangExpression resultExpression)
    {
        Type = PatternType.Tuple;
        TuplePattern = tuplePattern;
        ResultExpression = resultExpression;
    }

    /// <summary>
    /// 构造函数 - 类型匹配模式（带可选守卫条件）
    /// </summary>
    public MatchCase(
        string bindingVariable,
        string typeAnnotation,
        LangExpression resultExpression,
        LangExpression? guardCondition = null)
    {
        Type = PatternType.TypeMatch;
        BindingVariable = bindingVariable;
        TypeAnnotation = typeAnnotation;
        GuardCondition = guardCondition;
        ResultExpression = resultExpression;
    }

    /// <summary>
    /// 构造函数 - 范围匹配模式
    /// </summary>
    public MatchCase(RangePattern rangePattern, LangExpression resultExpression)
    {
        Type = PatternType.Range;
        RangePattern = rangePattern;
        ResultExpression = resultExpression;
    }





    /// <summary>
    /// 检查值的类型是否匹配给定的类型注解
    /// </summary>
    internal static bool CheckTypeMatch(LangValueType value, string typeAnnotation)
    {
        return typeAnnotation.ToLower() switch
        {
            "int" => value is IntLangValue,
            "double" => value is DoubleLangValue,
            "string" => value is StringLangValue,
            "bool" => value is BoolLangValue,
            "char" => value is CharLangValue,
            "list" => value is ListLangValue,
            "dict" => value is DictionaryLangValue,
            "tuple" => value is TupleLangValue,
            "array" => value is ArrayLangValue,
            _ => false
        };
    }

    /// <summary>
    /// 尝试从值中获取数值
    /// </summary>
    internal static bool TryGetNumericValue(LangValueType value, out double numValue)
    {
        numValue = 0;

        switch (value)
        {
            case IntLangValue intVal:
                numValue = intVal.Value;
                return true;
            case DoubleLangValue doubleVal:
                numValue = doubleVal.Value;
                return true;
            case CharLangValue charVal:
                numValue = charVal.Value;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// 获取元组的元素列表
    /// </summary>
    internal static List<LangValueType> FlattenTuple(TupleLangValue tuple)
    {
        return tuple.GetItems().ToList();
    }
}
