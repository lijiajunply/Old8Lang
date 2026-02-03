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

            // TODO: 注册更多 List 方法（约13个）
            // ... 等

            // TODO: 注册 String 方法（约15个）
            // registry.Register(new StringLengthMethod());
            // registry.Register(new StringSubstringMethod());
            // registry.Register(new StringReplaceMethod());
            // ... 等

            // TODO: 注册 Dictionary 方法（约8个）
            // registry.Register(new DictGetMethod());
            // registry.Register(new DictSetMethod());
            // registry.Register(new DictKeysMethod());
            // ... 等

            // TODO: 注册 Array 方法（约5个）
            // registry.Register(new ArrayLengthMethod());
            // registry.Register(new ArrayGetMethod());
            // ... 等

            // TODO: 注册 Task 方法（约4个）
            // registry.Register(new TaskThenMethod());
            // registry.Register(new TaskCatchMethod());
            // ... 等

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
