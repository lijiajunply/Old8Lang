using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// ListLangValue类型的扩展方法类，提供列表操作功能
/// </summary>
[Serializable]
public static class ListValueFuncStatic
{
    /// <param name="langValue">字符串数组</param>
    extension(ListLangValue langValue)
    {
        /// <summary>
        /// 向列表中添加元素
        /// </summary>
        /// <param name="langValueType">要添加的元素</param>
        /// <returns>添加的元素</returns>
        public LangValueType Add(LangValueType langValueType)
        {
            langValue.Values.Add(langValueType);
            return langValueType;
        }

        /// <summary>
        /// 从列表中移除指定元素
        /// </summary>
        /// <param name="num">要移除的元素</param>
        /// <returns>被移除的元素</returns>
        /// <exception cref="InvalidOperationError">当元素不存在时抛出</exception>
        public LangValueType Remove(LangValueType num)
        {
            for (var i = 0; i < langValue.Values.Count; i++)
            {
                if (!langValue.Values[i].Equal(num)) continue;
                var a = langValue.Values[i];
                langValue.Values.RemoveAt(i);
                return a;
            }

            throw new InvalidOperationError(langValue, "找不到要移除的元素");
        }

        /// <summary>
        /// 根据索引从列表中移除元素
        /// </summary>
        /// <param name="num">要移除的元素索引</param>
        /// <returns>被移除的元素</returns>
        public LangValueType RemoveAt(IntLangValue num)
        {
            var a = langValue.Values[num.Value];
            langValue.Values.RemoveAt(num.Value);
            return a;
        }

        /// <summary>
        /// 将另一个列表的所有元素添加到当前列表
        /// </summary>
        /// <param name="otherLangValue">要添加的列表</param>
        /// <returns>VoidLangValue，表示操作完成</returns>
        public VoidLangValue AddList(ListLangValue otherLangValue)
        {
            langValue.Values.AddRange(otherLangValue.Values);
            return new VoidLangValue();
        }

        /// <summary>
        /// 返回列表元素数量
        /// </summary>
        /// <returns>包含元素数量的IntLangValue</returns>
        public IntLangValue Count()
        {
            return new IntLangValue(langValue.Values.Count);
        }

        /// <summary>
        /// 对列表进行排序（默认使用快速排序）
        /// </summary>
        /// <returns>排序后的新列表</returns>
        public ListLangValue Sort()
        {
            // 创建新列表副本以避免修改原列表
            var sortedValues = new List<LangValueType>(langValue.Values);
            QuickSort(sortedValues, 0, sortedValues.Count - 1);
            return new ListLangValue(sortedValues);
        }

        /// <summary>
        /// 使用自定义比较函数对列表进行排序
        /// </summary>
        /// <param name="comparer">比较函数，接受两个元素，返回负数（a&lt;b）、0（a==b）或正数（a>b）</param>
        /// <returns>排序后的新列表</returns>
        public ListLangValue Sort(FuncLangValue comparer)
        {
            // 创建新列表副本以避免修改原列表
            var sortedValues = new List<LangValueType>(langValue.Values);
            QuickSortWithComparer(sortedValues, 0, sortedValues.Count - 1, comparer);
            return new ListLangValue(sortedValues);
        }

        /// <summary>
        /// 使用快速排序算法对列表进行排序
        /// </summary>
        /// <returns>排序后的新列表</returns>
        public ListLangValue QuickSort()
        {
            var sortedValues = new List<LangValueType>(langValue.Values);
            QuickSort(sortedValues, 0, sortedValues.Count - 1);
            return new ListLangValue(sortedValues);
        }

        /// <summary>
        /// 使用归并排序算法对列表进行排序
        /// </summary>
        /// <returns>排序后的新列表</returns>
        public ListLangValue MergeSort()
        {
            var sortedValues = new List<LangValueType>(langValue.Values);
            MergeSortImpl(sortedValues, 0, sortedValues.Count - 1);
            return new ListLangValue(sortedValues);
        }

        /// <summary>
        /// 使用冒泡排序算法对列表进行排序
        /// </summary>
        /// <returns>排序后的新列表</returns>
        public ListLangValue BubbleSort()
        {
            var sortedValues = new List<LangValueType>(langValue.Values);
            BubbleSortImpl(sortedValues);
            return new ListLangValue(sortedValues);
        }

        /// <summary>
        /// 使用选择排序算法对列表进行排序
        /// </summary>
        /// <returns>排序后的新列表</returns>
        public ListLangValue SelectionSort()
        {
            var sortedValues = new List<LangValueType>(langValue.Values);
            SelectionSortImpl(sortedValues);
            return new ListLangValue(sortedValues);
        }

        /// <summary>
        /// 使用插入排序算法对列表进行排序
        /// </summary>
        /// <returns>排序后的新列表</returns>
        public ListLangValue InsertionSort()
        {
            var sortedValues = new List<LangValueType>(langValue.Values);
            InsertionSortImpl(sortedValues);
            return new ListLangValue(sortedValues);
        }

        /// <summary>
        /// 使用堆排序算法对列表进行排序
        /// </summary>
        /// <returns>排序后的新列表</returns>
        public ListLangValue HeapSort()
        {
            var sortedValues = new List<LangValueType>(langValue.Values);
            HeapSortImpl(sortedValues);
            return new ListLangValue(sortedValues);
        }

        /// <summary>
        /// 检查列表是否已排序
        /// </summary>
        /// <returns>如果列表已排序返回true，否则返回false</returns>
        public BoolLangValue IsSorted()
        {
            for (int i = 1; i < langValue.Values.Count; i++)
            {
                if (langValue.Values[i].Less(langValue.Values[i - 1]))
                {
                    return new BoolLangValue();
                }
            }

            return new BoolLangValue(true);
        }

        /// <summary>
        /// 使用谓词函数过滤列表元素
        /// </summary>
        /// <param name="predicate">谓词函数，返回布尔值</param>
        /// <returns>包含满足条件元素的新列表</returns>
        public ListLangValue Filter(FuncLangValue predicate)
        {
            var filtered = new List<LangValueType>();
            foreach (var item in langValue.Values)
            {
                // 创建临时变量管理器
                var manager = new VariateManager();

                // 执行谓词函数
                var result = predicate.Run(manager, [item]);

                // 如果结果为真，则保留该元素
                if (result is BoolLangValue { Value: true })
                {
                    filtered.Add(item);
                }
            }

            return new ListLangValue(filtered);
        }

        /// <summary>
        /// 使用转换函数映射列表元素
        /// </summary>
        /// <param name="transform">转换函数，将元素转换为新值</param>
        /// <returns>包含转换后元素的新列表</returns>
        public ListLangValue Map(FuncLangValue transform)
        {
            var mapped = new List<LangValueType>();
            foreach (var item in langValue.Values)
            {
                // 创建临时变量管理器
                var manager = new VariateManager();

                // 执行转换函数
                var result = transform.Run(manager, [item]);
                mapped.Add(result);
            }

            return new ListLangValue(mapped);
        }

        /// <summary>
        /// 使用归约函数将列表元素归约为单个值
        /// </summary>
        /// <param name="reducer">归约函数，接受累加器和当前元素，返回新的累加器值</param>
        /// <param name="initialValue">初始累加器值</param>
        /// <returns>归约后的结果值</returns>
        public LangValueType Reduce(FuncLangValue reducer, LangValueType initialValue)
        {
            var accumulator = initialValue;
            foreach (var item in langValue.Values)
            {
                // 创建临时变量管理器
                var manager = new VariateManager();

                // 执行归约函数
                accumulator = reducer.Run(manager, [accumulator, item]);
            }

            return accumulator;
        }

        /// <summary>
        /// 反转列表元素顺序
        /// </summary>
        /// <returns>反转后的新列表</returns>
        public ListLangValue Reverse()
        {
            // 创建新列表副本以避免修改原列表
            var reversedValues = new List<LangValueType>(langValue.Values);
            reversedValues.Reverse();
            return new ListLangValue(reversedValues);
        }

        /// <summary>
        /// 检查列表是否包含指定元素
        /// </summary>
        /// <param name="element">要检查的元素</param>
        /// <returns>包含检查结果的BoolLangValue</returns>
        public BoolLangValue Contains(LangValueType element)
        {
            return new BoolLangValue(langValue.Values.Any(item => item.Equal(element)));
        }

