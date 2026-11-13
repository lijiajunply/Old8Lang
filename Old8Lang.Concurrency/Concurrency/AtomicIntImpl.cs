namespace Old8Lang.Concurrency;

/// <summary>
/// 原子整数类
/// </summary>
public class AtomicIntImpl(int initialValue)
{
    private int Value = initialValue;

    public int Get() => Interlocked.CompareExchange(ref Value, 0, 0);

    public void Set(int newValue) => Interlocked.Exchange(ref Value, newValue);

    public int Increment() => Interlocked.Increment(ref Value);

    public int Decrement() => Interlocked.Decrement(ref Value);

    public int Add(int delta) => Interlocked.Add(ref Value, delta);

    public bool CompareAndSet(int expectedValue, int newValue)
    {
        return Interlocked.CompareExchange(ref Value, newValue, expectedValue) == expectedValue;
    }
}
