namespace Old8Lang.Concurrency;

/// <summary>
/// 为默认线程池调度路径提供阻塞补偿，避免 barrier/mutex/latch 等原语在高阻塞场景下拖慢线程注入。
/// </summary>
internal static class VMThreadPoolCompatibility
{
    private const int BaseWorkerThreadsPerCore = 16;
    private static int _prepared;
    private static int _blockedThreadPoolWorkers;
    private static int _maxRequestedWorkerThreads;

    public static void PrepareForQueuedWork()
    {
        EnsureWorkerCapacity(Environment.ProcessorCount * BaseWorkerThreadsPerCore);
    }

    public static IDisposable EnterBlockingRegion()
    {
        if (!Thread.CurrentThread.IsThreadPoolThread)
        {
            return NoopScope.Instance;
        }

        int blockedWorkers = Interlocked.Increment(ref _blockedThreadPoolWorkers);
        int targetWorkerThreads = Environment.ProcessorCount * BaseWorkerThreadsPerCore + blockedWorkers;
        EnsureWorkerCapacity(targetWorkerThreads);

        return new BlockingScope();
    }

    private static void EnsureWorkerCapacity(int targetWorkerThreads)
    {
        if (Volatile.Read(ref _prepared) == 0)
        {
            Interlocked.Exchange(ref _prepared, 1);
        }

        while (true)
        {
            int previousTarget = Volatile.Read(ref _maxRequestedWorkerThreads);
            if (targetWorkerThreads <= previousTarget)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _maxRequestedWorkerThreads, targetWorkerThreads, previousTarget) !=
                previousTarget)
            {
                continue;
            }

            ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);
            if (targetWorkerThreads > workerThreads)
            {
                ThreadPool.SetMinThreads(targetWorkerThreads, completionPortThreads);
            }

            return;
        }
    }

    private sealed class BlockingScope : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            Interlocked.Decrement(ref _blockedThreadPoolWorkers);
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();

        public void Dispose()
        {
        }
    }
}
