using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Interpreter;
using Old8Lang.Error;
using System.Collections;

namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LINQ 查询执行器
/// 负责将 Old8Lang 的 LINQ 查询转换为 .NET LINQ 执行
/// </summary>
public class LinqQueryExecutor(VariateManager manager)
{
    /// <summary>
    /// 执行 LINQ 查询表达式
    /// </summary>
    public LangValueType ExecuteQuery(LinqExpression linqExpr)
    {
        // 获取初始数据源
        var sourceValue = linqExpr.FromClause.DataSource.Run(manager);
        var dataSource = ConvertToEnumerable(sourceValue);

        // 检查是否有 OrderBy 子句
        var hasOrderBy = linqExpr.BodyClauses.Any(c => c is OrderByClause);

        // 使用统一的迭代器处理所有子句
        var intermediateResults = new List<(object? item, Dictionary<string, LangValueType> letVariables)>();
        var result = new List<object?>();
        var rangeLangId = new LangId(linqExpr.FromClause.RangeVariable);

        foreach (var item in dataSource)
        {
            // 设置范围变量
            var oldRangeValue = manager.GetValue(rangeLangId);
            manager.Set(rangeLangId, ConvertToLangValue(item));

            try
            {
                // 按顺序执行所有查询体子句，如果任何 where 条件失败则跳过该元素
                var shouldInclude = true;
                var letVariables = new Dictionary<string, LangValueType>();

                // 处理所有 let 和 where 子句
                foreach (var clause in linqExpr.BodyClauses)
                {
                    if (clause is LetClause letClause)
                    {
                        // 计算并设置 let 变量
                        var letValue = letClause.Expression.Run(manager);
                        var letId = new LangId(letClause.Variable);
                        manager.Set(letId, letValue);
                        letVariables[letClause.Variable] = letValue;
                    }
                    else if (clause is WhereClause whereClause)
                    {
                        // 检查 where 条件
                        var conditionResult = whereClause.Condition.Run(manager);
                        if (!IsTruthy(conditionResult))
                        {
                            shouldInclude = false;
                            break;
                        }
                    }
                    else if (clause is OrderByClause)
                    {
                        // OrderBy 需要在收集所有元素后统一处理
                        // 这里暂时不处理，后续会处理
                    }
                }

                // 如果通过了所有 where 条件
                if (shouldInclude)
                {
                    if (hasOrderBy)
                    {
                        // 如果有 OrderBy，先保存中间结果（原始元素和 let 变量）
                        intermediateResults.Add((item, letVariables));
                    }
                    else
                    {
                        // 如果没有 OrderBy，直接执行 select
                        if (linqExpr.TerminationClause is SelectClause selectClause)
                        {
                            var projectedValue = selectClause.Projection.Run(manager);
                            result.Add(ConvertFromLangValue(projectedValue));
                        }
                        else if (linqExpr.TerminationClause is GroupByClause)
                        {
                            // GroupBy 需要特殊处理，稍后实现
                            result.Add(item);
                        }
                    }
                }
            }
            finally
            {
                // 恢复范围变量
                if (oldRangeValue is not null)
                    manager.Set(rangeLangId, oldRangeValue);
            }
        }

        // 处理 OrderBy 子句（如果有）
        var orderByClause = linqExpr.BodyClauses.OfType<OrderByClause>().FirstOrDefault();
        if (orderByClause is not null)
        {
            // 在排序后执行 select
            var sortedResults = ExecuteOrderByOnIntermediateResults(
                orderByClause,
                intermediateResults,
                linqExpr.FromClause.RangeVariable);

            // 对排序后的结果执行 select
            foreach (var (item, letVariables) in sortedResults)
            {
                var oldRangeValue = manager.GetValue(rangeLangId);
                manager.Set(rangeLangId, ConvertToLangValue(item));

                try
                {
                    // 恢复 let 变量
                    foreach (var (varName, varValue) in letVariables)
                    {
                        manager.Set(new LangId(varName), varValue);
                    }

                    // 执行 select
                    if (linqExpr.TerminationClause is SelectClause selectClause)
                    {
                        var projectedValue = selectClause.Projection.Run(manager);
                        result.Add(ConvertFromLangValue(projectedValue));
                    }
                    else if (linqExpr.TerminationClause is GroupByClause)
                    {
                        // GroupBy 需要特殊处理，稍后实现
                        result.Add(item);
                    }
                }
                finally
                {
                    if (oldRangeValue is not null)
                        manager.Set(rangeLangId, oldRangeValue);
                }
            }
        }

        // 处理查询延续（into）
        if (linqExpr.Continuation is not null)
        {
            result = ExecuteContinuation(linqExpr.Continuation, result).Cast<object?>().ToList();
        }

        // 将结果转换为 Old8Lang 的 List
        return ConvertToLangList(result);
    }

