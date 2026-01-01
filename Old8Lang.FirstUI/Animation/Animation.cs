using System.Diagnostics;
using Avalonia.Threading;

namespace Old8Lang.FirstUI.Animation;

/// <summary>
/// Animation 动画基类
/// </summary>
public class Animation : IAnimation
{
    private readonly Stopwatch Stopwatch = new();
    private DispatcherTimer? Timer;
    private double PausedProgress;
    private int CurrentIteration;

    /// <summary>
    /// 动画时长（毫秒）
    /// </summary>
    public int Duration { get; set; } = 300;

    /// <summary>
    /// 缓动函数
    /// </summary>
    public EasingFunction EasingFunc { get; set; } = Easing.Linear;

    /// <summary>
    /// 是否循环
    /// </summary>
    public bool Loop { get; set; } = false;

    /// <summary>
    /// 循环次数（-1 表示无限循环）
    /// </summary>
    public int LoopCount { get; set; } = -1;

    /// <summary>
    /// 是否反向播放（悠悠球效果）
    /// </summary>
    public bool AutoReverse { get; set; } = false;

    /// <summary>
    /// 动画延迟（毫秒）
    /// </summary>
    public int Delay { get; set; } = 0;

    /// <summary>
    /// 动画状态
    /// </summary>
    public AnimationStatus Status { get; private set; } = AnimationStatus.Idle;

    /// <summary>
    /// 当前进度（0.0 - 1.0）
    /// </summary>
    public double Progress { get; private set; } = 0;

    /// <summary>
    /// 帧率（FPS）
    /// </summary>
    public int FrameRate { get; set; } = 60;

    /// <summary>
    /// 动画更新事件（每帧触发）
    /// </summary>
    public event EventHandler<AnimationUpdateEventArgs>? Updated;

    /// <summary>
    /// 动画完成事件
    /// </summary>
    public event EventHandler<AnimationCompletedEventArgs>? Completed;

    /// <summary>
    /// 动画开始事件
    /// </summary>
    public event EventHandler? Started;

    /// <summary>
    /// 启动动画
    /// </summary>
    public virtual void Start()
    {
        if (Status == AnimationStatus.Running)
            return;

        Status = AnimationStatus.Running;
        Stopwatch.Restart();
        PausedProgress = 0;
        CurrentIteration = 0;

        StartTimer();
        Started?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 暂停动画
    /// </summary>
    public virtual void Pause()
    {
        if (Status != AnimationStatus.Running)
            return;

        Status = AnimationStatus.Paused;
        PausedProgress = Progress;
        Stopwatch.Stop();
        StopTimer();
    }

    /// <summary>
    /// 恢复动画
    /// </summary>
    public virtual void Resume()
    {
        if (Status != AnimationStatus.Paused)
            return;

        Status = AnimationStatus.Running;
        Stopwatch.Restart();
        StartTimer();
    }

    /// <summary>
    /// 停止动画
    /// </summary>
    public virtual void Stop()
    {
        Status = AnimationStatus.Stopped;
        Stopwatch.Stop();
        StopTimer();
        Progress = 0;
        PausedProgress = 0;

        Completed?.Invoke(this, new AnimationCompletedEventArgs { IsCancelled = true });
    }

    /// <summary>
    /// 重置动画
    /// </summary>
    public virtual void Reset()
    {
        Stop();
        Status = AnimationStatus.Idle;
        Progress = 0;
        CurrentIteration = 0;
    }

    /// <summary>
    /// 启动定时器
    /// </summary>
    private void StartTimer()
    {
        Timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / FrameRate)
        };
        Timer.Tick += OnTimerTick;
        Timer.Start();
    }

    /// <summary>
    /// 停止定时器
    /// </summary>
    private void StopTimer()
    {
        if (Timer != null)
        {
            Timer.Tick -= OnTimerTick;
            Timer.Stop();
            Timer = null;
        }
    }

    /// <summary>
    /// 定时器回调
    /// </summary>
    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (Status != AnimationStatus.Running)
            return;

        var elapsed = Stopwatch.ElapsedMilliseconds;
        var totalDuration = Duration + Delay;

        // 处理延迟
        if (elapsed < Delay)
        {
            Progress = 0;
            OnUpdate(0);
            return;
        }

        // 计算进度
        var animationElapsed = elapsed - Delay;
        var rawProgress = Math.Min((double)animationElapsed / Duration, 1.0);

        // 应用暂停偏移
        if (PausedProgress > 0)
        {
            rawProgress = Math.Min(PausedProgress + rawProgress, 1.0);
        }

        // 应用缓动
        var easedProgress = EasingFunc(rawProgress);

        // 处理反向播放
        if (AutoReverse && CurrentIteration % 2 == 1)
        {
            easedProgress = 1.0 - easedProgress;
        }

        Progress = easedProgress;
        OnUpdate(easedProgress);

        // 检查是否完成
        if (rawProgress >= 1.0)
        {
            CurrentIteration++;

            // 检查循环
            if (Loop && (LoopCount == -1 || CurrentIteration < LoopCount))
            {
                // 继续下一次循环
                Stopwatch.Restart();
                PausedProgress = 0;
            }
            else
            {
                // 动画完成
                Status = AnimationStatus.Completed;
                StopTimer();
                Completed?.Invoke(this, new AnimationCompletedEventArgs { IsCancelled = false });
            }
        }
    }

    /// <summary>
    /// 触发更新事件
    /// </summary>
    protected virtual void OnUpdate(double progress)
    {
        Updated?.Invoke(this, new AnimationUpdateEventArgs { Progress = progress });
    }

    /// <summary>
    /// 链式调用：设置时长
    /// </summary>
    public Animation SetDuration(int duration)
    {
        Duration = duration;
        return this;
    }

    /// <summary>
    /// 链式调用：设置缓动函数
    /// </summary>
    public Animation SetEasing(EasingFunction easing)
    {
        EasingFunc = easing;
        return this;
    }

    /// <summary>
    /// 链式调用：设置缓动函数（通过名称）
    /// </summary>
    public Animation SetEasing(string easingName)
    {
        EasingFunc = Easing.GetEasingFunction(easingName);
        return this;
    }

    /// <summary>
    /// 链式调用：设置循环
    /// </summary>
    public Animation SetLoop(bool loop, int count = -1)
    {
        Loop = loop;
        LoopCount = count;
        return this;
    }

    /// <summary>
    /// 链式调用：设置反向播放
    /// </summary>
    public Animation SetAutoReverse(bool autoReverse)
    {
        AutoReverse = autoReverse;
        return this;
    }

    /// <summary>
    /// 链式调用：设置延迟
    /// </summary>
    public Animation SetDelay(int delay)
    {
        Delay = delay;
        return this;
    }

    /// <summary>
    /// 链式调用：订阅更新事件
    /// </summary>
    public Animation OnUpdate(Action<double> callback)
    {
        Updated += (sender, e) => callback(e.Progress);
        return this;
    }

    /// <summary>
    /// 链式调用：订阅完成事件
    /// </summary>
    public Animation OnCompleted(Action callback)
    {
        Completed += (sender, e) =>
        {
            if (!e.IsCancelled)
                callback();
        };
        return this;
    }
}

