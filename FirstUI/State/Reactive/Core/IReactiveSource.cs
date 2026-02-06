namespace FirstUI.State.Reactive.Core;

/// <summary>
/// 响应式源接口
/// 实现此接口的对象可以被依赖追踪系统追踪
/// </summary>
public interface IReactiveSource
{
    /// <summary>
    /// 追踪当前访问
    /// 当值被读取时调用，用于收集依赖
    /// </summary>
    void Track();

    /// <summary>
    /// 触发更新
    /// 当值变化时调用，通知所有依赖者
    /// </summary>
    void Trigger();

    /// <summary>
    /// 添加订阅者（Effect）
    /// </summary>
    /// <param name="effect">要添加的副作用</param>
    void AddSubscriber(IEffect effect);

    /// <summary>
    /// 移除订阅者（Effect）
    /// </summary>
    /// <param name="effect">要移除的副作用</param>
    void RemoveSubscriber(IEffect effect);
}