    /// <summary>
    /// 在中间结果上执行 OrderBy（包含 let 变量）
    /// </summary>
    private List<(object? item, Dictionary<string, LangValueType> letVariables)> ExecuteOrderByOnIntermediateResults(
        OrderByClause orderByClause,
        List<(object? item, Dictionary<string, LangValueType> letVariables)> intermediateResults,
        string rangeVariable)
    {
        var rangeLangId = new LangId(rangeVariable);

        if (orderByClause.Orderings.Count == 0)
            return intermediateResults;

        // 第一个排序键
        var firstOrdering = orderByClause.Orderings[0];
        IOrderedEnumerable<(object? item, Dictionary<string, LangValueType> letVariables)> orderedList;

        if (firstOrdering.IsAscending)
        {
            orderedList = intermediateResults.OrderBy(entry =>
            {
                var (item, letVariables) = entry;
                var oldValue = manager.GetValue(rangeLangId);
                manager.Set(rangeLangId, ConvertToLangValue(item));
                try
                {
                    // 恢复 let 变量
                    foreach (var (varName, varValue) in letVariables)
                    {
                        manager.Set(new LangId(varName), varValue);
                    }

                    var keyValue = firstOrdering.KeyExpression.Run(manager);
                    return ConvertFromLangValue(keyValue);
                }
                finally
                {
                    if (oldValue is not null)
                        manager.Set(rangeLangId, oldValue);
                }
            });
        }
        else
        {
            orderedList = intermediateResults.OrderByDescending(entry =>
            {
                var (item, letVariables) = entry;
                var oldValue = manager.GetValue(rangeLangId);
                manager.Set(rangeLangId, ConvertToLangValue(item));
                try
                {
                    // 恢复 let 变量
                    foreach (var (varName, varValue) in letVariables)
                    {
                        manager.Set(new LangId(varName), varValue);
                    }

                    var keyValue = firstOrdering.KeyExpression.Run(manager);
                    return ConvertFromLangValue(keyValue);
                }
                finally
                {
                    if (oldValue is not null)
                        manager.Set(rangeLangId, oldValue);
                }
            });
        }

        // 后续排序键
        for (int i = 1; i < orderByClause.Orderings.Count; i++)
        {
            var ordering = orderByClause.Orderings[i];
            if (ordering.IsAscending)
            {
                orderedList = orderedList.ThenBy(entry =>
                {
                    var (item, letVariables) = entry;
                    var oldValue = manager.GetValue(rangeLangId);
                    manager.Set(rangeLangId, ConvertToLangValue(item));
                    try
                    {
                        // 恢复 let 变量
                        foreach (var (varName, varValue) in letVariables)
                        {
                            manager.Set(new LangId(varName), varValue);
                        }

                        var keyValue = ordering.KeyExpression.Run(manager);
                        return ConvertFromLangValue(keyValue);
                    }
                    finally
                    {
                        if (oldValue is not null)
                            manager.Set(rangeLangId, oldValue);
                    }
                });
            }
            else
            {
                orderedList = orderedList.ThenByDescending(entry =>
                {
                    var (item, letVariables) = entry;
                    var oldValue = manager.GetValue(rangeLangId);
                    manager.Set(rangeLangId, ConvertToLangValue(item));
                    try
                    {
                        // 恢复 let 变量
                        foreach (var (varName, varValue) in letVariables)
                        {
                            manager.Set(new LangId(varName), varValue);
                        }

                        var keyValue = ordering.KeyExpression.Run(manager);
                        return ConvertFromLangValue(keyValue);
                    }
                    finally
                    {
                        if (oldValue is not null)
                            manager.Set(rangeLangId, oldValue);
                    }
                });
            }
        }

        return orderedList.ToList();
    }

