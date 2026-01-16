using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Intermediates;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// TupleLangValue类型的扩展方法类，提供元组操作功能
/// </summary>
[Serializable]
public static class TupleValueFuncStatic
{
    extension(TupleLangValue tuple)
    {
        /// <summary>
        /// 获取元组指定索引的元素
        /// </summary>
        public LangValueType Get(IntLangValue index)
        {
            return tuple.Get(index.Value);
        }

        /// <summary>
        /// 将元组中的所有元素连接成字符串
        /// </summary>
        public StringLangValue Join(StringLangValue separator)
        {
            var elements = tuple.GetItems();
            var separatorValue = separator.Value;

            var result = string.Join(separatorValue, elements.Select(e => e.ToDisplayString()));
            return new StringLangValue(result);
        }

        /// <summary>
        /// 检查元组是否包含指定元素
        /// </summary>
        public BoolLangValue Contains(LangValueType value)
        {
            var elements = tuple.GetItems();
            var contains = elements.Any(item => item.Equal(value));
            return new BoolLangValue(contains);
        }

        /// <summary>
        /// 查找元组中第一个满足条件的元素
        /// </summary>
        public LangValueType Find(FuncLangValue predicate)
        {
            var elements = tuple.GetItems();
            var manager = ExecutionContext.GetCurrentManager() ?? new VariateManager();

            foreach (var item in elements)
            {
                try
                {
                    var result = predicate.Run(manager, [item]);
                    if (result is BoolLangValue { Value: true })
                    {
                        return item;
                    }
                }
                catch
                {
                    // 忽略执行错误，继续查找
                }
            }

            return new NullLangValue();
        }

        /// <summary>
        /// 过滤元组中的元素
        /// </summary>
        public TupleLangValue Filter(FuncLangValue predicate)
        {
            var elements = tuple.GetItems();
            var filteredElements = new List<LangValueType>();
            var manager = ExecutionContext.GetCurrentManager() ?? new VariateManager();

            foreach (var item in elements)
            {
                try
                {
                    var result = predicate.Run(manager, [item]);
                    if (result is BoolLangValue { Value: true })
                    {
                        filteredElements.Add(item);
                    }
                }
                catch
                {
                    filteredElements.Add(item);
                }
            }

            return TupleLangValue.CreateTupleFromList(filteredElements);
        }

        /// <summary>
        /// 转换元组中的所有元素
        /// </summary>
        public TupleLangValue Map(FuncLangValue func)
        {
            var elements = tuple.GetItems();
            var transformedElements = new List<LangValueType>();
            var manager = ExecutionContext.GetCurrentManager() ?? new VariateManager();

            foreach (var item in elements)
            {
                try
                {
                    var result = func.Run(manager, [item]);
                    transformedElements.Add(result);
                }
                catch
                {
                    transformedElements.Add(item);
                }
            }

            return TupleLangValue.CreateTupleFromList(transformedElements);
        }

        /// <summary>
        /// 聚合元组元素
        /// </summary>
        public LangValueType Reduce(FuncLangValue reducer, LangValueType seed)
        {
            var elements = tuple.GetItems();
            var result = seed;
            var manager = ExecutionContext.GetCurrentManager() ?? new VariateManager();

            foreach (var item in elements)
            {
                try
                {
                    result = reducer.Run(manager, [result, item]);
                }
                catch
                {
                    // 忽略执行错误，继续排序
                }
            }

            return result;
        }

        /// <summary>
        /// 对元组中的每个元素执行操作
        /// </summary>
        public VoidLangValue ForEach(FuncLangValue action)
        {
            var elements = tuple.GetItems();
            var manager = ExecutionContext.GetCurrentManager() ?? new VariateManager();

            foreach (var item in elements)
            {
                try
                {
                    action.Run(manager, [item]);
                }
                catch
                {
                    // 忽略执行错误，继续排序
                }
            }

            return new VoidLangValue();
        }

        /// <summary>
        /// 排序元组元素
        /// </summary>
        public TupleLangValue Sort(FuncLangValue? comparer = null)
        {
            var elements = tuple.GetItems();

            if (comparer is not null)
            {
                var manager = ExecutionContext.GetCurrentManager() ?? new VariateManager();
                var sortedElements = elements.ToList();
                for (int i = 0; i < sortedElements.Count - 1; i++)
                {
                    for (int j = i + 1; j < sortedElements.Count; j++)
                    {
                        try
                        {
                            var result = comparer.Run(manager,
                                [sortedElements[i], sortedElements[j]]);
                            if (result is IntLangValue { Value: > 0 })
                            {
                                (sortedElements[i], sortedElements[j]) = (sortedElements[j], sortedElements[i]);
                            }
                        }
                        catch
                        {
                            // 忽略执行错误，继续排序
                        }
                    }
                }

                return TupleLangValue.CreateTupleFromList(sortedElements);
            }
            else
            {
                var sortedElements = elements.OrderBy(e => e.ToDisplayString()).ToList();
                return TupleLangValue.CreateTupleFromList(sortedElements);
            }
        }

        /// <summary>
        /// 反转元组元素顺序
        /// </summary>
        public TupleLangValue Reverse()
        {
            var elements = tuple.GetItems();
            var reversedElements = elements.ToList();
            reversedElements.Reverse();
            return TupleLangValue.CreateTupleFromList(reversedElements);
        }

        /// <summary>
        /// 连接两个元组
        /// </summary>
        public TupleLangValue Concat(TupleLangValue other)
        {
            var elements1 = tuple.GetItems();
            var elements2 = other.GetItems();
            var allElements = elements1.Concat(elements2).ToList();
            return TupleLangValue.CreateTupleFromList(allElements);
        }

        /// <summary>
        /// 获取元素在元组中的索引
        /// </summary>
        public IntLangValue IndexOf(LangValueType value)
        {
            var elements = tuple.GetItems().ToList();
            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].Equal(value))
                {
                    return new IntLangValue(i);
                }
            }

            return new IntLangValue(-1);
        }

        /// <summary>
        /// 获取元组的切片
        /// </summary>
        public TupleLangValue Slice(IntLangValue start, IntLangValue end)
        {
            // TupleLangValue.Slice 已经返回一个新的 TupleLangValue（带有正确填充的 ItemValues）
            // 只要我们确保 TupleLangValue.Slice 的实现是正确的
            return tuple.Slice(start.Value, end.Value) as TupleLangValue
                   ?? new TupleLangValue(new List<LangExpression>());
        }

        /// <summary>
        /// 从元素列表创建元组
        /// </summary>
        private static TupleLangValue CreateTupleFromList(List<LangValueType> elements)
        {
            var exprs = elements.Cast<LangExpression>().ToList();
            var newTuple = new TupleLangValue(exprs);
            newTuple.ItemValues.AddRange(elements); // 预填充运行结果
            return newTuple;
        }
    }
}