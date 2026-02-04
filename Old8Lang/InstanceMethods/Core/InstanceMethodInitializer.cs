namespace Old8Lang.InstanceMethods.Core;

/// <summary>
/// 实例方法初始化器 - 负责注册所有内置的实例方法
/// </summary>
public static class InstanceMethodInitializer
{
    private static bool _initialized;
    private static readonly Lock InitLock = new();

    /// <summary>
    /// 初始化并注册所有内置实例方法
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        lock (InitLock)
        {
            if (_initialized) return;

            var registry = InstanceMethodRegistry.Instance;

            // 注册 List 基础方法（特有方法）
            registry.Register(new Implementations.List.ListAddMethod());
            registry.Register(new Implementations.List.ListRemoveMethod());
            registry.Register(new Implementations.List.ListClearMethod());
            registry.Register(new Implementations.List.ListRemoveAtMethod());
            registry.Register(new Implementations.List.ListAddListMethod());
            registry.Register(new Implementations.List.ListInsertMethod());

            // 注册 List 基础查询方法
            registry.Register(new Implementations.List.ListPopMethod());
            registry.Register(new Implementations.List.ListSliceMethod());
            registry.Register(new Implementations.List.ListElementAtOrDefaultMethod());
            registry.Register(new Implementations.List.ListSingleMethod());
            registry.Register(new Implementations.List.ListSingleOrDefaultMethod());
            registry.Register(new Implementations.List.ListIsEmptyMethod());
            registry.Register(new Implementations.List.ListToArrayMethod());

            // 注册 List 高级查询方法
            registry.Register(new Implementations.List.ListMaxByMethod());
            registry.Register(new Implementations.List.ListMinByMethod());
            registry.Register(new Implementations.List.ListTakeLastMethod());
            registry.Register(new Implementations.List.ListSkipLastMethod());
            registry.Register(new Implementations.List.ListTakeWhileMethod());
            registry.Register(new Implementations.List.ListSkipWhileMethod());

            // 注册 List 通用方法（基于 ILangList，通过包装器）
            registry.Register(new Implementations.List.ListFilterMethod());
            registry.Register(new Implementations.List.ListMapMethod());
            registry.Register(new Implementations.List.ListReduceMethod());
            registry.Register(new Implementations.List.ListReverseMethod());
            registry.Register(new Implementations.List.ListIndexOfMethod());
            registry.Register(new Implementations.List.ListConcatMethod());
            registry.Register(new Implementations.List.ListFindMethod());
            registry.Register(new Implementations.List.ListSkipMethod());
            registry.Register(new Implementations.List.ListTakeMethod());
            registry.Register(new Implementations.List.ListAnyMethod());
            registry.Register(new Implementations.List.ListAllMethod());
            registry.Register(new Implementations.List.ListSortMethod());
            registry.Register(new Implementations.List.ListFirstMethod());
            registry.Register(new Implementations.List.ListLastMethod());
            registry.Register(new Implementations.List.ListSumMethod());
            registry.Register(new Implementations.List.ListAverageMethod());
            registry.Register(new Implementations.List.ListMinMethod());
            registry.Register(new Implementations.List.ListMaxMethod());
            registry.Register(new Implementations.List.ListDistinctMethod());
            registry.Register(new Implementations.List.ListToStrMethod());
            registry.Register(new Implementations.List.ListFirstOrDefaultMethod());
            registry.Register(new Implementations.List.ListLastOrDefaultMethod());
            registry.Register(new Implementations.List.ListElementAtMethod());
            registry.Register(new Implementations.List.ListForEachMethod());
            registry.Register(new Implementations.List.ListJoinMethod());
            registry.Register(new Implementations.List.ListUnionMethod());
            registry.Register(new Implementations.List.ListIntersectMethod());
            registry.Register(new Implementations.List.ListExceptMethod());
            registry.Register(new Implementations.List.ListZipMethod());
            registry.Register(new Implementations.List.ListGroupByMethod());
            registry.Register(new Implementations.List.ListIsSortedMethod());
            registry.Register(new Implementations.List.ListContainsMethod());
            registry.Register(new Implementations.List.ListCountMethod());

            // 注册 List 特殊查询方法（带 predicate 的变体）
            registry.Register(new Implementations.List.ListFirstWithPredicateMethod());
            registry.Register(new Implementations.List.ListLastWithPredicateMethod());

            // 注册 List 聚合方法（Aggregate 是 Reduce 的别名，但支持不同参数）
            registry.Register(new Implementations.List.ListAggregateMethod());
            registry.Register(new Implementations.List.ListAggregateWithSeedMethod());

            // 注册 List 排序算法方法（特有的具体排序实现）
            registry.Register(new Implementations.List.ListSortWithComparerMethod());
            registry.Register(new Implementations.List.ListQuickSortMethod());
            registry.Register(new Implementations.List.ListMergeSortMethod());
            registry.Register(new Implementations.List.ListBubbleSortMethod());
            registry.Register(new Implementations.List.ListSelectionSortMethod());
            registry.Register(new Implementations.List.ListInsertionSortMethod());
            registry.Register(new Implementations.List.ListHeapSortMethod());

            // List 方法注册完成！

            // 注册 String 基础方法
            registry.Register(new Implementations.String.StringLengthMethod());
            registry.Register(new Implementations.String.StringSubstringMethod());
            registry.Register(new Implementations.String.StringReplaceMethod());
            registry.Register(new Implementations.String.StringSplitMethod());
            registry.Register(new Implementations.String.StringToUpperMethod());
            registry.Register(new Implementations.String.StringToLowerMethod());
            registry.Register(new Implementations.String.StringTrimMethod());
            registry.Register(new Implementations.String.StringContainsMethod());

            // 注册 String 高级方法
            registry.Register(new Implementations.String.StringIndexOfMethod());
            registry.Register(new Implementations.String.StringStartsWithMethod());
            registry.Register(new Implementations.String.StringEndsWithMethod());
            registry.Register(new Implementations.String.StringPadLeftMethod());
            registry.Register(new Implementations.String.StringPadRightMethod());
            registry.Register(new Implementations.String.StringReverseMethod());
            registry.Register(new Implementations.String.StringToCharArrayMethod());

            // String 方法迁移完成！共 15 个方法

            // 注册 Dictionary 方法
            registry.Register(new Implementations.Dictionary.DictKeysMethod());
            registry.Register(new Implementations.Dictionary.DictValuesMethod());
            registry.Register(new Implementations.Dictionary.DictContainsKeyMethod());
            registry.Register(new Implementations.Dictionary.DictRemoveMethod());
            registry.Register(new Implementations.Dictionary.DictClearMethod());
            registry.Register(new Implementations.Dictionary.DictCountMethod());

            // 注册 Dictionary 高级方法
            registry.Register(new Implementations.Dictionary.DictAddMethod());
            registry.Register(new Implementations.Dictionary.DictContainsValueMethod());
            registry.Register(new Implementations.Dictionary.DictGetOrElseMethod());
            registry.Register(new Implementations.Dictionary.DictMergeMethod());
            registry.Register(new Implementations.Dictionary.DictUpdateMethod());
            registry.Register(new Implementations.Dictionary.DictCloneMethod());
            registry.Register(new Implementations.Dictionary.DictMapMethod());
            registry.Register(new Implementations.Dictionary.DictFilterMethod());
            registry.Register(new Implementations.Dictionary.DictForEachMethod());
            registry.Register(new Implementations.Dictionary.DictToListMethod());
            registry.Register(new Implementations.Dictionary.DictIsEmptyMethod());

            // Dictionary 方法迁移完成！共 19 个方法

            // 注册 Array 方法
            registry.Register(new Implementations.Array.ArrayLengthMethod());
            registry.Register(new Implementations.Array.ArrayGetMethod());
            registry.Register(new Implementations.Array.ArraySetMethod());
            registry.Register(new Implementations.Array.ArrayToListMethod());
            registry.Register(new Implementations.Array.ArraySliceMethod());

            // Array 方法迁移完成！共 5 个方法

            // 注册 Array 通用方法（基于 ILangList）
            registry.Register(new Implementations.Array.ArrayContainsMethod());
            registry.Register(new Implementations.Array.ArrayReverseMethod());
            registry.Register(new Implementations.Array.ArrayFilterMethod());
            registry.Register(new Implementations.Array.ArrayMapMethod());
            registry.Register(new Implementations.Array.ArrayAnyMethod());
            registry.Register(new Implementations.Array.ArrayAllMethod());
            registry.Register(new Implementations.Array.ArrayFirstMethod());
            registry.Register(new Implementations.Array.ArrayFirstOrDefaultMethod());
            registry.Register(new Implementations.Array.ArrayLastMethod());
            registry.Register(new Implementations.Array.ArraySkipMethod());
            registry.Register(new Implementations.Array.ArrayTakeMethod());
            registry.Register(new Implementations.Array.ArrayDistinctMethod());
            registry.Register(new Implementations.Array.ArrayFindMethod());
            registry.Register(new Implementations.Array.ArrayConcatMethod());
            registry.Register(new Implementations.Array.ArrayIndexOfMethod());
            registry.Register(new Implementations.Array.ArraySumMethod());
            registry.Register(new Implementations.Array.ArrayAverageMethod());
            registry.Register(new Implementations.Array.ArrayMinMethod());
            registry.Register(new Implementations.Array.ArrayMaxMethod());
            registry.Register(new Implementations.Array.ArrayReduceMethod());
            registry.Register(new Implementations.Array.ArrayForEachMethod());
            registry.Register(new Implementations.Array.ArrayJoinMethod());
            registry.Register(new Implementations.Array.ArrayUnionMethod());
            registry.Register(new Implementations.Array.ArrayIntersectMethod());
            registry.Register(new Implementations.Array.ArrayExceptMethod());
            registry.Register(new Implementations.Array.ArrayZipMethod());
            registry.Register(new Implementations.Array.ArrayGroupByMethod());
            registry.Register(new Implementations.Array.ArraySortMethod());
            registry.Register(new Implementations.Array.ArrayIsSortedMethod());
            registry.Register(new Implementations.Array.ArrayToStrMethod());
            registry.Register(new Implementations.Array.ArrayElementAtMethod());
            registry.Register(new Implementations.Array.ArrayLastOrDefaultMethod());

            // 注册 Task 方法
            registry.Register(new Implementations.Task.TaskAwaitMethod());
            registry.Register(new Implementations.Task.TaskThenMethod());
            registry.Register(new Implementations.Task.TaskCatchMethod());
            registry.Register(new Implementations.Task.TaskFinallyMethod());

            // Task 方法迁移完成！共 4 个方法
            // 注意：Retry 方法在 Operation.cs 中有特殊处理，不需要注册为实例方法

            // 注册 Thread 方法
            registry.Register(new Implementations.Thread.ThreadJoinMethod());
            registry.Register(new Implementations.Thread.ThreadIsAliveMethod());
            registry.Register(new Implementations.Thread.ThreadStartMethod());

            // Thread 方法迁移完成！共 3 个方法
            // 注意：Abort 方法在 .NET Core 中不受支持，已移除

            // 注册 Tuple 方法
            registry.Register(new Implementations.Tuple.TupleGetMethod());
            registry.Register(new Implementations.Tuple.TupleToListMethod());

            // Tuple 方法迁移完成！共 2 个方法

            // 注册 Tuple 通用方法（基于 ILangList）
            registry.Register(new Implementations.Tuple.TupleContainsMethod());
            registry.Register(new Implementations.Tuple.TupleReverseMethod());
            registry.Register(new Implementations.Tuple.TupleFilterMethod());
            registry.Register(new Implementations.Tuple.TupleMapMethod());
            registry.Register(new Implementations.Tuple.TupleAnyMethod());
            registry.Register(new Implementations.Tuple.TupleAllMethod());
            registry.Register(new Implementations.Tuple.TupleFirstMethod());
            registry.Register(new Implementations.Tuple.TupleFirstOrDefaultMethod());
            registry.Register(new Implementations.Tuple.TupleLastMethod());
            registry.Register(new Implementations.Tuple.TupleSkipMethod());
            registry.Register(new Implementations.Tuple.TupleTakeMethod());
            registry.Register(new Implementations.Tuple.TupleDistinctMethod());
            registry.Register(new Implementations.Tuple.TupleFindMethod());
            registry.Register(new Implementations.Tuple.TupleConcatMethod());
            registry.Register(new Implementations.Tuple.TupleIndexOfMethod());
            registry.Register(new Implementations.Tuple.TupleSumMethod());
            registry.Register(new Implementations.Tuple.TupleAverageMethod());
            registry.Register(new Implementations.Tuple.TupleMinMethod());
            registry.Register(new Implementations.Tuple.TupleMaxMethod());
            registry.Register(new Implementations.Tuple.TupleReduceMethod());
            registry.Register(new Implementations.Tuple.TupleForEachMethod());
            registry.Register(new Implementations.Tuple.TupleJoinMethod());
            registry.Register(new Implementations.Tuple.TupleUnionMethod());
            registry.Register(new Implementations.Tuple.TupleIntersectMethod());
            registry.Register(new Implementations.Tuple.TupleExceptMethod());
            registry.Register(new Implementations.Tuple.TupleZipMethod());
            registry.Register(new Implementations.Tuple.TupleGroupByMethod());
            registry.Register(new Implementations.Tuple.TupleSortMethod());
            registry.Register(new Implementations.Tuple.TupleIsSortedMethod());
            registry.Register(new Implementations.Tuple.TupleToStrMethod());
            registry.Register(new Implementations.Tuple.TupleElementAtMethod());
            registry.Register(new Implementations.Tuple.TupleLastOrDefaultMethod());

            // 注册 Char 方法
            registry.Register(new Implementations.Char.CharToUpperMethod());
            registry.Register(new Implementations.Char.CharToLowerMethod());
            registry.Register(new Implementations.Char.CharIsDigitMethod());
            registry.Register(new Implementations.Char.CharIsLetterMethod());

            // 注册 Char 高级方法
            registry.Register(new Implementations.Char.CharIsWhiteSpaceMethod());
            registry.Register(new Implementations.Char.CharIsUpperMethod());
            registry.Register(new Implementations.Char.CharIsLowerMethod());
            registry.Register(new Implementations.Char.CharIsLetterOrDigitMethod());
            registry.Register(new Implementations.Char.CharIsPunctuationMethod());
            registry.Register(new Implementations.Char.CharIsSymbolMethod());
            registry.Register(new Implementations.Char.CharIsControlMethod());
            registry.Register(new Implementations.Char.CharToIntMethod());
            registry.Register(new Implementations.Char.CharGetNumericValueMethod());
            registry.Register(new Implementations.Char.CharCompareToMethod());

            // Char 方法迁移完成！共 14 个方法

            _initialized = true;
        }
    }

    /// <summary>
    /// 确保实例方法已初始化（延迟初始化）
    /// </summary>
    public static void EnsureInitialized()
    {
        lock (InitLock)
        {
            if (!_initialized)
            {
                Initialize();
            }
        }
    }

    /// <summary>
    /// 重置初始化状态（主要用于测试）
    /// </summary>
    public static void Reset()
    {
        lock (InitLock)
        {
            _initialized = false;
            InstanceMethodRegistry.Instance.Clear();
        }
    }
}
