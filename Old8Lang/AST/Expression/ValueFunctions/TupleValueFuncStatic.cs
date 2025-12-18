using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;
using System.Linq;
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
        /// <param name="index">索引值</param>
        /// <returns>指定索引的元素</returns>
        public LangValueType Get(IntLangValue index)
        {
            return tuple.Get(index.Value);
        }

        /// <summary>
        /// 将元组中的所有元素连接成字符串
        /// </summary>
        /// <param name="separator">分隔符</param>
        /// <returns>连接后的字符串</returns>
        public StringLangValue Join(StringLangValue separator)
        {
            var elements = GetAllElements(tuple);
            var separatorValue = separator?.Value ?? "";

            var result = string.Join(separatorValue, elements.Select(e => e.ToDisplayString()));
            return new StringLangValue(result);
        }

        /// <summary>
        /// 检查元组是否包含指定元素
        /// </summary>
        /// <param name="value">要查找的元素</param>
        /// <returns>如果包含返回true，否则返回false</returns>
        public BoolLangValue Contains(LangValueType value)
        {
            var elements = GetAllElements(tuple);
            var contains = elements.Any(item => item.Equal(value));
            return new BoolLangValue(contains);
        }

        /// <summary>
        /// 查找元组中第一个满足条件的元素
        /// </summary>
        /// <param name="predicate">条件函数</param>
        /// <returns>第一个满足条件的元素，如果没有找到返回Null</returns>
        public LangValueType Find(FuncLangValue predicate)
        {
            var elements = GetAllElements(tuple);

            // 尝试获取当前的 VariateManager，如果没有则创建新的
            var manager = ExecutionContext.GetCurrentManager();
            if (manager == null)
            {
                manager = new VariateManager();
            }

            foreach (var item in elements)
            {
                try
                {
                    var result = predicate.Run(manager, new List<LangExpression> { item });
                    if (result is BoolLangValue boolResult && boolResult.Value)
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
        /// <param name="predicate">条件函数</param>
        /// <returns>包含满足条件元素的新元组</returns>
        public TupleLangValue Filter(FuncLangValue predicate)
        {
            var elements = GetAllElements(tuple);
            var filteredElements = new List<LangValueType>();

            // 尝试获取当前的 VariateManager，如果没有则创建新的
            var manager = ExecutionContext.GetCurrentManager();
            if (manager == null)
            {
                manager = new VariateManager();
            }

            foreach (var item in elements)
            {
                try
                {
                    var result = predicate.Run(manager, new List<LangExpression> { item });
                    if (result is BoolLangValue boolResult && boolResult.Value)
                    {
                        filteredElements.Add(item);
                    }
                }
                catch
                {
                    // 忽略执行错误，保留该项
                    filteredElements.Add(item);
                }
            }

            return CreateTupleFromList(filteredElements);
        }

        /// <summary>
        /// 转换元组中的所有元素
        /// </summary>
        /// <param name="func">转换函数</param>
        /// <returns>包含转换后元素的新元组</returns>
        public TupleLangValue Map(FuncLangValue func)
        {
            var elements = GetAllElements(tuple);
            var transformedElements = new List<LangValueType>();

            // 尝试获取当前的 VariateManager，如果没有则创建新的
            var manager = ExecutionContext.GetCurrentManager();
            if (manager == null)
            {
                manager = new VariateManager();
            }

            foreach (var item in elements)
            {
                try
                {
                    var result = func.Run(manager, new List<LangExpression> { item });
                    transformedElements.Add(result);
                }
                catch
                {
                    // 如果转换失败，保留原值
                    transformedElements.Add(item);
                }
            }

            return CreateTupleFromList(transformedElements);
        }

        /// <summary>
        /// 聚合元组元素
        /// </summary>
        /// <param name="reducer">聚合函数</param>
        /// <param name="seed">初始值</param>
        /// <returns>聚合结果</returns>
        public LangValueType Reduce(FuncLangValue reducer, LangValueType seed)
        {
            var elements = GetAllElements(tuple);
            var result = seed;

            // 尝试获取当前的 VariateManager，如果没有则创建新的
            var manager = ExecutionContext.GetCurrentManager();
            if (manager == null)
            {
                manager = new VariateManager();
            }

            foreach (var item in elements)
            {
                try
                {
                    result = reducer.Run(manager, new List<LangExpression> { result, item });
                }
                catch
                {
                    // 忽略执行错误，继续聚合
                }
            }

            return result;
        }

        /// <summary>
        /// 对元组中的每个元素执行操作
        /// </summary>
        /// <param name="action">要执行的操作函数</param>
        /// <returns>VoidLangValue，表示操作完成</returns>
        public VoidLangValue ForEach(FuncLangValue action)
        {
            var elements = GetAllElements(tuple);

            // 尝试获取当前的 VariateManager，如果没有则创建新的
            var manager = ExecutionContext.GetCurrentManager();
            if (manager == null)
            {
                manager = new VariateManager();
            }

            foreach (var item in elements)
            {
                try
                {
                    action.Run(manager, new List<LangExpression> { item });
                }
                catch
                {
                    // 忽略执行错误，继续处理下一项
                }
            }

            return new VoidLangValue();
        }

        /// <summary>
        /// 排序元组元素
        /// </summary>
        /// <param name="comparer">比较函数（可选）</param>
        /// <returns>排序后的新元组</returns>
        public TupleLangValue Sort(FuncLangValue? comparer = null)
        {
            var elements = GetAllElements(tuple);

            if (comparer != null)
            {
                // 尝试获取当前的 VariateManager，如果没有则创建新的
                var manager = ExecutionContext.GetCurrentManager();
                if (manager == null)
                {
                    manager = new VariateManager();
                }

                // 使用自定义比较器排序
                var sortedElements = elements.ToList();
                for (int i = 0; i < sortedElements.Count - 1; i++)
                {
                    for (int j = i + 1; j < sortedElements.Count; j++)
                    {
                        try
                        {
                            var result = comparer.Run(manager,
                                new List<LangExpression> { sortedElements[i], sortedElements[j] });
                            if (result is IntLangValue intResult && intResult.Value > 0)
                            {
                                (sortedElements[i], sortedElements[j]) = (sortedElements[j], sortedElements[i]);
                            }
                        }
                        catch
                        {
                            // 忽略错误，保持原顺序
                        }
                    }
                }

                return CreateTupleFromList(sortedElements);
            }
            else
            {
                // 默认排序（按字符串表示）
                var sortedElements = elements.OrderBy(e => e.ToDisplayString()).ToList();
                return CreateTupleFromList(sortedElements);
            }
        }

        /// <summary>
        /// 反转元组元素顺序
        /// </summary>
        /// <returns>反转后的新元组</returns>
        public TupleLangValue Reverse()
        {
            var elements = GetAllElements(tuple);
            var reversedElements = elements.ToList();
            reversedElements.Reverse(); // 就地反转
            return CreateTupleFromList(reversedElements);
        }

        /// <summary>
        /// 连接两个元组
        /// </summary>
        /// <param name="other">要连接的元组</param>
        /// <returns>连接后的新元组</returns>
        public TupleLangValue Concat(TupleLangValue other)
        {
            var elements1 = GetAllElements(tuple);
            var elements2 = GetAllElements(other);
            var allElements = elements1.Concat(elements2).ToList();
            return CreateTupleFromList(allElements);
        }

        /// <summary>
        /// 获取元素在元组中的索引
        /// </summary>
        /// <param name="value">要查找的元素</param>
        /// <returns>元素的索引，如果未找到返回-1</returns>
        public IntLangValue IndexOf(LangValueType value)
        {
            var elements = GetAllElements(tuple);
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
        /// <param name="start">起始索引</param>
        /// <param name="end">结束索引</param>
        /// <returns>切片后的新元组</returns>
        public TupleLangValue Slice(IntLangValue start, IntLangValue end)
        {
            return tuple.Slice(start.Value, end.Value) as TupleLangValue
                   ?? new TupleLangValue(new NullLangValue(), new NullLangValue());
        }

        /// <summary>
        /// 获取元组中的所有元素（包括嵌套元组的元素）
        /// </summary>
        /// <param name="sourceTuple">元组</param>
        /// <returns>所有元素的列表</returns>
        private static List<LangValueType> GetAllElements(TupleLangValue sourceTuple)
        {
            var elements = new List<LangValueType>();
            CollectElements(sourceTuple, elements);
            return elements;
        }

        /// <summary>
        /// 递归收集元组中的所有元素
        /// </summary>
        /// <param name="current">当前元组或元素</param>
        /// <param name="elements">元素列表</param>
        private static void CollectElements(LangValueType current, List<LangValueType> elements)
        {
            if (current is TupleLangValue currentTuple)
            {
                // 如果是元组，递归收集其元素
                CollectElements(currentTuple.Value.Item1, elements);
                CollectElements(currentTuple.Value.Item2, elements);
            }
            else
            {
                // 如果是普通元素，添加到列表
                elements.Add(current);
            }
        }

        /// <summary>
        /// 从元素列表创建元组
        /// </summary>
        /// <param name="elements">元素列表</param>
        /// <returns>对应的元组</returns>
        private static TupleLangValue CreateTupleFromList(List<LangValueType> elements)
        {
            if (elements.Count == 0)
            {
                return new TupleLangValue(new NullLangValue(), new NullLangValue());
            }

            if (elements.Count == 1)
            {
                return new TupleLangValue(elements[0], new NullLangValue());
            }

            var result = new TupleLangValue(elements[0], elements[1]);
            for (int i = 2; i < elements.Count; i++)
            {
                result = new TupleLangValue(result, elements[i]);
            }

            return result;
        }
    }
}