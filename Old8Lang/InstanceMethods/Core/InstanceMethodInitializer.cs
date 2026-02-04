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

            // 注册 List 基础方法
            registry.Register(new Implementations.List.ListAddMethod());
            registry.Register(new Implementations.List.ListRemoveMethod());
            registry.Register(new Implementations.List.ListCountMethod());
            registry.Register(new Implementations.List.ListClearMethod());
            registry.Register(new Implementations.List.ListContainsMethod());

            // 注册 List 高级方法
            registry.Register(new Implementations.List.ListRemoveAtMethod());
            registry.Register(new Implementations.List.ListAddListMethod());
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

            // 注册 List 排序和聚合方法
            registry.Register(new Implementations.List.ListSortMethod());
            registry.Register(new Implementations.List.ListFirstMethod());
            registry.Register(new Implementations.List.ListLastMethod());
            registry.Register(new Implementations.List.ListInsertMethod());
            registry.Register(new Implementations.List.ListSumMethod());
            registry.Register(new Implementations.List.ListAverageMethod());
            registry.Register(new Implementations.List.ListMinMethod());
            registry.Register(new Implementations.List.ListMaxMethod());
            registry.Register(new Implementations.List.ListDistinctMethod());
            registry.Register(new Implementations.List.ListToStrMethod());

            // 注册 List 查询和聚合方法
            registry.Register(new Implementations.List.ListFirstWithPredicateMethod());
            registry.Register(new Implementations.List.ListFirstOrDefaultMethod());
            registry.Register(new Implementations.List.ListLastWithPredicateMethod());
            registry.Register(new Implementations.List.ListLastOrDefaultMethod());
            registry.Register(new Implementations.List.ListElementAtMethod());
            registry.Register(new Implementations.List.ListAggregateMethod());
            registry.Register(new Implementations.List.ListAggregateWithSeedMethod());
            registry.Register(new Implementations.List.ListForEachMethod());
            registry.Register(new Implementations.List.ListJoinMethod());

            // 注册 List 集合操作方法
            registry.Register(new Implementations.List.ListUnionMethod());
            registry.Register(new Implementations.List.ListIntersectMethod());
            registry.Register(new Implementations.List.ListExceptMethod());
            registry.Register(new Implementations.List.ListZipMethod());
            registry.Register(new Implementations.List.ListGroupByMethod());

            // 注册 List 排序检查方法
            registry.Register(new Implementations.List.ListIsSortedMethod());

            // 注册 List 排序算法方法
            registry.Register(new Implementations.List.ListSortWithComparerMethod());
            registry.Register(new Implementations.List.ListQuickSortMethod());
            registry.Register(new Implementations.List.ListMergeSortMethod());
            registry.Register(new Implementations.List.ListBubbleSortMethod());
            registry.Register(new Implementations.List.ListSelectionSortMethod());
            registry.Register(new Implementations.List.ListInsertionSortMethod());
            registry.Register(new Implementations.List.ListHeapSortMethod());

            // List 方法迁移完成！共 50 个方法

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
            registry.Register(new Implementations.Dictionary.DictGetMethod());
            registry.Register(new Implementations.Dictionary.DictSetMethod());
            registry.Register(new Implementations.Dictionary.DictKeysMethod());
            registry.Register(new Implementations.Dictionary.DictValuesMethod());
            registry.Register(new Implementations.Dictionary.DictContainsKeyMethod());
            registry.Register(new Implementations.Dictionary.DictRemoveMethod());
            registry.Register(new Implementations.Dictionary.DictClearMethod());
            registry.Register(new Implementations.Dictionary.DictCountMethod());

            // Dictionary 方法迁移完成！共 8 个方法

            // 注册 Array 方法
            registry.Register(new Implementations.Array.ArrayLengthMethod());
            registry.Register(new Implementations.Array.ArrayGetMethod());
            registry.Register(new Implementations.Array.ArraySetMethod());
            registry.Register(new Implementations.Array.ArrayToListMethod());
            registry.Register(new Implementations.Array.ArraySliceMethod());

            // Array 方法迁移完成！共 5 个方法

            // 注册 Task 方法
            registry.Register(new Implementations.Task.TaskAwaitMethod());
            registry.Register(new Implementations.Task.TaskThenMethod());
            registry.Register(new Implementations.Task.TaskCatchMethod());
            registry.Register(new Implementations.Task.TaskFinallyMethod());

            // Task 方法迁移完成！共 4 个方法
            // 注意：Retry 方法在 Operation.cs 中有特殊处理，不需要注册为实例方法

            // TODO: 注册 Thread 方法（约3个）
            // registry.Register(new ThreadJoinMethod());
            // registry.Register(new ThreadIsAliveMethod());
            // ... 等

            // TODO: 注册 Tuple 方法（约2个）
            // registry.Register(new TupleGetMethod());
            // ... 等

            // TODO: 注册 Char 方法（约4个）
            // registry.Register(new CharToUpperMethod());
            // registry.Register(new CharToLowerMethod());
            // ... 等

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
