using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Interpreter;
using Old8Lang.Error;
using System.Collections;

using Old8Lang.AST.Visitor;

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
        var sourceValue = linqExpr.FromClause.DataSource.Accept(RuntimeVisitors.CreateInterpreter(manager));
        var dataSource = ConvertToEnumerable(sourceValue);

        manager.AddChildren();
        try
        {
            return ExecuteQueryInScope(linqExpr, dataSource);
        }
        finally
        {
            manager.RemoveChildren();
        }
    }

    private LangValueType ExecuteQueryInScope(LinqExpression linqExpr, IEnumerable dataSource)
    {

        // 检查是否有 OrderBy 子句
        var hasOrderBy = linqExpr.BodyClauses.Any(c => c is OrderByClause);

        // 检查是否有 Join 子句
        var hasJoin = linqExpr.BodyClauses.Any(c => c is JoinClause);

        // 如果有 Join 子句，使用特殊的处理流程
        if (hasJoin)
        {
            return ExecuteQueryWithJoin(linqExpr, dataSource);
        }

        // 使用统一的迭代器处理所有子句
        var intermediateResults = new List<(object? item, Dictionary<string, LangValueType> letVariables)>();
        var result = new List<object?>();
        var groups = new List<(LangValueType Key, List<LangValueType> Values)>();
        var rangeLangId = new LangId(linqExpr.FromClause.RangeVariable);

        foreach (var item in dataSource)
        {
            // 设置范围变量
            var oldRangeValue = manager.GetValue(rangeLangId);
            manager.SetLocal(rangeLangId, ConvertToLangValue(item));

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
                        var letValue = letClause.Expression.Accept(RuntimeVisitors.CreateInterpreter(manager));
                        var letId = new LangId(letClause.Variable);
                        manager.SetLocal(letId, letValue);
                        letVariables[letClause.Variable] = letValue;
                    }
                    else if (clause is WhereClause whereClause)
                    {
                        // 检查 where 条件
                        var conditionResult = whereClause.Condition.Accept(RuntimeVisitors.CreateInterpreter(manager));
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
                            var projectedValue = selectClause.Projection.Accept(RuntimeVisitors.CreateInterpreter(manager));
                            result.Add(ConvertFromLangValue(projectedValue));
                        }
                        else if (linqExpr.TerminationClause is GroupByClause groupBy)
                        {
                            AddGroupValue(groups, groupBy);
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
                manager.SetLocal(rangeLangId, ConvertToLangValue(item));

                try
                {
                    // 恢复 let 变量
                    foreach (var (varName, varValue) in letVariables)
                    {
                        manager.SetLocal(new LangId(varName), varValue);
                    }

                    // 执行 select
                    if (linqExpr.TerminationClause is SelectClause selectClause)
                    {
                        var projectedValue = selectClause.Projection.Accept(RuntimeVisitors.CreateInterpreter(manager));
                        result.Add(ConvertFromLangValue(projectedValue));
                    }
                    else if (linqExpr.TerminationClause is GroupByClause groupBy)
                    {
                        AddGroupValue(groups, groupBy);
                    }
                }
                finally
                {
                    if (oldRangeValue is not null)
                        manager.Set(rangeLangId, oldRangeValue);
                }
            }
        }

        if (linqExpr.TerminationClause is GroupByClause)
            result = CreateGroupResults(groups);

        // 处理查询延续（into）
        if (linqExpr.Continuation is not null)
        {
            result = ExecuteContinuation(linqExpr.Continuation, result).Cast<object?>().ToList();
        }

        // 将结果转换为 Old8Lang 的 List
        return ConvertToLangList(result);
    }

    /// <summary>
    /// 执行包含 Join 子句的 LINQ 查询
    /// </summary>
    internal LangValueType ExecuteQueryWithJoin(LinqExpression linqExpr, IEnumerable dataSource)
    {
        var result = new List<object?>();
        var groups = new List<(LangValueType Key, List<LangValueType> Values)>();
        var allRows = new List<Dictionary<string, object?>>();

        // 将数据源转换为列表以便多次遍历
        var sourceList = dataSource.Cast<object?>().ToList();

        // 处理每个外部元素
        foreach (var outerItem in sourceList)
        {
            manager.AddChildren();
            try
            {
                var currentItems = new List<Dictionary<string, object?>>
                {
                    new() { [linqExpr.FromClause.RangeVariable] = outerItem }
                };

                foreach (var clause in linqExpr.BodyClauses)
                {
                    if (currentItems.Count == 0)
                        break;

                    if (clause is JoinClause joinClause)
                    {
                        var newItems = new List<Dictionary<string, object?>>();
                        var innerRangeLangId = new LangId(joinClause.RangeVariable);

                        foreach (var currentItem in currentItems)
                        {
                            BindRowVariables(currentItem);

                            // 获取内部数据源与外部键（按当前行求值，支持前置 let 和多层链式 join）
                            var innerSourceValue = joinClause.InnerDataSource.Accept(RuntimeVisitors.CreateInterpreter(manager));
                            var innerSource = ConvertToEnumerable(innerSourceValue).Cast<object?>().ToList();
                            var outerKeyValue = joinClause.OuterKeyExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
                            var outerKey = ConvertFromLangValue(outerKeyValue);

                            if (joinClause.IsGroupJoin && joinClause.GroupVariable is not null)
                            {
                                var matchingInnerItems = new List<LangValueType>();

                                foreach (var innerItem in innerSource)
                                {
                                    var oldInnerValue = manager.GetValue(innerRangeLangId);
                                    manager.SetLocal(innerRangeLangId, ConvertToLangValue(innerItem));
                                    try
                                    {
                                        var innerKeyValue = joinClause.InnerKeyExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
                                        var innerKey = ConvertFromLangValue(innerKeyValue);
                                        if (KeysEqual(outerKey, innerKey))
                                        {
                                            matchingInnerItems.Add(ConvertToLangValue(innerItem));
                                        }
                                    }
                                    finally
                                    {
                                        if (oldInnerValue is not null)
                                            manager.SetLocal(innerRangeLangId, oldInnerValue);
                                    }
                                }

                                var newItem = new Dictionary<string, object?>(currentItem)
                                {
                                    [joinClause.GroupVariable] = matchingInnerItems
                                };
                                newItems.Add(newItem);
                            }
                            else
                            {
                                foreach (var innerItem in innerSource)
                                {
                                    var oldInnerValue = manager.GetValue(innerRangeLangId);
                                    manager.SetLocal(innerRangeLangId, ConvertToLangValue(innerItem));
                                    try
                                    {
                                        var innerKeyValue = joinClause.InnerKeyExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
                                        var innerKey = ConvertFromLangValue(innerKeyValue);
                                        if (KeysEqual(outerKey, innerKey))
                                        {
                                            var newItem = new Dictionary<string, object?>(currentItem)
                                            {
                                                [joinClause.RangeVariable] = innerItem
                                            };
                                            newItems.Add(newItem);
                                        }
                                    }
                                    finally
                                    {
                                        if (oldInnerValue is not null)
                                            manager.SetLocal(innerRangeLangId, oldInnerValue);
                                    }
                                }
                            }
                        }

                        currentItems = newItems;
                    }
                    else if (clause is LetClause letClause)
                    {
                        foreach (var currentItem in currentItems)
                        {
                            BindRowVariables(currentItem);
                            var letValue = letClause.Expression.Accept(RuntimeVisitors.CreateInterpreter(manager));
                            currentItem[letClause.Variable] = letValue;
                        }
                    }
                    else if (clause is WhereClause whereClause)
                    {
                        var filteredItems = new List<Dictionary<string, object?>>();
                        foreach (var currentItem in currentItems)
                        {
                            BindRowVariables(currentItem);
                            var conditionResult = whereClause.Condition.Accept(RuntimeVisitors.CreateInterpreter(manager));
                            if (IsTruthy(conditionResult))
                            {
                                filteredItems.Add(currentItem);
                            }
                        }
                        currentItems = filteredItems;
                    }
                }

                allRows.AddRange(currentItems);
            }
            finally
            {
                manager.RemoveChildren();
            }
        }

        // 处理 OrderBy 子句（如果有）
        var orderByClause = linqExpr.BodyClauses.OfType<OrderByClause>().LastOrDefault();
        if (orderByClause is not null && orderByClause.Orderings.Count > 0 && allRows.Count > 1)
        {
            var keyed = new List<(Dictionary<string, object?> Row, object?[] Keys, int Index)>(allRows.Count);
            manager.AddChildren();
            try
            {
                for (int idx = 0; idx < allRows.Count; idx++)
                {
                    var row = allRows[idx];
                    BindRowVariables(row);
                    var keys = new object?[orderByClause.Orderings.Count];
                    for (int k = 0; k < orderByClause.Orderings.Count; k++)
                    {
                        var keyVal = orderByClause.Orderings[k].KeyExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
                        keys[k] = ConvertFromLangValue(keyVal);
                    }
                    keyed.Add((row, keys, idx));
                }
            }
            finally
            {
                manager.RemoveChildren();
            }

            keyed.Sort((left, right) =>
            {
                for (int k = 0; k < orderByClause.Orderings.Count; k++)
                {
                    int cmp = CompareSortKeys(left.Keys[k], right.Keys[k]);
                    if (cmp != 0)
                        return orderByClause.Orderings[k].IsAscending ? cmp : -cmp;
                }
                return left.Index.CompareTo(right.Index);
            });
            allRows = keyed.Select(entry => entry.Row).ToList();
        }

        // 执行终止子句（select / group by）
        if (allRows.Count > 0)
        {
            manager.AddChildren();
            try
            {
                foreach (var row in allRows)
                {
                    BindRowVariables(row);
                    if (linqExpr.TerminationClause is SelectClause selectClause)
                    {
                        var projectedValue = selectClause.Projection.Accept(RuntimeVisitors.CreateInterpreter(manager));
                        result.Add(ConvertFromLangValue(projectedValue));
                    }
                    else if (linqExpr.TerminationClause is GroupByClause groupBy)
                    {
                        AddGroupValue(groups, groupBy);
                    }
                }
            }
            finally
            {
                manager.RemoveChildren();
            }
        }

        if (linqExpr.TerminationClause is GroupByClause)
            result = CreateGroupResults(groups);

        // 处理查询延续（into）
        if (linqExpr.Continuation is not null)
        {
            result = ExecuteContinuation(linqExpr.Continuation, result).Cast<object?>().ToList();
        }

        return ConvertToLangList(result);
    }

    private void BindRowVariables(Dictionary<string, object?> row)
    {
        foreach (var (varName, varValue) in row)
        {
            if (varValue is List<LangValueType> listValue)
            {
                manager.SetLocal(new LangId(varName), new ListLangValue(listValue));
            }
            else
            {
                manager.SetLocal(new LangId(varName), ConvertToLangValue(varValue));
            }
        }
    }

    private static readonly IComparer<object?> SortKeyComparer = Comparer<object?>.Create(CompareSortKeys);

    private static int CompareSortKeys(object? a, object? b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        if (a is string leftStr && b is string rightStr)
            return string.CompareOrdinal(leftStr, rightStr);

        if (a is int or long or double or float or decimal &&
            b is int or long or double or float or decimal)
            return Convert.ToDouble(a).CompareTo(Convert.ToDouble(b));

        if (a is IComparable comparableA)
        {
            try
            {
                return comparableA.CompareTo(b);
            }
            catch
            {
                // 回退到字符串序比较
            }
        }

        return string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal);
    }

    private void AddGroupValue(List<(LangValueType Key, List<LangValueType> Values)> groups, GroupByClause clause)
    {
        var visitor = RuntimeVisitors.CreateInterpreter(manager);
        var key = clause.KeyExpression.Accept(visitor);
        var element = clause.ElementExpression.Accept(visitor);
        var index = groups.FindIndex(group => group.Key.Equal(key));
        if (index < 0)
        {
            groups.Add((key, [element]));
            return;
        }
        groups[index].Values.Add(element);
    }

    private List<object?> CreateGroupResults(List<(LangValueType Key, List<LangValueType> Values)> groups)
    {
        var results = new List<object?>();
        foreach (var (key, values) in groups)
        {
            var group = new DictionaryLangValue([
                new KeyValuePair<LangExpression, LangExpression>(new StringLangValue("Key"), key),
                new KeyValuePair<LangExpression, LangExpression>(new StringLangValue("Values"), new ListLangValue(values))
            ]);
            group.Accept(RuntimeVisitors.CreateInterpreter(manager));
            results.Add(group);
        }
        return results;
    }

    /// <summary>
    /// 在中间结果上执行 OrderBy（包含 let 变量）
    /// </summary>
    internal List<(object? item, Dictionary<string, LangValueType> letVariables)> ExecuteOrderByOnIntermediateResults(
        OrderByClause orderByClause,
        List<(object? item, Dictionary<string, LangValueType> letVariables)> intermediateResults,
        string rangeVariable)
    {
        var rangeLangId = new LangId(rangeVariable);

        if (orderByClause.Orderings.Count == 0)
            return intermediateResults;

        object? EvalKey((object? item, Dictionary<string, LangValueType> letVariables) entry, OrderingItem ordering)
        {
            var (item, letVariables) = entry;
            var oldValue = manager.GetValue(rangeLangId);
            manager.SetLocal(rangeLangId, ConvertToLangValue(item));
            try
            {
                foreach (var (varName, varValue) in letVariables)
                {
                    manager.SetLocal(new LangId(varName), varValue);
                }

                var keyValue = ordering.KeyExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
                return ConvertFromLangValue(keyValue);
            }
            finally
            {
                if (oldValue is not null)
                    manager.SetLocal(rangeLangId, oldValue);
            }
        }

        // 第一个排序键
        var firstOrdering = orderByClause.Orderings[0];
        IOrderedEnumerable<(object? item, Dictionary<string, LangValueType> letVariables)> orderedList =
            firstOrdering.IsAscending
                ? intermediateResults.OrderBy(entry => EvalKey(entry, firstOrdering), SortKeyComparer)
                : intermediateResults.OrderByDescending(entry => EvalKey(entry, firstOrdering), SortKeyComparer);

        // 后续排序键
        for (int i = 1; i < orderByClause.Orderings.Count; i++)
        {
            var ordering = orderByClause.Orderings[i];
            orderedList = ordering.IsAscending
                ? orderedList.ThenBy(entry => EvalKey(entry, ordering), SortKeyComparer)
                : orderedList.ThenByDescending(entry => EvalKey(entry, ordering), SortKeyComparer);
        }

        return orderedList.ToList();
    }

    /// <summary>
    /// 执行 from 子句
    /// </summary>
    internal IEnumerable ExecuteFromClause(FromClause fromClause)
    {
        var sourceValue = fromClause.DataSource.Accept(RuntimeVisitors.CreateInterpreter(manager));
        return ConvertToEnumerable(sourceValue);
    }

    /// <summary>
    /// 执行查询体子句
    /// </summary>
    internal IEnumerable ExecuteBodyClause(LinqClause clause, IEnumerable source, string rangeVariable)
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
    internal IEnumerable ExecuteTerminationClause(LinqClause clause, IEnumerable source, string rangeVariable)
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
    internal IEnumerable ExecuteWhere(WhereClause whereClause, IEnumerable source, string rangeVariable)
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
                var conditionResult = whereClause.Condition.Accept(RuntimeVisitors.CreateInterpreter(manager));

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
    internal IEnumerable ExecuteSelect(SelectClause selectClause, IEnumerable source, string rangeVariable)
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
                var projectedValue = selectClause.Projection.Accept(RuntimeVisitors.CreateInterpreter(manager));
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
    internal IEnumerable ExecuteOrderBy(OrderByClause orderByClause, IEnumerable source, string rangeVariable)
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
                    var keyValue = firstOrdering.KeyExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
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
                    var keyValue = firstOrdering.KeyExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
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
                        var keyValue = ordering.KeyExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
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
                        var keyValue = ordering.KeyExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
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
    internal IEnumerable ExecuteGroupBy(GroupByClause groupByClause, IEnumerable source, string rangeVariable)
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
                var keyValue = groupByClause.KeyExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
                var key = ConvertFromLangValue(keyValue);

                // 计算分组元素
                var elementValue = groupByClause.ElementExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
                var element = ConvertFromLangValue(elementValue);

                // 添加到分组
                if (key != null && !groups.ContainsKey(key))
                    groups[key] = [];

                if (key != null) groups[key].Add(element);
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
    internal IEnumerable ExecuteLet(LetClause letClause, IEnumerable source, string rangeVariable)
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
                var letValue = letClause.Expression.Accept(RuntimeVisitors.CreateInterpreter(manager));

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
    /// 执行 join 子句
    /// 支持 inner join 和 group join 两种模式
    /// </summary>
    internal IEnumerable ExecuteJoin(JoinClause joinClause, IEnumerable source, string rangeVariable)
    {
        var result = new List<object?>();
        var rangeLangId = new LangId(rangeVariable);
        var innerRangeLangId = new LangId(joinClause.RangeVariable);

        // 获取内部数据源
        var innerSourceValue = joinClause.InnerDataSource.Accept(RuntimeVisitors.CreateInterpreter(manager));
        var innerSource = ConvertToEnumerable(innerSourceValue).Cast<object?>().ToList();

        // 如果是 group join（join ... into groupVar）
        if (joinClause.IsGroupJoin && joinClause.GroupVariable is not null)
        {
            var groupLangId = new LangId(joinClause.GroupVariable);

            foreach (var outerItem in source)
            {
                // 设置外部范围变量
                var oldOuterValue = manager.GetValue(rangeLangId);
                manager.Set(rangeLangId, ConvertToLangValue(outerItem));

                try
                {
                    // 计算外部键
                    var outerKeyValue = joinClause.OuterKeyExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
                    var outerKey = ConvertFromLangValue(outerKeyValue);

                    // 查找所有匹配的内部元素
                    var matchingInnerItems = new List<LangValueType>();

                    foreach (var innerItem in innerSource)
                    {
                        // 设置内部范围变量
                        var oldInnerValue = manager.GetValue(innerRangeLangId);
                        manager.Set(innerRangeLangId, ConvertToLangValue(innerItem));

                        try
                        {
                            // 计算内部键
                            var innerKeyValue = joinClause.InnerKeyExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
                            var innerKey = ConvertFromLangValue(innerKeyValue);

                            // 比较键是否相等
                            if (KeysEqual(outerKey, innerKey))
                            {
                                matchingInnerItems.Add(ConvertToLangValue(innerItem));
                            }
                        }
                        finally
                        {
                            if (oldInnerValue is not null)
                                manager.Set(innerRangeLangId, oldInnerValue);
                        }
                    }

                    // 设置分组变量（包含所有匹配的内部元素）
                    manager.Set(groupLangId, new ListLangValue(matchingInnerItems));

                    // 将外部元素添加到结果（分组变量已设置，后续子句可以访问）
                    result.Add(outerItem);
                }
                finally
                {
                    if (oldOuterValue is not null)
                        manager.Set(rangeLangId, oldOuterValue);
                }
            }
        }
        else
        {
            // 普通 inner join
            foreach (var outerItem in source)
            {
                // 设置外部范围变量
                var oldOuterValue = manager.GetValue(rangeLangId);
                manager.Set(rangeLangId, ConvertToLangValue(outerItem));

                try
                {
                    // 计算外部键
                    var outerKeyValue = joinClause.OuterKeyExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
                    var outerKey = ConvertFromLangValue(outerKeyValue);

                    foreach (var innerItem in innerSource)
                    {
                        // 设置内部范围变量
                        var oldInnerValue = manager.GetValue(innerRangeLangId);
                        manager.Set(innerRangeLangId, ConvertToLangValue(innerItem));

                        try
                        {
                            // 计算内部键
                            var innerKeyValue = joinClause.InnerKeyExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
                            var innerKey = ConvertFromLangValue(innerKeyValue);

                            // 比较键是否相等
                            if (KeysEqual(outerKey, innerKey))
                            {
                                // 创建包含两个元素的元组作为结果
                                var joinedItem = new Dictionary<string, object?>
                                {
                                    [rangeVariable] = outerItem,
                                    [joinClause.RangeVariable] = innerItem
                                };
                                result.Add(joinedItem);
                            }
                        }
                        finally
                        {
                            if (oldInnerValue is not null)
                                manager.Set(innerRangeLangId, oldInnerValue);
                        }
                    }
                }
                finally
                {
                    if (oldOuterValue is not null)
                        manager.Set(rangeLangId, oldOuterValue);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 比较两个键是否相等
    /// </summary>
    internal static bool KeysEqual(object? key1, object? key2)
    {
        if (key1 is null && key2 is null)
            return true;
        if (key1 is null || key2 is null)
            return false;

        // 处理 LangValueType 的比较
        if (key1 is LangValueType lv1 && key2 is LangValueType lv2)
            return lv1.Equal(lv2);

        // 处理基本类型的比较
        return key1.Equals(key2);
    }

    /// <summary>
    /// 执行查询延续（into）
    /// </summary>
    internal IEnumerable ExecuteContinuation(QueryContinuation continuation, IEnumerable source)
    {
        // 延续是以先前结果为输入的新查询；复用逐元素 let/where/select 管线，
        // 避免 let 变量只保留最后一次求值。
        var input = ConvertToLangList(source);
        var query = new LinqExpression(new FromClause(continuation.Variable, input),
            continuation.BodyClauses, continuation.TerminationClause);
        var result = ExecuteQuery(query);
        return ConvertToEnumerable(result);
    }

    /// <summary>
    /// 将 Old8Lang 值转换为 IEnumerable
    /// </summary>
    internal IEnumerable ConvertToEnumerable(LangValueType value)
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
    internal LangValueType ConvertToLangValue(object? obj)
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
    internal object? ConvertFromLangValue(LangValueType value)
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
    internal LangValueType ConvertToLangList(IEnumerable enumerable)
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
    internal bool IsTruthy(LangValueType value)
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
