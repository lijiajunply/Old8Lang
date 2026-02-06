using Old8Lang.GlobalFunctions.Implementations;
using Old8Lang.GlobalFunctions.Implementations.Concurrency;
using Old8Lang.GlobalFunctions.Implementations.Reflection;

namespace Old8Lang.GlobalFunctions.Core;

/// <summary>
/// 全局函数初始化器 - 负责注册所有内置的全局函数
/// </summary>
public static class GlobalFunctionInitializer
{
    private static bool _initialized;
    private static readonly Lock InitLock = new();

    /// <summary>
    /// 初始化并注册所有内置全局函数
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        lock (InitLock)
        {
            if (_initialized) return;

            var registry = GlobalFunctionRegistry.Instance;

            // 注册 IO 函数
            registry.Register(new PrintLineFunction());
            registry.Register(new PrintFunction());
            registry.Register(new ReadLineFunction());
            registry.Register(new ErrorFunction());
            registry.Register(new ClearFunction());

            // 注册工具函数
            registry.Register(new LenFunction());
            registry.Register(new TypeFunction());
            registry.Register(new AssertFunction());
            registry.Register(new ShowValuesFunction());

            // 注册类型转换函数
            registry.Register(new IntFunction());
            registry.Register(new DoubleFunction());
            registry.Register(new CharFunction());
            registry.Register(new BoolFunction());

            // 注册系统函数（从 Instance.cs 迁移）
            registry.Register(new LockFunction());
            registry.Register(new ExecFunction());
            registry.Register(new ToObjFunction());
            registry.Register(new CompilerFunction());
            registry.Register(new SpawnFunction());
            registry.Register(new DictFunction());
            registry.Register(new TupleFunction());

            // 注册 JSON 函数
            registry.Register(new JsonSerializeFunction());
            registry.Register(new JsonDeserializeFunction());
            registry.Register(new JsonDeserializeFromFileFunction());
            registry.Register(new JsonSerializeToFileFunction());
            registry.Register(new JsonIsValidFunction());
            registry.Register(new JsonMinifyFunction());
            registry.Register(new JsonPrettifyFunction());
            registry.Register(new JsonMergeFunction());
            registry.Register(new JsonCompareFunction());
            registry.Register(new JsonGetValueFunction());

            // 注册并发函数 - Mutex
            registry.Register(new MutexCreateFunction());
            registry.Register(new MutexLockFunction());
            registry.Register(new MutexTryLockFunction());
            registry.Register(new MutexUnlockFunction());
            registry.Register(new MutexDisposeFunction());

            // 注册并发函数 - Semaphore
            registry.Register(new SemaphoreCreateFunction());
            registry.Register(new SemaphoreAcquireFunction());
            registry.Register(new SemaphoreTryAcquireFunction());
            registry.Register(new SemaphoreReleaseFunction());
            registry.Register(new SemaphoreDisposeFunction());

            // 注册并发函数 - AtomicInt
            registry.Register(new AtomicIntCreateFunction());
            registry.Register(new AtomicIntGetFunction());
            registry.Register(new AtomicIntSetFunction());
            registry.Register(new AtomicIntIncrementFunction());
            registry.Register(new AtomicIntDecrementFunction());
            registry.Register(new AtomicIntAddFunction());
            registry.Register(new AtomicIntCompareAndSetFunction());
            registry.Register(new AtomicIntDisposeFunction());

            // 注册并发函数 - Channel
            registry.Register(new ChannelCreateFunction());
            registry.Register(new ChannelCreateBoundedFunction());
            registry.Register(new ChannelSendFunction());
            registry.Register(new ChannelTrySendFunction());
            registry.Register(new ChannelReceiveFunction());
            registry.Register(new ChannelTryReceiveFunction());
            registry.Register(new ChannelCloseFunction());
            registry.Register(new ChannelDisposeFunction());

            // 注册并发函数 - ReadWriteLock
            registry.Register(new ReadWriteLockCreateFunction());
            registry.Register(new ReadLockAcquireFunction());
            registry.Register(new ReadLockReleaseFunction());
            registry.Register(new WriteLockAcquireFunction());
            registry.Register(new WriteLockReleaseFunction());
            registry.Register(new ReadLockTryAcquireFunction());
            registry.Register(new WriteLockTryAcquireFunction());
            registry.Register(new ReadWriteLockDisposeFunction());

            // 注册并发函数 - CountDownLatch
            registry.Register(new CountDownLatchCreateFunction());
            registry.Register(new CountDownLatchCountDownFunction());
            registry.Register(new CountDownLatchWaitFunction());
            registry.Register(new CountDownLatchWaitTimeoutFunction());
            registry.Register(new CountDownLatchGetCountFunction());
            registry.Register(new CountDownLatchDisposeFunction());

            // 注册并发函数 - CyclicBarrier
            registry.Register(new CyclicBarrierCreateFunction());
            registry.Register(new CyclicBarrierAwaitFunction());
            registry.Register(new CyclicBarrierAwaitTimeoutFunction());
            registry.Register(new CyclicBarrierGetParticipantCountFunction());
            registry.Register(new CyclicBarrierGetWaitingCountFunction());
            registry.Register(new CyclicBarrierDisposeFunction());

            // 注册并发函数 - CancellationTokenSource
            registry.Register(new CreateCancellationTokenSourceFunction());
            registry.Register(new CancelFunction());
            registry.Register(new CancelAfterFunction());
            registry.Register(new DisposeCancellationTokenSourceFunction());

            // 注册并发工具函数
            registry.Register(new SleepFunction());
            registry.Register(new GetCurrentThreadIdFunction());
            registry.Register(new GetProcessorCountFunction());

            // 注册反射函数 - 类型信息查询（已合并）
            registry.Register(new GetClassInfoFunction());  // 合并了 GetClassName, GetClassMethods, GetClassFields
            registry.Register(new GetMemberInfoFunction()); // 合并了 GetMethodInfo, GetFieldInfo

            // 注册反射函数 - 类型反射
            registry.Register(new GetTypeFunction());
            registry.Register(new GetAllTypesFunction());
            registry.Register(new TypeOfFunction());
            registry.Register(new GetTypeInfoFunction());

            // 注册反射函数 - 函数信息查询
            registry.Register(new GetFunctionInfoFunction());

            // 注册反射函数 - 动态调用
            registry.Register(new InvokeMethodFunction());

            // 注册反射函数 - 字段访问
            registry.Register(new GetFieldFunction());
            registry.Register(new SetFieldFunction());

            // 注册反射函数 - 实例创建和类型检查（已合并）
            registry.Register(new CreateInstanceFunction());
            registry.Register(new IsInstanceOfFunction());
            registry.Register(new HasMemberFunction());  // 合并了 HasMethod, HasField

            // 注册重载示例函数（演示重载功能）
            // 注意：这些是示例函数，实际使用时可能需要移除
            // registry.Register(new AddIntFunction());
            // registry.Register(new AddDoubleFunction());
            // registry.Register(new AddStringFunction());

            _initialized = true;
        }
    }

    /// <summary>
    /// 确保全局函数已初始化（延迟初始化）
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
}