/// <summary>
/// 动画更新事件参数
/// </summary>
public class AnimationUpdateEventArgs : EventArgs
{
    /// <summary>
    /// 当前进度（缓动后的值，0.0 - 1.0）
    /// </summary>
    public double Progress { get; set; }
}

/// <summary>
/// 泛型动画类（带起始值和结束值）
/// </summary>
public class Animation<T> : Animation
{
    /// <summary>
    /// 起始值
    /// </summary>
    public T From { get; set; }

    /// <summary>
    /// 结束值
    /// </summary>
    public T To { get; set; }

    /// <summary>
    /// 插值函数
    /// </summary>
    public Func<T, T, double, T> Interpolator { get; set; }

    /// <summary>
    /// 当前值
    /// </summary>
    public T CurrentValue { get; private set; }

    /// <summary>
    /// 值更新事件
    /// </summary>
    public new event EventHandler<AnimationValueEventArgs<T>>? Updated;

    public Animation(T from, T to, Func<T, T, double, T> interpolator)
    {
        From = from;
        To = to;
        Interpolator = interpolator;
        CurrentValue = from;
    }

    protected override void OnUpdate(double progress)
    {
        CurrentValue = Interpolator(From, To, progress);
        Updated?.Invoke(this, new AnimationValueEventArgs<T>
        {
            Progress = progress,
            Value = CurrentValue
        });
        base.OnUpdate(progress);
    }

    /// <summary>
    /// 链式调用：订阅值更新事件
    /// </summary>
    public Animation<T> OnValueUpdate(Action<T> callback)
    {
        Updated += (sender, e) => callback(e.Value);
        return this;
    }

    /// <summary>
    /// 链式调用：设置时长（重写以返回正确类型）
    /// </summary>
    public new Animation<T> SetDuration(int duration)
    {
        base.SetDuration(duration);
        return this;
    }

    /// <summary>
    /// 链式调用：设置缓动函数（重写以返回正确类型）
    /// </summary>
    public new Animation<T> SetEasing(EasingFunction easing)
    {
        base.SetEasing(easing);
        return this;
    }

    /// <summary>
    /// 链式调用：设置缓动函数（通过名称，重写以返回正确类型）
    /// </summary>
    public new Animation<T> SetEasing(string easingName)
    {
        base.SetEasing(easingName);
        return this;
    }

    /// <summary>
    /// 链式调用：设置循环（重写以返回正确类型）
    /// </summary>
    public new Animation<T> SetLoop(bool loop, int count = -1)
    {
        base.SetLoop(loop, count);
        return this;
    }

    /// <summary>
    /// 链式调用：设置反向播放（重写以返回正确类型）
    /// </summary>
    public new Animation<T> SetAutoReverse(bool autoReverse)
    {
        base.SetAutoReverse(autoReverse);
        return this;
    }

    /// <summary>
    /// 链式调用：设置延迟（重写以返回正确类型）
    /// </summary>
    public new Animation<T> SetDelay(int delay)
    {
        base.SetDelay(delay);
        return this;
    }
}

/// <summary>
/// 动画值更新事件参数
/// </summary>
public class AnimationValueEventArgs<T> : AnimationUpdateEventArgs
{
    /// <summary>
    /// 当前值
    /// </summary>
    public T Value { get; set; }
}