    /// <summary>
    /// 在结果集上执行 OrderBy（已废弃，保留用于兼容）
    /// </summary>
    private List<object?> ExecuteOrderByOnResult(OrderByClause orderByClause, List<object?> result, string rangeVariable)
    {
        var rangeLangId = new LangId(rangeVariable);

        if (orderByClause.Orderings.Count == 0)
            return result;

        // 第一个排序键
        var firstOrdering = orderByClause.Orderings[0];
        IOrderedEnumerable<object?> orderedList;

        if (firstOrdering.IsAscending)
        {
            orderedList = result.OrderBy(item =>
            {
                var oldValue = manager.GetValue(rangeLangId);
                manager.Set(rangeLangId, ConvertToLangValue(item));
                try
                {
                    var keyValue = firstOrdering.KeyExpression.Run(manager);
                    return ConvertFromLangValue(keyValue);
                }
                finally
                {
                    if (oldValue is not null)
                        manager.Set(rangeLangId, oldValue);
                }
            });
        }
        else
        {
            orderedList = result.OrderByDescending(item =>
            {
                var oldValue = manager.GetValue(rangeLangId);
                manager.Set(rangeLangId, ConvertToLangValue(item));
                try
                {
                    var keyValue = firstOrdering.KeyExpression.Run(manager);
                    return ConvertFromLangValue(keyValue);
                }
                finally
                {
                    if (oldValue is not null)
                        manager.Set(rangeLangId, oldValue);
                }
            });
        }

        // 后续排序键
        for (int i = 1; i < orderByClause.Orderings.Count; i++)
        {
            var ordering = orderByClause.Orderings[i];
            if (ordering.IsAscending)
            {
                orderedList = orderedList.ThenBy(item =>
                {
                    var oldValue = manager.GetValue(rangeLangId);
                    manager.Set(rangeLangId, ConvertToLangValue(item));
                    try
                    {
                        var keyValue = ordering.KeyExpression.Run(manager);
                        return ConvertFromLangValue(keyValue);
                    }
                    finally
                    {
                        if (oldValue is not null)
                            manager.Set(rangeLangId, oldValue);
                    }
                });
            }
            else
            {
                orderedList = orderedList.ThenByDescending(item =>
                {
                    var oldValue = manager.GetValue(rangeLangId);
                    manager.Set(rangeLangId, ConvertToLangValue(item));
                    try
                    {
                        var keyValue = ordering.KeyExpression.Run(manager);
                        return ConvertFromLangValue(keyValue);
                    }
                    finally
                    {
                        if (oldValue is not null)
                            manager.Set(rangeLangId, oldValue);
                    }
                });
            }
        }

        return orderedList.ToList();
    }

    /// <summary>
    /// 执行 from 子句
    /// </summary>
    private IEnumerable ExecuteFromClause(FromClause fromClause)
    {
        var sourceValue = fromClause.DataSource.Run(manager);
        return ConvertToEnumerable(sourceValue);
    }

    /// <summary>
    /// 执行查询体子句
    /// </summary>
    private IEnumerable ExecuteBodyClause(LinqClause clause, IEnumerable source, string rangeVariable)
    {
        return clause switch
        {
            WhereClause whereClause => ExecuteWhere(whereClause, source, rangeVariable),
            OrderByClause orderByClause => ExecuteOrderBy(orderByClause, source, rangeVariable),
            LetClause letClause => ExecuteLet(letClause, source, rangeVariable),
            JoinClause joinClause => ExecuteJoin(joinClause, source, rangeVariable),
            FromClause fromClause => ExecuteFromClause(fromClause),
            _ => throw new InvalidOperationError(clause, $"不支持的查询体子句类型: {clause.GetType().Name}")
        };
    }

    /// <summary>
    /// 执行终止子句
    /// </summary>
    private IEnumerable ExecuteTerminationClause(LinqClause clause, IEnumerable source, string rangeVariable)
    {
        return clause switch
        {
            SelectClause selectClause => ExecuteSelect(selectClause, source, rangeVariable),
            GroupByClause groupByClause => ExecuteGroupBy(groupByClause, source, rangeVariable),
            _ => throw new InvalidOperationError(clause, $"不支持的终止子句类型: {clause.GetType().Name}")
        };
    }

    /// <summary>
    /// 执行 where 子句（转换为 .NET Where）
    /// </summary>
    private IEnumerable ExecuteWhere(WhereClause whereClause, IEnumerable source, string rangeVariable)
    {
        var result = new List<object?>();
        var rangeLangId = new LangId(rangeVariable);

        foreach (var item in source)
        {
            // 在当前作用域中设置范围变量
            var oldValue = manager.GetValue(rangeLangId);
            manager.Set(rangeLangId, ConvertToLangValue(item));

            try
            {
                // 执行 where 条件表达式
                var conditionResult = whereClause.Condition.Run(manager);

                // 判断条件是否为真
                if (IsTruthy(conditionResult))
                {
                    result.Add(item);
                }
            }
            finally
            {
                // 恢复范围变量
                if (oldValue is not null)
                    manager.Set(rangeLangId, oldValue);
            }
        }

        return result;
    }

