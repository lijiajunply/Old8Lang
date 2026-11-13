using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// MatchCase 的执行/编译辅助实现（原位于 AST 目录，阶段 1 迁出）。
/// </summary>
public partial class MatchCase
{
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
                    var patternValue = Pattern.Accept(new InterpreterVisitor(manager));
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
    internal bool MatchTuple(LangValueType value, VariateManager manager, out Dictionary<string, LangValueType>? boundValues)
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
                var patternValue = patternElement.Value.Accept(new InterpreterVisitor(manager));
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
    internal bool MatchType(LangValueType value, VariateManager manager, out Dictionary<string, LangValueType>? boundValues)
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
                var guardResult = GuardCondition.Accept(new InterpreterVisitor(manager));

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
    internal bool MatchRange(LangValueType value, VariateManager manager)
    {
        if (RangePattern is null)
            return false;

        // 计算范围的起始值和结束值
        var startValue = RangePattern.Start.Accept(new InterpreterVisitor(manager));
        var endValue = RangePattern.End.Accept(new InterpreterVisitor(manager));

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
}
