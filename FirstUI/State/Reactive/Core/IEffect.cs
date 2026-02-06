namespace FirstUI.State.Reactive.Core;

/// <summary>
/// 副作用接口
/// 实现此接口的对象可以响应响应式源的变化
/// </summary>
public interface IEffect : IDisposable
{
    /// <summary>
    /// 执行副作用
    /// </summary>
    void Run();

    /// <summary>
    /// 添加依赖
    /// </summary>
    /// <param name="source">依赖的响应式源</param>
    void AddDependency(IReactiveSource source);

    /// <summary>
    /// 清除所有依赖
    /// </summary>
    void ClearDependencies();

    /// <summary>
    /// 是否已释放
    /// </summary>
    bool IsDisposed { get; }
}