    /// <summary>
    /// 执行 select 子句（转换为 .NET Select）
    /// </summary>
    private IEnumerable ExecuteSelect(SelectClause selectClause, IEnumerable source, string rangeVariable)
    {
        var result = new List<object?>();
        var rangeLangId = new LangId(rangeVariable);

        foreach (var item in source)
        {
            // 在当前作用域中设置范围变量
            var oldValue = manager.GetValue(rangeLangId);
            manager.Set(rangeLangId, ConvertToLangValue(item));

            try
            {
                // 执行 select 投影表达式
                var projectedValue = selectClause.Projection.Run(manager);
                result.Add(ConvertFromLangValue(projectedValue));
            }
            finally
            {
                // 恢复范围变量
                if (oldValue is not null)
                    manager.Set(rangeLangId, oldValue);
            }
        }

        return result;
    }

    /// <summary>
    /// 执行 orderby 子句（转换为 .NET OrderBy/ThenBy）
    /// </summary>
    private IEnumerable ExecuteOrderBy(OrderByClause orderByClause, IEnumerable source, string rangeVariable)
    {
        var list = source.Cast<object?>().ToList();
        var rangeLangId = new LangId(rangeVariable);

        if (orderByClause.Orderings.Count == 0)
            return list;

        // 第一个排序键
        var firstOrdering = orderByClause.Orderings[0];
        IOrderedEnumerable<object?> orderedList;

        if (firstOrdering.IsAscending)
        {
            orderedList = list.OrderBy(item =>
            {
                var oldValue = manager.GetValue(rangeLangId);
                manager.Set(rangeLangId, ConvertToLangValue(item));
                try
                {
                    var keyValue = firstOrdering.KeyExpression.Run(manager);
                    return ConvertFromLangValue(keyValue);
                }
                finally
                {
                    if (oldValue is not null)
                        manager.Set(rangeLangId, oldValue);
                }
            });
        }
        else
        {
            orderedList = list.OrderByDescending(item =>
            {
                var oldValue = manager.GetValue(rangeLangId);
                manager.Set(rangeLangId, ConvertToLangValue(item));
                try
                {
                    var keyValue = firstOrdering.KeyExpression.Run(manager);
                    return ConvertFromLangValue(keyValue);
                }
                finally
                {
                    if (oldValue is not null)
                        manager.Set(rangeLangId, oldValue);
                }
            });
        }

        // 后续排序键（ThenBy）
        for (int i = 1; i < orderByClause.Orderings.Count; i++)
        {
            var ordering = orderByClause.Orderings[i];
            if (ordering.IsAscending)
            {
                orderedList = orderedList.ThenBy(item =>
                {
                    var oldValue = manager.GetValue(rangeLangId);
                    manager.Set(rangeLangId, ConvertToLangValue(item));
                    try
                    {
                        var keyValue = ordering.KeyExpression.Run(manager);
                        return ConvertFromLangValue(keyValue);
                    }
                    finally
                    {
                        if (oldValue is not null)
                            manager.Set(rangeLangId, oldValue);
                    }
                });
            }
            else
            {
                orderedList = orderedList.ThenByDescending(item =>
                {
                    var oldValue = manager.GetValue(rangeLangId);
                    manager.Set(rangeLangId, ConvertToLangValue(item));
                    try
                    {
                        var keyValue = ordering.KeyExpression.Run(manager);
                        return ConvertFromLangValue(keyValue);
                    }
                    finally
                    {
                        if (oldValue is not null)
                            manager.Set(rangeLangId, oldValue);
                    }
                });
            }
        }

        return orderedList.ToList();
    }

    /// <summary>
    /// 执行 group by 子句（转换为 .NET GroupBy）
    /// </summary>
    private IEnumerable ExecuteGroupBy(GroupByClause groupByClause, IEnumerable source, string rangeVariable)
    {
        var groups = new Dictionary<object, List<object?>>();
        var rangeLangId = new LangId(rangeVariable);

        foreach (var item in source)
        {
            var oldValue = manager.GetValue(rangeLangId);
            manager.Set(rangeLangId, ConvertToLangValue(item));

            try
            {
                // 计算分组键
                var keyValue = groupByClause.KeyExpression.Run(manager);
                var key = ConvertFromLangValue(keyValue);

                // 计算分组元素
                var elementValue = groupByClause.ElementExpression.Run(manager);
                var element = ConvertFromLangValue(elementValue);

                // 添加到分组
                if (!groups.ContainsKey(key))
                    groups[key] = new List<object?>();

                groups[key].Add(element);
            }
            finally
            {
                if (oldValue is not null)
                    manager.Set(rangeLangId, oldValue);
            }
        }

        // 返回分组结果（每个分组包含 Key 和 Values）
        return groups.Select(g => new { g.Key, Values = g.Value }).ToList();
    }