        /// <summary>
        /// 查找元素在列表中第一次出现的索引
        /// </summary>
        /// <param name="element">要查找的元素</param>
        /// <returns>包含索引的IntLangValue，未找到返回-1</returns>
        public IntLangValue IndexOf(LangValueType element)
        {
            for (var i = 0; i < langValue.Values.Count; i++)
            {
                if (langValue.Values[i].Equal(element))
                {
                    return new IntLangValue(i);
                }
            }

            return new IntLangValue(-1); // 未找到返回-1
        }

        /// <summary>
        /// 连接两个列表，返回包含所有元素的新列表
        /// </summary>
        /// <param name="otherList">要连接的另一个列表</param>
        /// <returns>包含两个列表所有元素的新列表</returns>
        public ListLangValue Concat(ListLangValue otherList)
        {
            var result = new List<LangValueType>(langValue.Values);
            result.AddRange(otherList.Values);
            return new ListLangValue(result);
        }

        /// <summary>
        /// 对列表元素进行聚合操作
        /// </summary>
        /// <param name="accumulator">聚合函数，接受累加器和当前元素，返回新的累加器值</param>
        /// <returns>聚合后的结果值</returns>
        public LangValueType Aggregate(FuncLangValue accumulator)
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "无法对空列表进行聚合操作");
            }

            var result = langValue.Values[0];
            for (var i = 1; i < langValue.Values.Count; i++)
            {
                var manager = new VariateManager();

                result = accumulator.Run(manager, [result, langValue.Values[i]]);
            }

            return result;
        }

        /// <summary>
        /// 对列表元素进行聚合操作，使用指定的初始值
        /// </summary>
        /// <param name="accumulator">聚合函数，接受累加器和当前元素，返回新的累加器值</param>
        /// <param name="seed">聚合的初始值</param>
        /// <returns>聚合后的结果值</returns>
        public LangValueType Aggregate(FuncLangValue accumulator, LangValueType seed)
        {
            var result = seed;
            foreach (var t in langValue.Values)
            {
                var manager = new VariateManager();

                result = accumulator.Run(manager, [result, t]);
            }

            return result;
        }

        /// <summary>
        /// 查找列表中第一个满足条件的元素
        /// </summary>
        /// <param name="predicate">谓词函数，返回布尔值</param>
        /// <returns>第一个满足条件的元素，如果找不到则返回Null</returns>
        public LangValueType Find(FuncLangValue predicate)
        {
            foreach (var item in langValue.Values)
            {
                var manager = new VariateManager();

                var result = predicate.Run(manager, [item]);
                if (result is BoolLangValue { Value: true })
                {
                    return item;
                }
            }

            return new NullLangValue();
        }

        /// <summary>
        /// 跳过列表的前n个元素，返回剩余元素
        /// </summary>
        /// <param name="count">要跳过的元素数量</param>
        /// <returns>包含剩余元素的新列表</returns>
        public ListLangValue Skip(IntLangValue count)
        {
            var skipCount = Math.Max(0, Math.Min(count.Value, langValue.Values.Count));
            var result = langValue.Values.Skip(skipCount).ToList();
            return new ListLangValue(result);
        }

        /// <summary>
        /// 检查列表中是否有任何元素满足条件
        /// </summary>
        /// <param name="predicate">谓词函数，返回布尔值</param>
        /// <returns>如果有元素满足条件则返回true，否则返回false</returns>
        public BoolLangValue Any(FuncLangValue predicate)
        {
            foreach (var item in langValue.Values)
            {
                var manager = new VariateManager();

                var result = predicate.Run(manager, [item]);
                if (result is BoolLangValue { Value: true })
                {
                    return new BoolLangValue(true);
                }
            }

            return new BoolLangValue();
        }

        /// <summary>
        /// 在指定索引处插入元素
        /// </summary>
        /// <param name="index">插入位置的索引</param>
        /// <param name="element">要插入的元素</param>
        /// <returns>VoidLangValue，表示操作完成</returns>
        public VoidLangValue Insert(IntLangValue index, LangValueType element)
        {
            var insertIndex = Math.Max(0, Math.Min(index.Value, langValue.Values.Count));
            langValue.Values.Insert(insertIndex, element);
            return new VoidLangValue();
        }

        /// <summary>
        /// 清空列表中的所有元素
        /// </summary>
        /// <returns>VoidLangValue，表示操作完成</returns>
        public VoidLangValue Clear()
        {
            langValue.ClearInternal();
            return new VoidLangValue();
        }

        /// <summary>
        /// 对列表中的每个元素执行操作
        /// </summary>
        /// <param name="action">操作函数，接受当前元素作为参数</param>
        /// <returns>VoidLangValue，表示操作完成</returns>
        public VoidLangValue ForEach(FuncLangValue action)
        {
            // 获取当前的 VariateManager
            // 如果没有找到外部 manager，创建新的
            var manager = ExecutionContext.GetCurrentManager() ?? new VariateManager();

            // 检查 action 是否是 lambda（lambda 通常 Id 为 null 且没有 Method）
            var isLambda = action.Id is null && action.Method is null;

            if (isLambda)
            {
                // 对于 lambda，我们需要避免使用闭包机制（深拷贝）
                // 而是直接传递原始 manager 以支持外部变量访问
                foreach (var item in langValue.Values)
                {
                    // 直接执行 lambda 的主体，不创建闭包
                    // 保存当前作用域
                    var savedScopes = new List<Dictionary<string, LangValueType>>(manager.Scopes);

                    try
                    {
                        // 添加新的作用域层级
                        manager.AddChildren();
                        manager.IsFunc = true;

                        // 将参数添加到当前作用域
                        if (action.Ids?.Count == 1)
                        {
                            var paramId = action.Ids[0];
                            if (paramId is not null)
                            {
                                manager.Set(paramId, item);
                            }
                        }
                        else if (action.Ids?.Count > 0)
                        {
                            // 如果有多个参数，将 item 作为第一个参数
                            var firstParamId = action.Ids[0];
                            if (firstParamId is not null)
                            {
                                manager.Set(firstParamId, item);
                            }
                        }

                        // 执行 lambda 主体
                        action.BlockStatement?.Run(manager);
                    }
                    finally
                    {
                        // 恢复作用域
                        manager.Scopes.Clear();
                        manager.Scopes.AddRange(savedScopes);
                        manager.IsFunc = false;
                        manager.IsReturn = false;
                        manager.Result = new VoidLangValue();
                    }
                }
            }
            else
            {
                // 对于非 lambda（原生方法），使用正常的 Run 调用
                foreach (var item in langValue.Values)
                {
                    action.Run(manager, [item]);
                }
            }

            return new VoidLangValue();
        }

        /// <summary>
        /// 将列表转换为数组
        /// </summary>
        /// <returns>包含相同元素的新数组</returns>
        public ArrayLangValue ToArray()
        {
            return new ArrayLangValue(langValue.Values);
        }


        /// <summary>
        /// 检查列表中是否所有元素都满足条件
        /// </summary>
        /// <param name="predicate">谓词函数，返回布尔值</param>
        /// <returns>如果所有元素都满足条件则返回true，否则返回false</returns>
        public BoolLangValue All(FuncLangValue predicate)
        {
            foreach (var item in langValue.Values)
            {
                var manager = new VariateManager();

                var result = predicate.Run(manager, [item]);
                if (result is BoolLangValue { Value: false })
                {
                    return new BoolLangValue();
                }
            }

            return new BoolLangValue(true);
        }

        /// <summary>
        /// 获取列表的前n个元素
        /// </summary>
        /// <param name="count">要获取的元素数量</param>
        /// <returns>包含前n个元素的新列表</returns>
        public ListLangValue Take(IntLangValue count)
        {
            var takeCount = Math.Max(0, Math.Min(count.Value, langValue.Values.Count));
            var result = langValue.Values.Take(takeCount).ToList();
            return new ListLangValue(result);
        }

        /// <summary>
        /// 移除并返回列表的最后一个元素
        /// </summary>
        /// <returns>被移除的最后一个元素</returns>
        /// <exception cref="InvalidOperationError">当列表为空时抛出</exception>
        public LangValueType Pop()
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "无法从空列表中移除元素");
            }

            var lastIndex = langValue.Values.Count - 1;
            var lastElement = langValue.Values[lastIndex];
            langValue.Values.RemoveAt(lastIndex);
            return lastElement;
        }

        /// <summary>
        /// 获取列表的切片（子列表）
        /// </summary>
        /// <param name="start">起始索引（包含）</param>
        /// <param name="end">结束索引（不包含）</param>
        /// <returns>包含切片元素的新列表</returns>
        public ListLangValue Slice(IntLangValue start, IntLangValue end)
        {
            var startIndex = Math.Max(0, Math.Min(start.Value, langValue.Values.Count));
            var endIndex = Math.Max(0, Math.Min(end.Value, langValue.Values.Count));

            if (startIndex > endIndex)
            {
                return new ListLangValue(new List<LangExpression>());
            }

            var result = langValue.Values.Skip(startIndex).Take(endIndex - startIndex).ToList();
            return new ListLangValue(result);
        }

        /// <summary>
        /// 连接字符串数组为单个字符串
        /// </summary>
        /// <param name="separator">分隔符</param>
        /// <returns>连接后的字符串</returns>
        public StringLangValue Join(StringLangValue separator)
        {
            if (langValue is null)
            {
                throw new ArgumentNullException(nameof(langValue), "字符串数组不能为空");
            }

            return new StringLangValue(string.Join(separator.Value, langValue.Values.Select(v => v.ToDisplayString())));
        }

        /// <summary>
        /// 移除列表中的重复元素
        /// </summary>
        /// <returns>包含去重后元素的新列表</returns>
        public ListLangValue Distinct()
        {
            var distinct = new List<LangValueType>();
            var seen = new HashSet<string>();

            foreach (var item in langValue.Values)
            {
                var key = item.ToDisplayString();
                if (seen.Add(key))
                {
                    distinct.Add(item);
                }
            }

            return new ListLangValue(distinct);
        }

        /// <summary>
        /// 使用键选择器对列表进行排序
        /// </summary>
        /// <param name="keySelector">键选择函数</param>
        /// <param name="ascending">是否升序排序，默认为true</param>
        /// <returns>排序后的新列表</returns>
        public ListLangValue SortBy(FuncLangValue keySelector, BoolLangValue? ascending = null)
        {
            var isAscending = ascending?.Value ?? true;
            var manager = new VariateManager();

            // 创建索引-元素-键的映射列表
            var indexedItems = new List<(int index, LangValueType item, LangValueType key)>();
            for (int i = 0; i < langValue.Values.Count; i++)
            {
                var item = langValue.Values[i];
                var key = keySelector.Run(manager, [item]);
                indexedItems.Add((i, item, key));
            }

            // 排序
            indexedItems.Sort((a, b) =>
            {
                var comparison = CompareKeys(a.key, b.key);
                // 如果键相同，保持原始顺序(稳定排序)
                if (comparison == 0)
                {
                    comparison = a.index.CompareTo(b.index);
                }

                return isAscending ? comparison : -comparison;
            });

            // 提取排序后的元素
            var sortedValues = indexedItems.Select(x => x.item).ToList();
            return new ListLangValue(sortedValues);
        }

        /// <summary>
        /// 检查列表是否为空
        /// </summary>
        /// <returns>如果列表为空返回true，否则返回false</returns>
        public BoolLangValue IsEmpty()
        {
            return new BoolLangValue(langValue.Values.Count == 0);
        }

        /// <summary>
        /// 获取列表的第一个元素
        /// </summary>
        /// <returns>列表的第一个元素</returns>
        /// <exception cref="InvalidOperationError">当列表为空时抛出</exception>
        public LangValueType First()
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "列表为空，无法获取第一个元素");
            }

            return langValue.Values[0];
        }

        /// <summary>
        /// 获取列表中第一个满足条件的元素
        /// </summary>
        /// <param name="predicate">谓词函数</param>
        /// <returns>第一个满足条件的元素</returns>
        /// <exception cref="InvalidOperationError">当没有元素满足条件时抛出</exception>
        public LangValueType First(FuncLangValue predicate)
        {
            foreach (var item in langValue.Values)
            {
                var manager = new VariateManager();
                var result = predicate.Run(manager, [item]);
                if (result is BoolLangValue { Value: true })
                {
                    return item;
                }
            }

            throw new InvalidOperationError(langValue, "没有元素满足指定条件");
        }

        /// <summary>
        /// 获取列表的第一个元素，如果列表为空则返回null
        /// </summary>
        /// <returns>列表的第一个元素或null</returns>
        public LangValueType FirstOrDefault()
        {
            return langValue.Values.Count == 0 ? new NullLangValue() : langValue.Values[0];
        }

        /// <summary>
        /// 获取列表中第一个满足条件的元素，如果没有则返回null
        /// </summary>
        /// <param name="predicate">谓词函数</param>
        /// <returns>第一个满足条件的元素或null</returns>
        public LangValueType FirstOrDefault(FuncLangValue predicate)
        {
            foreach (var item in langValue.Values)
            {
                var manager = new VariateManager();
                var result = predicate.Run(manager, [item]);
                if (result is BoolLangValue { Value: true })
                {
                    return item;
                }
            }

            return new NullLangValue();
        }

        /// <summary>
        /// 获取列表的最后一个元素
        /// </summary>
        /// <returns>列表的最后一个元素</returns>
        /// <exception cref="InvalidOperationError">当列表为空时抛出</exception>
        public LangValueType Last()
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "列表为空，无法获取最后一个元素");
            }

            return langValue.Values[^1];
        }

        /// <summary>
        /// 获取列表中最后一个满足条件的元素
        /// </summary>
        /// <param name="predicate">谓词函数</param>
        /// <returns>最后一个满足条件的元素</returns>
        /// <exception cref="InvalidOperationError">当没有元素满足条件时抛出</exception>
        public LangValueType Last(FuncLangValue predicate)
        {
            for (int i = langValue.Values.Count - 1; i >= 0; i--)
            {
                var item = langValue.Values[i];
                var manager = new VariateManager();
                var result = predicate.Run(manager, [item]);
                if (result is BoolLangValue { Value: true })
                {
                    return item;
                }
            }

            throw new InvalidOperationError(langValue, "没有元素满足指定条件");
        }

        /// <summary>
        /// 获取列表的最后一个元素，如果列表为空则返回null
        /// </summary>
        /// <returns>列表的最后一个元素或null</returns>
        public LangValueType LastOrDefault()
        {
            return langValue.Values.Count == 0 ? new NullLangValue() : langValue.Values[^1];
        }

        /// <summary>
        /// 获取列表中最后一个满足条件的元素，如果没有则返回null
        /// </summary>
        /// <param name="predicate">谓词函数</param>
        /// <returns>最后一个满足条件的元素或null</returns>
        public LangValueType LastOrDefault(FuncLangValue predicate)
        {
            for (int i = langValue.Values.Count - 1; i >= 0; i--)
            {
                var item = langValue.Values[i];
                var manager = new VariateManager();
                var result = predicate.Run(manager, [item]);
                if (result is BoolLangValue { Value: true })
                {
                    return item;
                }
            }

            return new NullLangValue();
        }

        /// <summary>
        /// 获取列表中指定索引处的元素，如果索引越界则返回null
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns>指定索引处的元素或null</returns>
        public LangValueType ElementAtOrDefault(IntLangValue index)
        {
            var idx = index.Value;
            if (idx < 0) idx = langValue.Values.Count + idx;
            if (idx < 0 || idx >= langValue.Values.Count)
            {
                return new NullLangValue();
            }

            return langValue.Values[idx];
        }

        /// <summary>
        /// 获取唯一的元素，如果列表为空或有多个元素则抛出异常
        /// </summary>
        /// <returns>唯一的元素</returns>
        /// <exception cref="InvalidOperationError">当列表为空或有多个元素时抛出</exception>
        public LangValueType Single()
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "列表为空");
            }

            if (langValue.Values.Count > 1)
            {
                throw new InvalidOperationError(langValue, "列表包含多个元素");
            }

            return langValue.Values[0];
        }

        /// <summary>
        /// 获取唯一满足条件的元素
        /// </summary>
        /// <param name="predicate">谓词函数</param>
        /// <returns>唯一满足条件的元素</returns>
        /// <exception cref="InvalidOperationError">当没有或有多个元素满足条件时抛出</exception>
        public LangValueType Single(FuncLangValue predicate)
        {
            LangValueType? found = null;
            foreach (var item in langValue.Values)
            {
                var manager = new VariateManager();
                var result = predicate.Run(manager, [item]);
                if (result is BoolLangValue { Value: true })
                {
                    if (found is not null)
                    {
                        throw new InvalidOperationError(langValue, "多个元素满足指定条件");
                    }

                    found = item;
                }
            }

            if (found is null)
            {
                throw new InvalidOperationError(langValue, "没有元素满足指定条件");
            }

            return found;
        }

        /// <summary>
        /// 获取唯一的元素，如果列表为空则返回null，如果有多个元素则抛出异常
        /// </summary>
        /// <returns>唯一的元素或null</returns>
        /// <exception cref="InvalidOperationError">当列表有多个元素时抛出</exception>
        public LangValueType SingleOrDefault()
        {
            if (langValue.Values.Count == 0)
            {
                return new NullLangValue();
            }

            if (langValue.Values.Count > 1)
            {
                throw new InvalidOperationError(langValue, "列表包含多个元素");
            }

            return langValue.Values[0];
        }

        /// <summary>
        /// 获取唯一满足条件的元素，如果没有则返回null
        /// </summary>
        /// <param name="predicate">谓词函数</param>
        /// <returns>唯一满足条件的元素或null</returns>
        /// <exception cref="InvalidOperationError">当有多个元素满足条件时抛出</exception>
        public LangValueType SingleOrDefault(FuncLangValue predicate)
        {
            LangValueType? found = null;
            foreach (var item in langValue.Values)
            {
                var manager = new VariateManager();
                var result = predicate.Run(manager, [item]);
                if (result is BoolLangValue { Value: true })
                {
                    if (found is not null)
                    {
                        throw new InvalidOperationError(langValue, "多个元素满足指定条件");
                    }

                    found = item;
                }
            }

            return found ?? new NullLangValue();
        }

        /// <summary>
        /// 返回两个列表的并集（去重）
        /// </summary>
        /// <param name="other">另一个列表</param>
        /// <returns>包含两个列表所有不重复元素的新列表</returns>
        public ListLangValue Union(ListLangValue other)
        {
            var result = new List<LangValueType>();
            var seen = new HashSet<string>();

            foreach (var item in langValue.Values)
            {
                var key = item.ToDisplayString();
                if (seen.Add(key))
                {
                    result.Add(item);
                }
            }

            foreach (var item in other.Values)
            {
                var key = item.ToDisplayString();
                if (seen.Add(key))
                {
                    result.Add(item);
                }
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 返回两个列表的交集
        /// </summary>
        /// <param name="other">另一个列表</param>
        /// <returns>包含两个列表共有元素的新列表</returns>
        public ListLangValue Intersect(ListLangValue other)
        {
            var result = new List<LangValueType>();
            var otherSet = new HashSet<string>(other.Values.Select(v => v.ToDisplayString()));
            var seen = new HashSet<string>();

            foreach (var item in langValue.Values)
            {
                var key = item.ToDisplayString();
                if (otherSet.Contains(key) && seen.Add(key))
                {
                    result.Add(item);
                }
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 返回两个列表的差集（当前列表中有但另一个列表中没有的元素）
        /// </summary>
        /// <param name="other">另一个列表</param>
        /// <returns>包含差集元素的新列表</returns>
        public ListLangValue Except(ListLangValue other)
        {
            var result = new List<LangValueType>();
            var otherSet = new HashSet<string>(other.Values.Select(v => v.ToDisplayString()));
            var seen = new HashSet<string>();

            foreach (var item in langValue.Values)
            {
                var key = item.ToDisplayString();
                if (!otherSet.Contains(key) && seen.Add(key))
                {
                    result.Add(item);
                }
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 返回两个列表的对称差集（只在一个列表中出现的元素）
        /// </summary>
        /// <param name="other">另一个列表</param>
        /// <returns>包含对称差集元素的新列表</returns>
        public ListLangValue SymmetricExcept(ListLangValue other)
        {
            var result = new List<LangValueType>();
            var thisSet = new HashSet<string>(langValue.Values.Select(v => v.ToDisplayString()));
            var otherSet = new HashSet<string>(other.Values.Select(v => v.ToDisplayString()));
            var seen = new HashSet<string>();

            // 添加只在当前列表中的元素
            foreach (var item in langValue.Values)
            {
                var key = item.ToDisplayString();
                if (!otherSet.Contains(key) && seen.Add(key))
                {
                    result.Add(item);
                }
            }

            // 添加只在另一个列表中的元素
            foreach (var item in other.Values)
            {
                var key = item.ToDisplayString();
                if (!thisSet.Contains(key) && seen.Add(key))
                {
                    result.Add(item);
                }
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 检查当前列表是否是另一个列表的子集
        /// </summary>
        /// <param name="other">另一个列表</param>
        /// <returns>如果是子集返回true，否则返回false</returns>
        public BoolLangValue IsSubsetOf(ListLangValue other)
        {
            var otherSet = new HashSet<string>(other.Values.Select(v => v.ToDisplayString()));

            foreach (var item in langValue.Values)
            {
                if (!otherSet.Contains(item.ToDisplayString()))
                {
                    return new BoolLangValue(false);
                }
            }

            return new BoolLangValue(true);
        }

        /// <summary>
        /// 检查当前列表是否是另一个列表的超集
        /// </summary>
        /// <param name="other">另一个列表</param>
        /// <returns>如果是超集返回true，否则返回false</returns>
        public BoolLangValue IsSupersetOf(ListLangValue other)
        {
            var thisSet = new HashSet<string>(langValue.Values.Select(v => v.ToDisplayString()));

            foreach (var item in other.Values)
            {
                if (!thisSet.Contains(item.ToDisplayString()))
                {
                    return new BoolLangValue(false);
                }
            }

            return new BoolLangValue(true);
        }

        /// <summary>
        /// 检查两个列表是否有交集
        /// </summary>
        /// <param name="other">另一个列表</param>
        /// <returns>如果有交集返回true，否则返回false</returns>
        public BoolLangValue Overlaps(ListLangValue other)
        {
            var otherSet = new HashSet<string>(other.Values.Select(v => v.ToDisplayString()));

            foreach (var item in langValue.Values)
            {
                if (otherSet.Contains(item.ToDisplayString()))
                {
                    return new BoolLangValue(true);
                }
            }

            return new BoolLangValue(false);
        }

        /// <summary>
        /// 检查两个列表是否包含相同的元素（忽略顺序和重复）
        /// </summary>
        /// <param name="other">另一个列表</param>
        /// <returns>如果包含相同元素返回true，否则返回false</returns>
        public BoolLangValue SetEquals(ListLangValue other)
        {
            var thisSet = new HashSet<string>(langValue.Values.Select(v => v.ToDisplayString()));
            var otherSet = new HashSet<string>(other.Values.Select(v => v.ToDisplayString()));

            return new BoolLangValue(thisSet.SetEquals(otherSet));
        }

        /// <summary>
        /// 计算列表中所有数值元素的和
        /// </summary>
        /// <returns>所有元素的和</returns>
        /// <exception cref="InvalidOperationError">当列表为空或包含非数值元素时抛出</exception>
        public LangValueType Sum()
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "无法对空列表求和");
            }

            double sum = 0;
            bool hasDouble = false;

            foreach (var item in langValue.Values)
            {
                switch (item)
                {
                    case IntLangValue intVal:
                        sum += intVal.Value;
                        break;
                    case DoubleLangValue doubleVal:
                        sum += doubleVal.Value;
                        hasDouble = true;
                        break;
                    default:
                        throw new InvalidOperationError(langValue, $"无法对非数值类型 {item.TypeToString()} 求和");
                }
            }

            return hasDouble ? new DoubleLangValue(sum) : new IntLangValue((int)sum);
        }

        /// <summary>
        /// 使用选择器计算列表元素的和
        /// </summary>
        /// <param name="selector">选择器函数，返回数值</param>
        /// <returns>所有选择结果的和</returns>
        public LangValueType Sum(FuncLangValue selector)
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "无法对空列表求和");
            }

            double sum = 0;
            bool hasDouble = false;
            var manager = new VariateManager();

            foreach (var item in langValue.Values)
            {
                var result = selector.Run(manager, [item]);
                switch (result)
                {
                    case IntLangValue intVal:
                        sum += intVal.Value;
                        break;
                    case DoubleLangValue doubleVal:
                        sum += doubleVal.Value;
                        hasDouble = true;
                        break;
                    default:
                        throw new InvalidOperationError(langValue, $"选择器返回了非数值类型 {result.TypeToString()}");
                }
            }

            return hasDouble ? new DoubleLangValue(sum) : new IntLangValue((int)sum);
        }

        /// <summary>
        /// 计算列表中所有数值元素的平均值
        /// </summary>
        /// <returns>所有元素的平均值</returns>
        /// <exception cref="InvalidOperationError">当列表为空或包含非数值元素时抛出</exception>
        public DoubleLangValue Average()
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "无法对空列表求平均值");
            }

            double sum = 0;

            foreach (var item in langValue.Values)
            {
                switch (item)
                {
                    case IntLangValue intVal:
                        sum += intVal.Value;
                        break;
                    case DoubleLangValue doubleVal:
                        sum += doubleVal.Value;
                        break;
                    default:
                        throw new InvalidOperationError(langValue, $"无法对非数值类型 {item.TypeToString()} 求平均值");
                }
            }

            return new DoubleLangValue(sum / langValue.Values.Count);
        }

        /// <summary>
        /// 使用选择器计算列表元素的平均值
        /// </summary>
        /// <param name="selector">选择器函数，返回数值</param>
        /// <returns>所有选择结果的平均值</returns>
        public DoubleLangValue Average(FuncLangValue selector)
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "无法对空列表求平均值");
            }

            double sum = 0;
            var manager = new VariateManager();

            foreach (var item in langValue.Values)
            {
                var result = selector.Run(manager, [item]);
                switch (result)
                {
                    case IntLangValue intVal:
                        sum += intVal.Value;
                        break;
                    case DoubleLangValue doubleVal:
                        sum += doubleVal.Value;
                        break;
                    default:
                        throw new InvalidOperationError(langValue, $"选择器返回了非数值类型 {result.TypeToString()}");
                }
            }

            return new DoubleLangValue(sum / langValue.Values.Count);
        }

        /// <summary>
        /// 获取列表中的最小值
        /// </summary>
        /// <returns>最小值</returns>
        /// <exception cref="InvalidOperationError">当列表为空时抛出</exception>
        public LangValueType Min()
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "无法对空列表求最小值");
            }

            var min = langValue.Values[0];
            for (int i = 1; i < langValue.Values.Count; i++)
            {
                if (langValue.Values[i].Less(min))
                {
                    min = langValue.Values[i];
                }
            }

            return min;
        }

        /// <summary>
        /// 使用选择器获取列表中的最小值
        /// </summary>
        /// <param name="selector">选择器函数</param>
        /// <returns>选择结果的最小值</returns>
        public LangValueType Min(FuncLangValue selector)
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "无法对空列表求最小值");
            }

            var manager = new VariateManager();
            var minValue = selector.Run(manager, [langValue.Values[0]]);

            for (int i = 1; i < langValue.Values.Count; i++)
            {
                var currentValue = selector.Run(manager, [langValue.Values[i]]);
                if (currentValue.Less(minValue))
                {
                    minValue = currentValue;
                }
            }

            return minValue;
        }

        /// <summary>
        /// 获取具有最小选择值的元素
        /// </summary>
        /// <param name="selector">选择器函数</param>
        /// <returns>具有最小选择值的元素</returns>
        public LangValueType MinBy(FuncLangValue selector)
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "无法对空列表求最小值");
            }

            var manager = new VariateManager();
            var minItem = langValue.Values[0];
            var minValue = selector.Run(manager, [minItem]);

            for (int i = 1; i < langValue.Values.Count; i++)
            {
                var currentItem = langValue.Values[i];
                var currentValue = selector.Run(manager, [currentItem]);
                if (currentValue.Less(minValue))
                {
                    minItem = currentItem;
                    minValue = currentValue;
                }
            }

            return minItem;
        }

        /// <summary>
        /// 获取列表中的最大值
        /// </summary>
        /// <returns>最大值</returns>
        /// <exception cref="InvalidOperationError">当列表为空时抛出</exception>
        public LangValueType Max()
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "无法对空列表求最大值");
            }

            var max = langValue.Values[0];
            for (int i = 1; i < langValue.Values.Count; i++)
            {
                if (max.Less(langValue.Values[i]))
                {
                    max = langValue.Values[i];
                }
            }

            return max;
        }

        /// <summary>
        /// 使用选择器获取列表中的最大值
        /// </summary>
        /// <param name="selector">选择器函数</param>
        /// <returns>选择结果的最大值</returns>
        public LangValueType Max(FuncLangValue selector)
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "无法对空列表求最大值");
            }

            var manager = new VariateManager();
            var maxValue = selector.Run(manager, [langValue.Values[0]]);

            for (int i = 1; i < langValue.Values.Count; i++)
            {
                var currentValue = selector.Run(manager, [langValue.Values[i]]);
                if (maxValue.Less(currentValue))
                {
                    maxValue = currentValue;
                }
            }

            return maxValue;
        }

        /// <summary>
        /// 获取具有最大选择值的元素
        /// </summary>
        /// <param name="selector">选择器函数</param>
        /// <returns>具有最大选择值的元素</returns>
        public LangValueType MaxBy(FuncLangValue selector)
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "无法对空列表求最大值");
            }

            var manager = new VariateManager();
            var maxItem = langValue.Values[0];
            var maxValue = selector.Run(manager, [maxItem]);

            for (int i = 1; i < langValue.Values.Count; i++)
            {
                var currentItem = langValue.Values[i];
                var currentValue = selector.Run(manager, [currentItem]);
                if (maxValue.Less(currentValue))
                {
                    maxItem = currentItem;
                    maxValue = currentValue;
                }
            }

            return maxItem;
        }

        /// <summary>
        /// 按键对列表元素进行分组
        /// </summary>
        /// <param name="keySelector">键选择器函数</param>
        /// <returns>分组结果的字典，键为分组键，值为该组的元素列表</returns>
        public DictionaryLangValue GroupBy(FuncLangValue keySelector)
        {
            var groups = new Dictionary<string, List<LangValueType>>();
            var keyMapping = new Dictionary<string, LangValueType>();
            var manager = new VariateManager();

            foreach (var item in langValue.Values)
            {
                var keyValue = keySelector.Run(manager, [item]);
                var keyStr = keyValue.ToDisplayString();

                if (!groups.ContainsKey(keyStr))
                {
                    groups[keyStr] = [];
                    keyMapping[keyStr] = keyValue;
                }

                groups[keyStr].Add(item);
            }

            // 转换为 DictionaryLangValue
            var tuples = new List<TupleLangValue>();
            foreach (var (keyStr, items) in groups)
            {
                tuples.Add(new TupleLangValue(keyMapping[keyStr], new ListLangValue(items)));
            }

            return new DictionaryLangValue(tuples);
        }

        /// <summary>
        /// 按键对列表元素进行分组，并对每组应用元素选择器
        /// </summary>
        /// <param name="keySelector">键选择器函数</param>
        /// <param name="elementSelector">元素选择器函数</param>
        /// <returns>分组结果的字典</returns>
        public DictionaryLangValue GroupBy(FuncLangValue keySelector, FuncLangValue elementSelector)
        {
            var groups = new Dictionary<string, List<LangValueType>>();
            var keyMapping = new Dictionary<string, LangValueType>();
            var manager = new VariateManager();

            foreach (var item in langValue.Values)
            {
                var keyValue = keySelector.Run(manager, [item]);
                var keyStr = keyValue.ToDisplayString();
                var elementValue = elementSelector.Run(manager, [item]);

                if (!groups.ContainsKey(keyStr))
                {
                    groups[keyStr] = [];
                    keyMapping[keyStr] = keyValue;
                }

                groups[keyStr].Add(elementValue);
            }

            // 转换为 DictionaryLangValue
            var tuples = new List<TupleLangValue>();
            foreach (var (keyStr, items) in groups)
            {
                tuples.Add(new TupleLangValue(keyMapping[keyStr], new ListLangValue(items)));
            }

            return new DictionaryLangValue(tuples);
        }

        /// <summary>
        /// 从列表开头获取元素，直到条件不满足
        /// </summary>
        /// <param name="predicate">谓词函数</param>
        /// <returns>满足条件的连续元素列表</returns>
        public ListLangValue TakeWhile(FuncLangValue predicate)
        {
            var result = new List<LangValueType>();
            var manager = new VariateManager();

            foreach (var item in langValue.Values)
            {
                var conditionResult = predicate.Run(manager, [item]);
                if (conditionResult is BoolLangValue { Value: true })
                {
                    result.Add(item);
                }
                else
                {
                    break;
                }
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 从列表开头获取元素，直到条件不满足（带索引）
        /// </summary>
        /// <param name="predicate">谓词函数，接受元素和索引</param>
        /// <returns>满足条件的连续元素列表</returns>
        public ListLangValue TakeWhileIndexed(FuncLangValue predicate)
        {
            var result = new List<LangValueType>();
            var manager = new VariateManager();

            for (int i = 0; i < langValue.Values.Count; i++)
            {
                var item = langValue.Values[i];
                var conditionResult = predicate.Run(manager, [item, new IntLangValue(i)]);
                if (conditionResult is BoolLangValue { Value: true })
                {
                    result.Add(item);
                }
                else
                {
                    break;
                }
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 跳过列表开头的元素，直到条件不满足
        /// </summary>
        /// <param name="predicate">谓词函数</param>
        /// <returns>跳过后的剩余元素列表</returns>
        public ListLangValue SkipWhile(FuncLangValue predicate)
        {
            var result = new List<LangValueType>();
            var manager = new VariateManager();
            var skipping = true;

            foreach (var item in langValue.Values)
            {
                if (skipping)
                {
                    var conditionResult = predicate.Run(manager, [item]);
                    if (conditionResult is not BoolLangValue { Value: true })
                    {
                        skipping = false;
                        result.Add(item);
                    }
                }
                else
                {
                    result.Add(item);
                }
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 跳过列表开头的元素，直到条件不满足（带索引）
        /// </summary>
        /// <param name="predicate">谓词函数，接受元素和索引</param>
        /// <returns>跳过后的剩余元素列表</returns>
        public ListLangValue SkipWhileIndexed(FuncLangValue predicate)
        {
            var result = new List<LangValueType>();
            var manager = new VariateManager();
            var skipping = true;

            for (int i = 0; i < langValue.Values.Count; i++)
            {
                var item = langValue.Values[i];
                if (skipping)
                {
                    var conditionResult = predicate.Run(manager, [item, new IntLangValue(i)]);
                    if (conditionResult is not BoolLangValue { Value: true })
                    {
                        skipping = false;
                        result.Add(item);
                    }
                }
                else
                {
                    result.Add(item);
                }
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 获取最后n个元素
        /// </summary>
        /// <param name="count">要获取的元素数量</param>
        /// <returns>最后n个元素的列表</returns>
        public ListLangValue TakeLast(IntLangValue count)
        {
            var takeCount = Math.Max(0, Math.Min(count.Value, langValue.Values.Count));
            var skipCount = langValue.Values.Count - takeCount;
            var result = langValue.Values.Skip(skipCount).ToList();
            return new ListLangValue(result);
        }

        /// <summary>
        /// 跳过最后n个元素
        /// </summary>
        /// <param name="count">要跳过的元素数量</param>
        /// <returns>跳过后的元素列表</returns>
        public ListLangValue SkipLast(IntLangValue count)
        {
            var skipCount = Math.Max(0, Math.Min(count.Value, langValue.Values.Count));
            var takeCount = langValue.Values.Count - skipCount;
            var result = langValue.Values.Take(takeCount).ToList();
            return new ListLangValue(result);
        }

        /// <summary>
        /// 检查列表是否不为空
        /// </summary>
        /// <returns>如果列表不为空返回true，否则返回false</returns>
        public BoolLangValue Any()
        {
            return new BoolLangValue(langValue.Values.Count > 0);
        }

        /// <summary>
        /// 将列表分成指定大小的块
        /// </summary>
        /// <param name="size">每个块的大小</param>
        /// <returns>包含块的列表</returns>
        public ListLangValue Chunk(IntLangValue size)
        {
            if (size.Value <= 0)
            {
                throw new InvalidOperationError(langValue, "块大小必须大于0");
            }

            var result = new List<LangValueType>();
            for (int i = 0; i < langValue.Values.Count; i += size.Value)
            {
                var chunk = langValue.Values.Skip(i).Take(size.Value).ToList();
                result.Add(new ListLangValue(chunk));
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 将两个列表合并为元组列表
        /// </summary>
        /// <param name="other">另一个列表</param>
        /// <returns>包含元组的列表，每个元组包含两个列表对应位置的元素</returns>
        public ListLangValue Zip(ListLangValue other)
        {
            var result = new List<LangValueType>();
            var minLength = Math.Min(langValue.Values.Count, other.Values.Count);

            for (int i = 0; i < minLength; i++)
            {
                var tuple = CreateTupleWithValues(langValue.Values[i], other.Values[i]);
                result.Add(tuple);
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 将两个列表合并，使用结果选择器
        /// </summary>
        /// <param name="other">另一个列表</param>
        /// <param name="resultSelector">结果选择器函数，接受两个元素</param>
        /// <returns>合并后的列表</returns>
        public ListLangValue Zip(ListLangValue other, FuncLangValue resultSelector)
        {
            var result = new List<LangValueType>();
            var minLength = Math.Min(langValue.Values.Count, other.Values.Count);
            var manager = new VariateManager();

            for (int i = 0; i < minLength; i++)
            {
                var combined = resultSelector.Run(manager, [langValue.Values[i], other.Values[i]]);
                result.Add(combined);
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 将三个列表合并为元组列表
        /// </summary>
        /// <param name="second">第二个列表</param>
        /// <param name="third">第三个列表</param>
        /// <returns>包含三元组的列表</returns>
        public ListLangValue Zip3(ListLangValue second, ListLangValue third)
        {
            var result = new List<LangValueType>();
            var minLength = Math.Min(Math.Min(langValue.Values.Count, second.Values.Count), third.Values.Count);

            for (int i = 0; i < minLength; i++)
            {
                var tuple = CreateTupleWithValues(langValue.Values[i], second.Values[i], third.Values[i]);
                result.Add(tuple);
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 展平嵌套列表（一层）
        /// </summary>
        /// <returns>展平后的列表</returns>
        public ListLangValue Flatten()
        {
            var result = new List<LangValueType>();

            foreach (var item in langValue.Values)
            {
                if (item is ListLangValue innerList)
                {
                    result.AddRange(innerList.Values);
                }
                else if (item is ArrayLangValue innerArray)
                {
                    result.AddRange(innerArray.RunResult);
                }
                else
                {
                    result.Add(item);
                }
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 将每个元素映射到一个列表，然后展平结果
        /// </summary>
        /// <param name="selector">选择器函数，返回列表</param>
        /// <returns>展平后的列表</returns>
        public ListLangValue SelectMany(FuncLangValue selector)
        {
            var result = new List<LangValueType>();
            var manager = new VariateManager();

            foreach (var item in langValue.Values)
            {
                var selected = selector.Run(manager, [item]);
                if (selected is ListLangValue innerList)
                {
                    result.AddRange(innerList.Values);
                }
                else if (selected is ArrayLangValue innerArray)
                {
                    result.AddRange(innerArray.RunResult);
                }
                else
                {
                    result.Add(selected);
                }
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 将每个元素映射到一个列表，然后展平结果，并应用结果选择器
        /// </summary>
        /// <param name="collectionSelector">集合选择器函数，返回列表</param>
        /// <param name="resultSelector">结果选择器函数，接受原始元素和集合元素</param>
        /// <returns>展平后的列表</returns>
        public ListLangValue SelectMany(FuncLangValue collectionSelector, FuncLangValue resultSelector)
        {
            var result = new List<LangValueType>();
            var manager = new VariateManager();

            foreach (var item in langValue.Values)
            {
                var collection = collectionSelector.Run(manager, [item]);
                IEnumerable<LangValueType> innerItems;

                if (collection is ListLangValue innerList)
                {
                    innerItems = innerList.Values;
                }
                else if (collection is ArrayLangValue innerArray)
                {
                    innerItems = innerArray.RunResult;
                }
                else
                {
                    innerItems = [collection];
                }

                foreach (var innerItem in innerItems)
                {
                    var combined = resultSelector.Run(manager, [item, innerItem]);
                    result.Add(combined);
                }
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// FlatMap 是 SelectMany 的别名
        /// </summary>
        /// <param name="selector">选择器函数，返回列表</param>
        /// <returns>展平后的列表</returns>
        public ListLangValue FlatMap(FuncLangValue selector)
        {
            var result = new List<LangValueType>();
            var manager = new VariateManager();

            foreach (var item in langValue.Values)
            {
                var selected = selector.Run(manager, [item]);
                if (selected is ListLangValue innerList)
                {
                    result.AddRange(innerList.Values);
                }
                else if (selected is ArrayLangValue innerArray)
                {
                    result.AddRange(innerArray.RunResult);
                }
                else
                {
                    result.Add(selected);
                }
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 深度展平嵌套列表（递归展平所有层级）
        /// </summary>
        /// <returns>完全展平后的列表</returns>
        public ListLangValue FlattenDeep()
        {
            var result = new List<LangValueType>();
            FlattenDeepRecursive(langValue.Values, result);
            return new ListLangValue(result);
        }

        /// <summary>
        /// 将列表与索引配对
        /// </summary>
        /// <returns>包含(索引, 元素)元组的列表</returns>
        public ListLangValue WithIndex()
        {
            var result = new List<LangValueType>();

            for (int i = 0; i < langValue.Values.Count; i++)
            {
                var tuple = CreateTupleWithValues(new IntLangValue(i), langValue.Values[i]);
                result.Add(tuple);
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 生成笛卡尔积
        /// </summary>
        /// <param name="other">另一个列表</param>
        /// <returns>笛卡尔积结果列表</returns>
        public ListLangValue CartesianProduct(ListLangValue other)
        {
            var result = new List<LangValueType>();

            foreach (var item1 in langValue.Values)
            {
                foreach (var item2 in other.Values)
                {
                    var tuple = CreateTupleWithValues(item1, item2);
                    result.Add(tuple);
                }
            }

            return new ListLangValue(result);
        }

        /// <summary>
        /// 生成所有可能的排列
        /// </summary>
        /// <returns>包含所有排列的列表</returns>
        public ListLangValue Permutations()
        {
            var result = new List<LangValueType>();
            var items = langValue.Values.ToList();
            GeneratePermutations(items, 0, result);
            return new ListLangValue(result);
        }

        /// <summary>
        /// 生成所有可能的组合
        /// </summary>
        /// <param name="k">组合大小</param>
        /// <returns>包含所有k-组合的列表</returns>
        public ListLangValue Combinations(IntLangValue k)
        {
            var result = new List<LangValueType>();
            var items = langValue.Values.ToList();
            GenerateCombinations(items, k.Value, 0, [], result);
            return new ListLangValue(result);
        }

        /// <summary>
        /// 将列表分成两部分：满足条件的和不满足条件的
        /// </summary>
        /// <param name="predicate">谓词函数</param>
        /// <returns>包含两个列表的元组：(满足条件的, 不满足条件的)</returns>
        public TupleLangValue Partition(FuncLangValue predicate)
        {
            var trueList = new List<LangValueType>();
            var falseList = new List<LangValueType>();
            var manager = new VariateManager();

            foreach (var item in langValue.Values)
            {
                var result = predicate.Run(manager, [item]);
                if (result is BoolLangValue { Value: true })
                {
                    trueList.Add(item);
                }
                else
                {
                    falseList.Add(item);
                }
            }

            // 创建元组并手动设置 ItemValues
            var tuple = CreateTupleWithValues(new ListLangValue(trueList), new ListLangValue(falseList));
            return tuple;
        }

        /// <summary>
        /// 将相邻的相同元素分组
        /// </summary>
        /// <returns>分组后的列表</returns>
        public ListLangValue GroupAdjacent()
        {
            var result = new List<LangValueType>();
            if (langValue.Values.Count == 0)
            {
                return new ListLangValue(result);
            }

            var currentGroup = new List<LangValueType> { langValue.Values[0] };

            for (int i = 1; i < langValue.Values.Count; i++)
            {
                if (langValue.Values[i].Equal(langValue.Values[i - 1]))
                {
                    currentGroup.Add(langValue.Values[i]);
                }
                else
                {
                    result.Add(new ListLangValue(currentGroup));
                    currentGroup = [langValue.Values[i]];
                }
            }

            result.Add(new ListLangValue(currentGroup));
            return new ListLangValue(result);
        }

        /// <summary>
        /// 使用键选择器将相邻的相同键元素分组
        /// </summary>
        /// <param name="keySelector">键选择器函数</param>
        /// <returns>分组后的列表</returns>
        public ListLangValue GroupAdjacentBy(FuncLangValue keySelector)
        {
            var result = new List<LangValueType>();
            if (langValue.Values.Count == 0)
            {
                return new ListLangValue(result);
            }

            var manager = new VariateManager();
            var currentGroup = new List<LangValueType> { langValue.Values[0] };
            var currentKey = keySelector.Run(manager, [langValue.Values[0]]);

            for (int i = 1; i < langValue.Values.Count; i++)
            {
                var key = keySelector.Run(manager, [langValue.Values[i]]);
                if (key.Equal(currentKey))
                {
                    currentGroup.Add(langValue.Values[i]);
                }
                else
                {
                    result.Add(new ListLangValue(currentGroup));
                    currentGroup = [langValue.Values[i]];
                    currentKey = key;
                }
            }

            result.Add(new ListLangValue(currentGroup));
            return new ListLangValue(result);
        }
    }

    /// <summary>
    /// 递归展平嵌套列表
    /// </summary>
    private static void FlattenDeepRecursive(IEnumerable<LangValueType> items, List<LangValueType> result)
    {
        foreach (var item in items)
        {
            if (item is ListLangValue innerList)
            {
                FlattenDeepRecursive(innerList.Values, result);
            }
            else if (item is ArrayLangValue innerArray)
            {
                FlattenDeepRecursive(innerArray.RunResult, result);
            }
            else
            {
                result.Add(item);
            }
        }
    }

    /// <summary>
    /// 创建一个带有预填充 ItemValues 的 TupleLangValue
    /// </summary>
    private static TupleLangValue CreateTupleWithValues(params LangValueType[] values)
    {
        var tuple = new TupleLangValue(values.Cast<LangExpression>().ToList());
        foreach (var value in values)
        {
            tuple.ItemValues.Add(value);
        }
        return tuple;
    }

    /// <summary>
    /// 生成排列的辅助方法
    /// </summary>
    private static void GeneratePermutations(List<LangValueType> items, int start, List<LangValueType> result)
    {
        if (start == items.Count)
        {
            result.Add(new ListLangValue(new List<LangValueType>(items)));
            return;
        }

        for (int i = start; i < items.Count; i++)
        {
            (items[start], items[i]) = (items[i], items[start]);
            GeneratePermutations(items, start + 1, result);
            (items[start], items[i]) = (items[i], items[start]);
        }
    }

    /// <summary>
    /// 生成组合的辅助方法
    /// </summary>
    private static void GenerateCombinations(List<LangValueType> items, int k, int start,
        List<LangValueType> current, List<LangValueType> result)
    {
        if (current.Count == k)
        {
            result.Add(new ListLangValue(new List<LangValueType>(current)));
            return;
        }

        for (int i = start; i < items.Count; i++)
        {
            current.Add(items[i]);
            GenerateCombinations(items, k, i + 1, current, result);
            current.RemoveAt(current.Count - 1);
        }
    }

    /// <summary>
    /// 使用快速排序算法对列表进行排序
    /// </summary>
    /// <param name="nums">要排序的列表</param>
    /// <param name="left">排序范围的左边界</param>
    /// <param name="right">排序范围的右边界</param>
    private static void QuickSort(List<LangValueType> nums, int left, int right)
    {
        while (true)
        {
            if (left < right)
            {
                int pivotIndex = Partition(nums, left, right);
                QuickSort(nums, left, pivotIndex - 1);
                left = pivotIndex + 1;
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// 快速排序的分区函数，选择最右侧元素作为枢轴
    /// </summary>
    /// <param name="nums">要分区的列表</param>
    /// <param name="left">分区范围的左边界</param>
    /// <param name="right">分区范围的右边界</param>
    /// <returns>枢轴元素的最终位置</returns>
    private static int Partition(List<LangValueType> nums, int left, int right)
    {
        var pivot = nums[right];
        var i = left - 1;

        for (var j = left; j < right; j++)
        {
            if (!nums[j].Less(pivot)) continue;
            i++;
            Swap(nums, i, j);
        }

        Swap(nums, i + 1, right);
        return i + 1;
    }

    /// <summary>
    /// 使用自定义比较器的快速排序算法
    /// </summary>
    /// <param name="nums">要排序的列表</param>
    /// <param name="left">排序范围的左边界</param>
    /// <param name="right">排序范围的右边界</param>
    /// <param name="comparer">比较函数</param>
    private static void QuickSortWithComparer(List<LangValueType> nums, int left, int right, FuncLangValue comparer)
    {
        while (true)
        {
            if (left < right)
            {
                int pivotIndex = PartitionWithComparer(nums, left, right, comparer);
                QuickSortWithComparer(nums, left, pivotIndex - 1, comparer);
                left = pivotIndex + 1;
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// 使用自定义比较器的快速排序分区函数
    /// </summary>
    /// <param name="nums">要分区的列表</param>
    /// <param name="left">分区范围的左边界</param>
    /// <param name="right">分区范围的右边界</param>
    /// <param name="comparer">比较函数</param>
    /// <returns>枢轴元素的最终位置</returns>
    private static int PartitionWithComparer(List<LangValueType> nums, int left, int right, FuncLangValue comparer)
    {
        var pivot = nums[right];
        var i = left - 1;
        var manager = new VariateManager();

        for (var j = left; j < right; j++)
        {
            // 调用比较函数：comparer(nums[j], pivot)
            // 如果返回负数，表示 nums[j] < pivot
            var result = comparer.Run(manager, [nums[j], pivot]);
            if (result is IntLangValue { Value: < 0 })
            {
                i++;
                Swap(nums, i, j);
            }
        }

        Swap(nums, i + 1, right);
        return i + 1;
    }

    /// <summary>
    /// 归并排序的递归实现
    /// </summary>
    /// <param name="nums">要排序的列表</param>
    /// <param name="left">排序范围的左边界</param>
    /// <param name="right">排序范围的右边界</param>
    private static void MergeSortImpl(List<LangValueType> nums, int left, int right)
    {
        if (left < right)
        {
            int mid = left + (right - left) / 2;
            MergeSortImpl(nums, left, mid);
            MergeSortImpl(nums, mid + 1, right);
            MergeList(nums, left, mid, right);
        }
    }

    /// <summary>
    /// 归并排序的合并操作
    /// </summary>
    /// <param name="nums">要合并的列表</param>
    /// <param name="left">左子列表的起始索引</param>
    /// <param name="mid">左子列表的结束索引</param>
    /// <param name="right">右子列表的结束索引</param>
    private static void MergeList(List<LangValueType> nums, int left, int mid, int right)
    {
        int n1 = mid - left + 1;
        int n2 = right - mid;

        var leftArray = new List<LangValueType>(n1);
        var rightArray = new List<LangValueType>(n2);

        for (int x = 0; x < n1; x++)
            leftArray.Add(nums[left + x]);
        for (int x = 0; x < n2; x++)
            rightArray.Add(nums[mid + 1 + x]);

        int i = 0, j = 0, k = left;

        while (i < n1 && j < n2)
        {
            if (!rightArray[j].Less(leftArray[i]))
            {
                nums[k] = leftArray[i];
                i++;
            }
            else
            {
                nums[k] = rightArray[j];
                j++;
            }

            k++;
        }

        while (i < n1)
        {
            nums[k] = leftArray[i];
            i++;
            k++;
        }

        while (j < n2)
        {
            nums[k] = rightArray[j];
            j++;
            k++;
        }
    }

    /// <summary>
    /// 冒泡排序实现
    /// </summary>
    /// <param name="nums">要排序的列表</param>
    private static void BubbleSortImpl(List<LangValueType> nums)
    {
        int n = nums.Count;
        for (int i = 0; i < n - 1; i++)
        {
            bool swapped = false;
            for (int j = 0; j < n - i - 1; j++)
            {
                if (nums[j + 1].Less(nums[j]))
                {
                    Swap(nums, j, j + 1);
                    swapped = true;
                }
            }

            // 如果这一轮没有发生交换，说明列表已排序
            if (!swapped) break;
        }
    }

    /// <summary>
    /// 选择排序实现
    /// </summary>
    /// <param name="nums">要排序的列表</param>
    private static void SelectionSortImpl(List<LangValueType> nums)
    {
        int n = nums.Count;
        for (int i = 0; i < n - 1; i++)
        {
            int minIndex = i;
            for (int j = i + 1; j < n; j++)
            {
                if (nums[j].Less(nums[minIndex]))
                {
                    minIndex = j;
                }
            }

            if (minIndex != i)
            {
                Swap(nums, i, minIndex);
            }
        }
    }

    /// <summary>
    /// 插入排序实现
    /// </summary>
    /// <param name="nums">要排序的列表</param>
    private static void InsertionSortImpl(List<LangValueType> nums)
    {
        int n = nums.Count;
        for (int i = 1; i < n; i++)
        {
            var key = nums[i];
            int j = i - 1;

            while (j >= 0 && key.Less(nums[j]))
            {
                nums[j + 1] = nums[j];
                j--;
            }

            nums[j + 1] = key;
        }
    }

    /// <summary>
    /// 堆排序实现
    /// </summary>
    /// <param name="nums">要排序的列表</param>
    private static void HeapSortImpl(List<LangValueType> nums)
    {
        int n = nums.Count;

        // 构建最大堆
        for (int i = n / 2 - 1; i >= 0; i--)
        {
            HeapifyList(nums, n, i);
        }

        // 逐个提取元素
        for (int i = n - 1; i > 0; i--)
        {
            Swap(nums, 0, i);
            HeapifyList(nums, i, 0);
        }
    }

    /// <summary>
    /// 堆排序的堆化操作
    /// </summary>
    /// <param name="nums">要堆化的列表</param>
    /// <param name="n">堆的大小</param>
    /// <param name="i">当前需要堆化的节点索引</param>
    private static void HeapifyList(List<LangValueType> nums, int n, int i)
    {
        int largest = i;
        int left = 2 * i + 1;
        int right = 2 * i + 2;

        if (left < n && nums[largest].Less(nums[left]))
        {
            largest = left;
        }

        if (right < n && nums[largest].Less(nums[right]))
        {
            largest = right;
        }

        if (largest != i)
        {
            Swap(nums, i, largest);
            HeapifyList(nums, n, largest);
        }
    }

    /// <summary>
    /// 交换列表中两个元素的位置
    /// </summary>
    /// <param name="nums">列表</param>
    /// <param name="i">第一个元素的索引</param>
    /// <param name="j">第二个元素的索引</param>
    private static void Swap(List<LangValueType> nums, int i, int j)
    {
        (nums[i], nums[j]) = (nums[j], nums[i]);
    }

    /// <summary>
    /// 比较两个键的大小
    /// </summary>
    /// <param name="a">第一个键</param>
    /// <param name="b">第二个键</param>
    /// <returns>比较结果：负数表示a&lt;b，0表示a==b，正数表示a>b</returns>
    private static int CompareKeys(LangValueType a, LangValueType b)
    {
        return (a, b) switch
        {
            (IntLangValue ia, IntLangValue ib) => ia.Value.CompareTo(ib.Value),
            (DoubleLangValue da, DoubleLangValue db) => da.Value.CompareTo(db.Value),
            (StringLangValue sa, StringLangValue sb) => string.Compare(sa.Value, sb.Value, StringComparison.Ordinal),
            (BoolLangValue ba, BoolLangValue bb) => ba.Value.CompareTo(bb.Value),
            (CharLangValue ca, CharLangValue cb) => ca.Value.CompareTo(cb.Value),
            _ => string.Compare(a.ToDisplayString(), b.ToDisplayString(), StringComparison.Ordinal)
        };
    }
}