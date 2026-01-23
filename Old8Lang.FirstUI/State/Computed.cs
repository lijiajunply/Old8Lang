namespace Old8Lang.FirstUI.State;

/// <summary>
/// Computed 计算属性类
/// 基于其他状态派生新状态，当依赖状态变化时自动重新计算
/// </summary>
/// <typeparam name="T">计算结果类型</typeparam>
public class Computed<T> : ObservableState<T>
{
    private readonly Func<T?> _computeFunction;
    private readonly List<IState> _dependencies = [];
    private readonly List<Action> _unsubscribers = [];
    private bool _isComputing;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="computeFunction">计算函数</param>
    /// <param name="dependencies">依赖的状态列表</param>
    public Computed(Func<T?> computeFunction, params IState[] dependencies)
        : base(default)
    {
        _computeFunction = computeFunction ?? throw new ArgumentNullException(nameof(computeFunction));
        _dependencies.AddRange(dependencies);

        // 订阅所有依赖状态的变化
        foreach (var dependency in _dependencies)
        {
            var unsubscribe = SubscribeToDependency(dependency);
            _unsubscribers.Add(unsubscribe);
        }

        // 初始计算
        Recompute();
    }

    /// <summary>
    /// 订阅依赖状态
    /// </summary>
    private Action SubscribeToDependency(IState dependency)
    {
        EventHandler<StateChangedEventArgs> handler = (sender, e) =>
        {
            Recompute();
        };

        dependency.Changed += handler;

        return () => dependency.Changed -= handler;
    }

    /// <summary>
    /// 重新计算
    /// </summary>
    public void Recompute()
    {
        if (_isComputing)
        {
            // 防止循环依赖导致的无限递归
            throw new InvalidOperationException("Circular dependency detected in Computed property");
        }

        try
        {
            _isComputing = true;
            var newValue = _computeFunction();

            // 直接设置值，避免触发额外的通知
            if (!EqualityComparer<T>.Default.Equals(Value, newValue))
            {
                var oldValue = Value;
                // 使用反射访问私有字段（或者重新设计 State 类暴露 SetValueInternal）
                var field = typeof(State<T>).GetField("_value",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(this, newValue);

                OnChanged(oldValue, newValue);
            }
        }
        finally
        {
            _isComputing = false;
        }
    }

    /// <summary>
    /// 添加新的依赖状态
    /// </summary>
    public void AddDependency(IState dependency)
    {
        if (!_dependencies.Contains(dependency))
        {
            _dependencies.Add(dependency);
            var unsubscribe = SubscribeToDependency(dependency);
            _unsubscribers.Add(unsubscribe);
            Recompute();
        }
    }

    /// <summary>
    /// 移除依赖状态
    /// </summary>
    public void RemoveDependency(IState dependency)
    {
        var index = _dependencies.IndexOf(dependency);
        if (index >= 0)
        {
            _dependencies.RemoveAt(index);
            _unsubscribers[index]?.Invoke();
            _unsubscribers.RemoveAt(index);
        }
    }

    /// <summary>
    /// 获取所有依赖状态
    /// </summary>
    public IReadOnlyList<IState> Dependencies => _dependencies.AsReadOnly();

    /// <summary>
    /// 禁止直接设置计算属性的值
    /// </summary>
    public new T? Value
    {
        get => base.Value;
        set => throw new InvalidOperationException("Cannot directly set the value of a Computed property");
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        foreach (var unsubscribe in _unsubscribers)
        {
            unsubscribe?.Invoke();
        }
        _unsubscribers.Clear();
        _dependencies.Clear();
        ClearSubscribers();
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    public override string ToString()
    {
        return $"Computed = {base.Value?.ToString() ?? "null"}";
    }
}

/// <summary>
/// Computed 工厂类
/// </summary>
public static class Computed
{
    /// <summary>
    /// 创建计算属性
    /// </summary>
    public static Computed<T> Create<T>(Func<T?> computeFunction, params IState[] dependencies)
    {
        return new Computed<T>(computeFunction, dependencies);
    }

    /// <summary>
    /// 创建计算属性（单个依赖）
    /// </summary>
    public static Computed<TResult> From<TSource, TResult>(
        State<TSource> source,
        Func<TSource?, TResult?> selector)
    {
        return new Computed<TResult>(() => selector(source.Value), source);
    }

    /// <summary>
    /// 创建计算属性（两个依赖）
    /// </summary>
    public static Computed<TResult> From<T1, T2, TResult>(
        State<T1> state1,
        State<T2> state2,
        Func<T1?, T2?, TResult?> selector)
    {
        return new Computed<TResult>(() => selector(state1.Value, state2.Value), state1, state2);
    }

    /// <summary>
    /// 创建计算属性（三个依赖）
    /// </summary>
    public static Computed<TResult> From<T1, T2, T3, TResult>(
        State<T1> state1,
        State<T2> state2,
        State<T3> state3,
        Func<T1?, T2?, T3?, TResult?> selector)
    {
        return new Computed<TResult>(
            () => selector(state1.Value, state2.Value, state3.Value),
            state1, state2, state3);
    }

    /// <summary>
    /// 创建计算属性（四个依赖）
    /// </summary>
    public static Computed<TResult> From<T1, T2, T3, T4, TResult>(
        State<T1> state1,
        State<T2> state2,
        State<T3> state3,
        State<T4> state4,
        Func<T1?, T2?, T3?, T4?, TResult?> selector)
    {
        return new Computed<TResult>(
            () => selector(state1.Value, state2.Value, state3.Value, state4.Value),
            state1, state2, state3, state4);
    }
}

/// <summary>
/// Computed 扩展方法
/// </summary>
public static class ComputedExtensions
{
    /// <summary>
    /// 从状态创建计算属性
    /// </summary>
    public static Computed<TResult> Select<TSource, TResult>(
        this State<TSource> source,
        Func<TSource?, TResult?> selector)
    {
        return Computed.From(source, selector);
    }

    /// <summary>
    /// 组合两个状态创建计算属性
    /// </summary>
    public static Computed<TResult> Combine<T1, T2, TResult>(
        this State<T1> state1,
        State<T2> state2,
        Func<T1?, T2?, TResult?> selector)
    {
        return Computed.From(state1, state2, selector);
    }
}
