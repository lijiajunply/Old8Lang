namespace Old8Lang.Concurrency;

/// <summary>
/// 为 VM 并发执行单元提供稳定的逻辑身份。
/// </summary>
internal static class VMExecutionUnitContext
{
    private static readonly AsyncLocal<int?> CurrentExecutionUnit = new();
    private static int _executionUnitIdCounter;

    public static int CurrentId
    {
        get
        {
            if (CurrentExecutionUnit.Value is int executionUnitId)
            {
                return executionUnitId;
            }

            return -Thread.CurrentThread.ManagedThreadId - 1;
        }
    }

    public static IDisposable EnterNewScope()
    {
        return new Scope(Interlocked.Increment(ref _executionUnitIdCounter));
    }

    private sealed class Scope : IDisposable
    {
        private readonly int? _previousExecutionUnitId;
        private bool _disposed;

        public Scope(int executionUnitId)
        {
            _previousExecutionUnitId = CurrentExecutionUnit.Value;
            CurrentExecutionUnit.Value = executionUnitId;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            CurrentExecutionUnit.Value = _previousExecutionUnitId;
            _disposed = true;
        }
    }
}