    /// <summary>
    /// 执行 let 子句
    /// </summary>
    private IEnumerable ExecuteLet(LetClause letClause, IEnumerable source, string rangeVariable)
    {
        // let 子句需要创建新的匿名对象，包含原有变量和新变量
        // 暂时简化实现：直接在作用域中设置新变量
        var result = new List<object?>();
        var rangeLangId = new LangId(rangeVariable);
        var letLangId = new LangId(letClause.Variable);

        foreach (var item in source)
        {
            var oldRangeValue = manager.GetValue(rangeLangId);
            manager.Set(rangeLangId, ConvertToLangValue(item));

            try
            {
                // 计算 let 表达式的值
                var letValue = letClause.Expression.Run(manager);

                // 在作用域中设置 let 变量
                manager.Set(letLangId, letValue);

                // 保持原有元素
                result.Add(item);
            }
            finally
            {
                if (oldRangeValue is not null)
                    manager.Set(rangeLangId, oldRangeValue);
            }
        }

        return result;
    }

    /// <summary>
    /// 执行 join 子句（暂不实现，较复杂）
    /// </summary>
    private IEnumerable ExecuteJoin(JoinClause joinClause, IEnumerable source, string rangeVariable)
    {
        throw new NotImplementedException("join 子句暂未实现");
    }

    /// <summary>
    /// 执行查询延续（into）
    /// </summary>
    private IEnumerable ExecuteContinuation(QueryContinuation continuation, IEnumerable source)
    {
        // 设置延续变量
        var continuationId = new LangId(continuation.Variable);
        manager.Set(continuationId, ConvertToLangValue(source));

        // 执行延续后的查询体
        IEnumerable result = source;
        foreach (var clause in continuation.BodyClauses)
        {
            result = ExecuteBodyClause(clause, result, continuation.Variable);
        }

        // 执行终止子句
        result = ExecuteTerminationClause(continuation.TerminationClause, result, continuation.Variable);

        return result;
    }

    /// <summary>
    /// 将 Old8Lang 值转换为 IEnumerable
    /// </summary>
    private IEnumerable ConvertToEnumerable(LangValueType value)
    {
        if (value is ILangList langList)
        {
            return langList.GetItems();
        }

        if (value is ArrayLangValue arrayValue)
        {
            return arrayValue.Values;
        }

        if (value is IEnumerable enumerable)
        {
            return enumerable;
        }

        throw new InvalidOperationError(value, $"无法将类型 {value.GetType().Name} 转换为 IEnumerable");
    }

    /// <summary>
    /// 将对象转换为 Old8Lang 值
    /// </summary>
    private LangValueType ConvertToLangValue(object? obj)
    {
        return obj switch
        {
            null => new NullLangValue(),
            int i => new IntLangValue(i),
            double d => new DoubleLangValue(d),
            string s => new StringLangValue(s),
            bool b => new BoolLangValue(b),
            LangValueType langValue => langValue,
            _ => new IntLangValue() // 默认值
        };
    }

    /// <summary>
    /// 将 Old8Lang 值转换为对象
    /// </summary>
    private object? ConvertFromLangValue(LangValueType value)
    {
        return value switch
        {
            IntLangValue intVal => intVal.Value,
            DoubleLangValue doubleVal => doubleVal.Value,
            StringLangValue strVal => strVal.Value,
            BoolLangValue boolVal => boolVal.Value,
            NullLangValue => null,
            _ => value
        };
    }

    /// <summary>
    /// 将 IEnumerable 转换为 Old8Lang List
    /// </summary>
    private LangValueType ConvertToLangList(IEnumerable enumerable)
    {
        var list = new List<LangValueType>();

        foreach (var item in enumerable)
        {
            list.Add(ConvertToLangValue(item));
        }

        return new ListLangValue(list);
    }

    /// <summary>
    /// 判断值是否为真
    /// </summary>
    private bool IsTruthy(LangValueType value)
    {
        return value switch
        {
            BoolLangValue boolVal => boolVal.Value,
            NullLangValue => false,
            IntLangValue intVal => intVal.Value != 0,
            DoubleLangValue doubleVal => doubleVal.Value != 0.0,
            StringLangValue strVal => !string.IsNullOrEmpty(strVal.Value),
            _ => true
        };
    }
}