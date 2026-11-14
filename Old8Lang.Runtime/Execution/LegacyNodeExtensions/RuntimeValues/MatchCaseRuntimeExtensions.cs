using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// MatchCase 的模式匹配执行辅助（阶段 2 从语法节点迁出）。
/// </summary>
internal static class MatchCaseRuntimeExtensions
{
    /// <summary>
    /// 检查给定的值是否匹配当前 case
    /// </summary>
    /// <param name="node">case 分支</param>
    /// <param name="value">要匹配的值</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="boundValues">输出绑定的变量字典（变量名 -> 值）</param>
    /// <returns>是否匹配</returns>
    internal static bool IsMatch(this MatchCase node, LangValueType value, VariateManager manager,
        out Dictionary<string, LangValueType>? boundValues)
    {
        boundValues = null;

        switch (node.Type)
        {
            case PatternType.Wildcard:
                // 通配符匹配所有值
                return true;

            case PatternType.VariableBinding:
                // 变量绑定匹配所有值，并绑定变量
                if (node.BindingVariable is not null)
                {
                    boundValues = new Dictionary<string, LangValueType>
                    {
                        [node.BindingVariable] = value
                    };
                }
                return true;

            case PatternType.Value:
                // 值匹配：计算 pattern 表达式并比较
                if (node.Pattern is not null)
                {
                    var patternValue = node.Pattern.Accept(RuntimeVisitors.CreateInterpreter(manager));
                    return value.Equal(patternValue);
                }
                return false;

            case PatternType.Tuple:
                // 元组解构匹配
                return MatchTuple(node, value, manager, out boundValues);

            case PatternType.TypeMatch:
                // 类型匹配（带可选守卫条件）
                return MatchType(node, value, manager, out boundValues);

            case PatternType.Range:
                // 范围匹配
                return MatchRange(node, value, manager);

            default:
                return false;
        }
    }

    /// <summary>
    /// 执行元组解构匹配
    /// </summary>
    private static bool MatchTuple(MatchCase node, LangValueType value, VariateManager manager,
        out Dictionary<string, LangValueType>? boundValues)
    {
        boundValues = null;

        if (node.TuplePattern is null)
            return false;

        // 检查值是否是元组类型
        if (value is not TupleLangValue tuple)
            return false;

        // 将元组展平为列表
        var tupleElements = FlattenTuple(tuple);

        // 检查元素数量是否匹配
        if (tupleElements.Count != node.TuplePattern.Elements.Count)
            return false;

        // 逐个匹配元组元素
        boundValues = new Dictionary<string, LangValueType>();

        for (int i = 0; i < node.TuplePattern.Elements.Count; i++)
        {
            var patternElement = node.TuplePattern.Elements[i];
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
                var patternValue = patternElement.Value.Accept(RuntimeVisitors.CreateInterpreter(manager));
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
    private static bool MatchType(MatchCase node, LangValueType value, VariateManager manager,
        out Dictionary<string, LangValueType>? boundValues)
    {
        boundValues = null;

        if (node.TypeAnnotation is null || node.BindingVariable is null)
            return false;

        // 检查类型是否匹配
        if (!CheckTypeMatch(value, node.TypeAnnotation))
            return false;

        // 创建临时作用域来绑定变量，用于守卫条件评估
        boundValues = new Dictionary<string, LangValueType>
        {
            [node.BindingVariable] = value
        };

        // 如果有守卫条件，需要在绑定变量后评估守卫条件
        if (node.GuardCondition is not null)
        {
            // 添加新的子作用域并绑定变量
            manager.AddChildren();
            try
            {
                var tempId = new LangId(node.BindingVariable);
                manager.Set(tempId, value);

                // 评估守卫条件
                var guardResult = node.GuardCondition.Accept(RuntimeVisitors.CreateInterpreter(manager));

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
    private static bool MatchRange(MatchCase node, LangValueType value, VariateManager manager)
    {
        if (node.RangePattern is null)
            return false;

        // 计算范围的起始值和结束值
        var startValue = node.RangePattern.Start.Accept(RuntimeVisitors.CreateInterpreter(manager));
        var endValue = node.RangePattern.End.Accept(RuntimeVisitors.CreateInterpreter(manager));

        // 只支持数值类型的范围匹配
        if (!TryGetNumericValue(value, out var numValue))
            return false;

        if (!TryGetNumericValue(startValue, out var startNum))
            return false;

        if (!TryGetNumericValue(endValue, out var endNum))
            return false;

        // 检查是否在范围内
        bool inRange = true;

        if (node.RangePattern.IncludeStart)
        {
            inRange &= numValue >= startNum;
        }
        else
        {
            inRange &= numValue > startNum;
        }

        if (node.RangePattern.IncludeEnd)
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
    /// 获取元组的元素列表
    /// </summary>
    private static List<LangValueType> FlattenTuple(TupleLangValue tuple)
    {
        return tuple.GetItems().ToList();
    }
}
