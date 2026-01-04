using System.Collections.Concurrent;

namespace Old8Lang.Interpreter;

/// <summary>
/// 对象池实现，用于减少频繁创建和销毁对象的开销
/// </summary>
/// <typeparam name="T">池化对象类型，必须实现IPoolable接口</typeparam>
public class ObjectPool<T> where T : class, IPoolable
{
    private readonly ConcurrentBag<T> _objectPoolables = [];
    private readonly int _maxSize;
    private readonly Func<T> _factory;

    /// <summary>
    /// 初始化对象池
    /// </summary>
    /// <param name="factory">对象创建工厂方法</param>
    /// <param name="maxSize">对象池最大容量</param>
    public ObjectPool(Func<T> factory, int maxSize = 1000)
    {
        _factory = factory;
        _maxSize = maxSize;
    }

    /// <summary>
    /// 从对象池获取对象实例
    /// </summary>
    /// <returns>对象实例</returns>
    public T Get()
    {
        return _objectPoolables.TryTake(out var item) ? item : _factory();
    }

    /// <summary>
    /// 将对象归还到对象池
    /// </summary>
    /// <param name="item">要归还的对象</param>
    public void Return(T item)
    {
        if (_objectPoolables.Count < _maxSize)
        {
            item.Reset();
            _objectPoolables.Add(item);
        }
    }
}

/// <summary>
/// 池化对象接口，所有需要池化的对象必须实现此接口
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// 重置对象状态，使其可以被复用
    /// </summary>
    void Reset();
}