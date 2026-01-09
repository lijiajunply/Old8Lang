using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

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
public class MatchCase
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
    /// 检查给定的值是否匹配当前 case
    /// </summary>
    /// <param name="value">要匹配的值</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="boundValues">输出绑定的变量字典（变量名 -> 值）</param>
    /// <returns>是否匹配</returns>
    public bool IsMatch(LangValueType value, VariateManager manager, out Dictionary<string, LangValueType>? boundValues)
    {
        boundValues = null;

        switch (Type)
        {
            case PatternType.Wildcard:
                // 通配符匹配所有值
                return true;

            case PatternType.VariableBinding:
                // 变量绑定匹配所有值，并绑定变量
                if (BindingVariable is not null)
                {
                    boundValues = new Dictionary<string, LangValueType>
                    {
                        [BindingVariable] = value
                    };
                }
                return true;

            case PatternType.Value:
                // 值匹配：计算 pattern 表达式并比较
                if (Pattern is not null)
                {
                    var patternValue = Pattern.Run(manager);
                    return value.Equal(patternValue);
                }
                return false;

            case PatternType.Tuple:
                // 元组解构匹配
                return MatchTuple(value, manager, out boundValues);

            case PatternType.TypeMatch:
                // 类型匹配（带可选守卫条件）
                return MatchType(value, manager, out boundValues);

            case PatternType.Range:
                // 范围匹配
                return MatchRange(value, manager);

            default:
                return false;
        }
    }

    /// <summary>
    /// 执行元组解构匹配
    /// </summary>
    private bool MatchTuple(LangValueType value, VariateManager manager, out Dictionary<string, LangValueType>? boundValues)
    {
        boundValues = null;

        if (TuplePattern is null)
            return false;

        // 检查值是否是元组类型
        if (value is not TupleLangValue tuple)
            return false;

        // 将元组展平为列表
        var tupleElements = FlattenTuple(tuple);

        // 检查元素数量是否匹配
        if (tupleElements.Count != TuplePattern.Elements.Count)
            return false;

        // 逐个匹配元组元素
        boundValues = new Dictionary<string, LangValueType>();

        for (int i = 0; i < TuplePattern.Elements.Count; i++)
        {
            var patternElement = TuplePattern.Elements[i];
            var tupleElement = tupleElements[i];

            // 通配符 _ 匹配任意值，不绑定
            if (patternElement.IsWildcard)
            {
                continue;
            }

            // 变量绑定：绑定值到变量
            if (patternElement.Variable is not null)
            {
                boundValues[patternElement.Variable] = tupleElement;
                continue;
            }

            // 值匹配：比较值是否相等
            if (patternElement.Value is not null)
            {
                var patternValue = patternElement.Value.Run(manager);
                if (!tupleElement.Equal(patternValue))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 执行类型匹配（带可选守卫条件）
    /// </summary>
    private bool MatchType(LangValueType value, VariateManager manager, out Dictionary<string, LangValueType>? boundValues)
    {
        boundValues = null;

        if (TypeAnnotation is null || BindingVariable is null)
            return false;

        // 检查类型是否匹配
        if (!CheckTypeMatch(value, TypeAnnotation))
            return false;

        // 创建临时作用域来绑定变量，用于守卫条件评估
        boundValues = new Dictionary<string, LangValueType>
        {
            [BindingVariable] = value
        };

        // 如果有守卫条件，需要在绑定变量后评估守卫条件
        if (GuardCondition is not null)
        {
            // 添加新的子作用域并绑定变量
            manager.AddChildren();
            try
            {
                var tempId = new LangId(BindingVariable);
                manager.Set(tempId, value);

                // 评估守卫条件
                var guardResult = GuardCondition.Run(manager);

                // 守卫条件必须是布尔类型
                if (guardResult is BoolLangValue boolValue)
                {
                    return boolValue.Value;
                }

                // 非布尔类型的守卫条件视为 false
                return false;
            }
            finally
            {
                manager.RemoveChildren();
            }
        }

        return true;
    }

    /// <summary>
    /// 执行范围匹配
    /// </summary>
    private bool MatchRange(LangValueType value, VariateManager manager)
    {
        if (RangePattern is null)
            return false;

        // 计算范围的起始值和结束值
        var startValue = RangePattern.Start.Run(manager);
        var endValue = RangePattern.End.Run(manager);

        // 只支持数值类型的范围匹配
        if (!TryGetNumericValue(value, out var numValue))
            return false;

        if (!TryGetNumericValue(startValue, out var startNum))
            return false;

        if (!TryGetNumericValue(endValue, out var endNum))
            return false;

        // 检查是否在范围内
        bool inRange = true;

        if (RangePattern.IncludeStart)
        {
            inRange &= numValue >= startNum;
        }
        else
        {
            inRange &= numValue > startNum;
        }

        if (RangePattern.IncludeEnd)
        {
            inRange &= numValue <= endNum;
        }
        else
        {
            inRange &= numValue < endNum;
        }

        return inRange;
    }

    /// <summary>
    /// 检查值的类型是否匹配给定的类型注解
    /// </summary>
    private static bool CheckTypeMatch(LangValueType value, string typeAnnotation)
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
    private static bool TryGetNumericValue(LangValueType value, out double numValue)
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
    /// 将嵌套的元组展平为列表
    /// 例如：((1, 2), 3) -> [1, 2, 3]
    /// </summary>
    private static List<LangValueType> FlattenTuple(TupleLangValue tuple)
    {
        var result = new List<LangValueType>();

        var first = tuple.Value.Item1;
        var second = tuple.Value.Item2;

        // 递归展平第一个元素
        if (first is TupleLangValue firstTuple)
        {
            result.AddRange(FlattenTuple(firstTuple));
        }
        else if (first is not NullLangValue) // 排除单元素元组的 null 占位符
        {
            result.Add(first);
        }

        // 递归展平第二个元素
        if (second is TupleLangValue secondTuple)
        {
            result.AddRange(FlattenTuple(secondTuple));
        }
        else if (second is not NullLangValue) // 排除单元素元组的 null 占位符
        {
            result.Add(second);
        }

        return result;
    }
}
