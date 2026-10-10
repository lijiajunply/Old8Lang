using System.Reflection.Emit;
using Old8Lang.Error;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 线程值类型，用于表示和管理线程
/// </summary>
public partial class ThreadLangValue : LangValueType
{
    /// <summary>
    /// 线程对象
    /// </summary>
    private readonly Thread _thread;

    /// <summary>
    /// 线程安全锁
    /// </summary>
    private readonly Lock _lock = new();

    /// <summary>
    /// 线程入口点委托（ThreadStart）
    /// </summary>
    private readonly ThreadStart? _threadStart;

    /// <summary>
    /// 线程入口点委托（ParameterizedThreadStart）
    /// </summary>
    private readonly ParameterizedThreadStart? _parameterizedThreadStart;

    /// <summary>
    /// 线程执行结果
    /// </summary>
    private object? _result;

    /// <summary>
    /// 获取线程执行结果（等同于Join但不阻塞）
    /// </summary>
    public LangValueType Result
    {
        get
        {
            // 等待线程完成
            Join();

            lock (_lock)
            {
                if (_exception is not null)
                {
                    throw new InvalidOperationError(this, "线程执行异常: " + _exception);
                }

                // 如果 _result 为 null，返回 VoidLangValue
                if (_result is null)
                {
                    return new VoidLangValue();
                }

                return ObjToValue(_result);
            }
        }
    }

    /// <summary>
    /// 线程执行是否完成
    /// </summary>
    private bool _isCompleted;

    /// <summary>
    /// 线程执行过程中发生的异常
    /// </summary>
    private Exception? _exception;

    /// <summary>
    /// 取消令牌
    /// </summary>
    private readonly CancellationToken _cancellationToken;

    /// <summary>
    /// 取消令牌源
    /// </summary>
    private readonly CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// 外部管理器，用于访问外部变量
    /// </summary>
    public VariateManager? ExternalManager { get; set; }

    /// <summary>
    /// 进度报告事件
    /// </summary>
    public event Action<object>? ProgressReported;

    /// <summary>
    /// 报告进度
    /// </summary>
    /// <param name="progress">进度信息</param>
    public void ReportProgress(object progress)
    {
        ProgressReported?.Invoke(progress);
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="threadStart">线程入口点</param>
    /// <param name="position">源代码位置</param>
    /// <param name="cancellationToken">取消令牌</param>
    public ThreadLangValue(ThreadStart threadStart, SourcePosition position = default,
        CancellationToken cancellationToken = default) : base(position)
    {
        _cancellationToken = cancellationToken;
        _threadStart = threadStart;
        _thread = new Thread(() =>
        {
            try
            {
                threadStart();
            }
            catch (OperationCanceledException)
            {
                // 线程被取消
                SetException(new OperationCanceledException("线程被取消"));
            }
            catch (Exception ex)
            {
                SetException(ex);
            }
        });
    }

    /// <summary>
    /// 构造函数，带参数
    /// </summary>
    /// <param name="parameterizedThreadStart">带参数的线程入口点</param>
    /// <param name="parameter">线程参数</param>
    /// <param name="position">源代码位置</param>
    /// <param name="cancellationToken">取消令牌</param>
    public ThreadLangValue(ParameterizedThreadStart parameterizedThreadStart, object? parameter,
        SourcePosition position = default, CancellationToken cancellationToken = default) : base(position)
    {
        _cancellationToken = cancellationToken;
        _parameterizedThreadStart = parameterizedThreadStart;
        _thread = new Thread((param) =>
        {
            try
            {
                parameterizedThreadStart(param);
            }
            catch (OperationCanceledException)
            {
                // 线程被取消
                SetException(new OperationCanceledException("线程被取消"));
            }
            catch (Exception ex)
            {
                SetException(ex);
            }
        });
    }

    /// <summary>
    /// 构造函数，内部使用，带有取消令牌源
    /// </summary>
    /// <param name="threadStart">线程入口点</param>
    /// <param name="cancellationTokenSource">取消令牌源</param>
    /// <param name="position">源代码位置</param>
    private ThreadLangValue(ThreadStart threadStart, CancellationTokenSource cancellationTokenSource,
        SourcePosition position = default) : base(position)
    {
        _cancellationTokenSource = cancellationTokenSource;
        _cancellationToken = cancellationTokenSource.Token;
        _threadStart = threadStart;
        _thread = new Thread(() =>
        {
            try
            {
                threadStart();
            }
            catch (OperationCanceledException)
            {
                // 线程被取消
                SetException(new OperationCanceledException("线程被取消"));
            }
            catch (Exception ex)
            {
                SetException(ex);
            }
        });
    }

    /// <summary>
    /// 等待线程完成
    /// </summary>
    /// <returns>线程执行结果</returns>
    public LangValueType Join(IntLangValue? timeout = null)
    {
        if (timeout is not null)
        {
            var joined = _thread.Join(timeout.GetValue<int>());
            return new BoolLangValue(joined);
        }

        _thread.Join();

        lock (_lock)
        {
            if (_exception is not null)
            {
                throw new InvalidOperationError(this, "线程执行异常: " + _exception);
            }

            // 如果 _result 为 null，返回 VoidLangValue
            if (_result is null)
            {
                return new VoidLangValue();
            }

            return ObjToValue(_result);
        }
    }

    /// <summary>
    /// 启动线程
    /// </summary>
    /// <param name="parameter">可选的线程参数</param>
    public void Start(object? parameter = null)
    {
        if (_parameterizedThreadStart is not null && parameter is not null)
        {
            _thread.Start(parameter);
        }
        else
        {
            _thread.Start();
        }
    }

    /// <summary>
    /// 检查线程是否正在运行
    /// </summary>
    public bool IsAlive()
    {
        return _thread.IsAlive;
    }

    /// <summary>
    /// 获取或设置线程是否为后台线程
    /// </summary>
    public bool IsBackground
    {
        get => _thread.IsBackground;
        set => _thread.IsBackground = value;
    }

    /// <summary>
    /// 获取或设置线程名称
    /// </summary>
    public string? Name
    {
        get => _thread.Name;
        set => _thread.Name = value;
    }

    /// <summary>
    /// 获取或设置线程优先级
    /// </summary>
    public int Priority
    {
        get => (int)_thread.Priority;
        set => _thread.Priority = (ThreadPriority)value;
    }

    /// <summary>
    /// 获取线程的托管线程ID
    /// </summary>
    public int ManagedThreadId => _thread.ManagedThreadId;

    /// <summary>
    /// 设置线程执行结果
    /// </summary>
    /// <param name="result">执行结果</param>
    public void SetResult(object result)
    {
        lock (_lock)
        {
            _result = result;
            _isCompleted = true;
        }
    }

    /// <summary>
    /// 设置线程执行异常
    /// </summary>
    /// <param name="exception">异常对象</param>
    public void SetException(Exception exception)
    {
        lock (_lock)
        {
            _exception = exception;
            _isCompleted = true;
        }
    }

    /// <summary>
    /// 获取线程状态
    /// </summary>
    public ThreadState State => _thread.ThreadState;

    /// <summary>
    /// 检查线程是否已完成
    /// </summary>
    public bool IsCompleted
    {
        get
        {
            lock (_lock)
            {
                return _isCompleted;
            }
        }
    }

    /// <summary>
    /// 将对象转换为语言值类型
    /// </summary>
    /// <param name="obj">要转换的对象</param>
    /// <returns>转换后的语言值类型</returns>
    private new static LangValueType ObjToValue(object obj)
    {
        // 如果已经是 LangValueType，直接返回
        if (obj is LangValueType langValue)
        {
            return langValue;
        }

        return obj switch
        {
            int i => new IntLangValue(i),
            double d => new DoubleLangValue(d),
            string s => new StringLangValue(s),
            bool b => new BoolLangValue(b),
            char c => new CharLangValue(c),
            null => new NullLangValue(),
            _ => new VoidLangValue() // 对于未知类型，返回 VoidLangValue
        };
    }

    public override Type OutputType(LocalManager local)
    {
        return typeof(ThreadLangValue);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 加载当前线程对象到 IL 栈
        ilGenerator.Emit(OpCodes.Ldarg_0);
    }

    public override void SetValueToIl(ILGenerator ilGenerator, LocalManager local, string idName)
    {
        // 声明局部变量
        var localVar = ilGenerator.DeclareLocal(typeof(ThreadLangValue));
        // 添加到局部变量管理器
        local.AddLocalVar(idName, localVar);
        // 设置线程对象到 IL 变量
        ilGenerator.Emit(OpCodes.Stloc, localVar);
    }

    public override object GetValue()
    {
        return this;
    }

    /// <summary>
    /// 处理点操作，直接调用 Join 方法，避免反射调用
    /// 其他方法调用委托给基类，由基类处理扩展方法
    /// </summary>
    /// <param name="dotExpression">点表达式</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>方法调用结果</returns>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        // 处理方法调用（Instance 类型）
        if (dotExpression is Instance instance)
        {
            switch (instance.Id.IdName)
            {
                case "Join":
                    // 直接调用 Join 方法，避免反射
                    if (instance.Ids.Count > 0)
                    {
                        var timeoutValue = instance.Ids[0].Run(manager);
                        return Join(timeoutValue as IntLangValue);
                    }

                    return Join();

                case "Wait":
                    // 直接调用 Join 方法（Wait 等同于 Join）
                    if (instance.Ids.Count > 0)
                    {
                        var timeoutValue = instance.Ids[0].Run(manager);
                        return Join(timeoutValue as IntLangValue);
                    }

                    return Join();

                case "Start":
                    // 启动线程
                    if (instance.Ids.Count > 0)
                    {
                        var param = instance.Ids[0].Run(manager);
                        Start(param.GetValue());
                    }
                    else
                    {
                        Start();
                    }

                    return new VoidLangValue(Position);

                case "IsAlive":
                    // 检查线程是否存活
                    return new BoolLangValue(IsAlive());

                case "State":
                    // 返回线程状态
                    return new StringLangValue(State.ToString());

                case "IsCompleted":
                    // 返回是否完成
                    return new BoolLangValue(IsCompleted);

                case "Status":
                    // 返回线程状态字符串
                    return new StringLangValue(State.ToString());

                case "Id":
                    // 返回线程ID
                    return new IntLangValue(ManagedThreadId);

                case "Cancel":
                    // 取消线程
                    Cancel();
                    return new VoidLangValue(Position);
            }
        }

        // 处理属性访问（LangId 类型）
        if (dotExpression is LangId langId)
        {
            switch (langId.IdName)
            {
                case "State":
                    return new StringLangValue(State.ToString());
                case "IsCompleted":
                    return new BoolLangValue(IsCompleted);
                case "Result":
                    return Result; // 访问Result属性，会自动等待线程完成
                case "Id":
                    return new IntLangValue(ManagedThreadId);
                case "ManagedThreadId":
                    return new IntLangValue(ManagedThreadId);
                case "IsBackground":
                    return new BoolLangValue(IsBackground);
                case "Name":
                    return Name is not null ? new StringLangValue(Name) : new NullLangValue(Position);
                case "Priority":
                    return new IntLangValue(Priority);
            }
        }

        // 处理属性设置（ClassMemberId 类型）
        if (dotExpression is ClassMemberId memberId)
        {
            switch (memberId.IdName)
            {
                case "IsBackground":
                    return new BoolLangValue(IsBackground);
                case "Name":
                    return Name is not null ? new StringLangValue(Name) : new NullLangValue(Position);
                case "Priority":
                    return new IntLangValue(Priority);
            }
        }

        // 其他情况（包括扩展方法如 Then、WithTimeout、Retry）调用基类方法
        return base.Dot(dotExpression, manager);
    }

    /// <summary>
    /// 等待所有线程完成
    /// </summary>
    /// <param name="tasks">线程列表</param>
    /// <param name="position">源码位置</param>
    /// <returns>包含所有线程结果的列表</returns>
    public static LangValueType WhenAll(List<ThreadLangValue> tasks, SourcePosition position)
    {
        var results = new List<LangValueType>();

        foreach (var task in tasks)
        {
            results.Add(task.Join());
        }

        return new ListLangValue(results, position: position);
    }

    /// <summary>
    /// 等待任意一个线程完成
    /// </summary>
    /// <param name="tasks">线程列表</param>
    /// <param name="position">源码位置</param>
    /// <returns>第一个完成的线程的结果</returns>
    public static LangValueType WhenAny(List<ThreadLangValue> tasks, SourcePosition position)
    {
        if (tasks.Count == 0)
        {
            throw new ArgumentError(position, "WhenAny requires at least one thread");
        }

        // 使用信号量来等待第一个完成的线程
        var semaphore = new Semaphore(0, tasks.Count);
        LangValueType? result = null;
        var lockObj = new Lock();
        var completed = false;

        foreach (var task in tasks)
        {
            var capturedTask = task;
            var thread = new Thread(() =>
            {
                try
                {
                    var res = capturedTask.Join();
                    lock (lockObj)
                    {
                        if (!completed)
                        {
                            result = res;
                            completed = true;
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });
            thread.Start();
        }

        semaphore.WaitOne(); // 等待第一个线程完成

        return result ?? new VoidLangValue(position);
    }

    /// <summary>
    /// 创建延迟执行的线程
    /// </summary>
    /// <param name="delayMsValue">延迟毫秒数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="position">源码位置</param>
    /// <returns>延迟线程</returns>
    public static LangValueType Delay(int delayMsValue, CancellationToken cancellationToken, SourcePosition position)
    {
        return new ThreadLangValue(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(delayMsValue);
            }
            catch (OperationCanceledException)
            {
                // 线程被取消
            }
        }, position, cancellationToken);
    }

    /// <summary>
    /// 线程完成后执行下一个线程。
    /// </summary>
    /// <remarks>
    /// continuation 返回的线程会自动 <see cref="Start"/>（continuation 内一般只写
    /// <c>spawn(f)</c>，而 spawn 只创建不启动），语义与虚拟机模式的
    /// <c>VmThreadThenMethod</c> 保持一致。
    /// </remarks>
    public ThreadLangValue Then(Func<LangValueType, ThreadLangValue> continuation)
    {
        var tcs = new CancellationTokenSource();
        ThreadLangValue? thenThread = null;

        thenThread = new ThreadLangValue(() =>
        {
            try
            {
                // 等待当前线程完成
                var result = Join();

                // 执行下一个线程
                var nextThread = continuation(result);

                // continuation 返回的线程需要先启动再等待
                nextThread.Start();

                // 等待下一个线程完成
                var nextResult = nextThread.Join();

                // 设置结果
                thenThread?.SetResult(nextResult.GetValue());
            }
            catch (Exception ex)
            {
                thenThread?.SetException(ex);
            }
        }, Position, tcs.Token);

        // 设置外部管理器
        if (thenThread is not null)
        {
            thenThread.ExternalManager = ExternalManager;
        }

        return thenThread ?? throw new ArgumentError(Position, "Then continuation must return a thread");
    }

    /// <summary>
    /// 为线程添加超时限制：超过 <paramref name="timeoutMs"/> 仍未结束则以
    /// <see cref="TimeoutException"/> 结束。
    /// </summary>
    /// <remarks>
    /// 用 <see cref="Join(IntLangValue?)"/> 的带超时重载真正限制等待时长；
    /// 超时后抛 <see cref="TimeoutException"/>（与虚拟机模式的 <c>VmThreadWithTimeoutMethod</c> 一致）。
    /// </remarks>
    public ThreadLangValue WithTimeout(int timeoutMs)
    {
        var tcs = new CancellationTokenSource();
        ThreadLangValue? timeoutThread = null;

        timeoutThread = new ThreadLangValue(() =>
        {
            try
            {
                // 带超时等待当前线程
                var joined = _thread.Join(timeoutMs);

                if (!joined)
                {
                    tcs.Cancel();
                    throw new TimeoutException($"线程等待超时（{timeoutMs}ms）");
                }

                // 已结束：取回结果（若线程内部有异常，Join() 会抛出）
                timeoutThread?.SetResult(Join().GetValue());
            }
            catch (Exception ex)
            {
                timeoutThread?.SetException(ex);
            }
        }, Position, tcs.Token)
        {
            // 设置外部管理器
            ExternalManager = ExternalManager
        };

        return timeoutThread;
    }

    /// <summary>
    /// 实现线程重试机制：把原始线程入口逻辑重新执行，最多 <paramref name="retryCount"/> 次重试。
    /// </summary>
    /// <remarks>
    /// 线程本身只能启动一次，所以这里复用创建线程时保存的入口委托
    /// （<see cref="_threadStart"/>）重新执行；带参数的线程（<see cref="ParameterizedThreadStart"/>）
    /// 没有保存入口委托，会抛出 <see cref="NotSupportedException"/>。
    /// </remarks>
    public ThreadLangValue Retry(int retryCount, int delayMs = 0)
    {
        var originalStart = _threadStart;
        var tcs = new CancellationTokenSource();
        ThreadLangValue? retryThread = null;

        retryThread = new ThreadLangValue(() =>
        {
            if (originalStart is null)
            {
                throw new NotSupportedException("该线程没有可重试的入口逻辑（带参数的线程不支持 Retry）");
            }

            Exception? lastException = null;

            for (int i = 0; i <= retryCount; i++)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    originalStart();

                    // 原始线程入口会把结果/异常写回原线程（this），重试线程要把结果转写到自己身上，
                    // 这样对重试线程 Join() 才能拿到结果（与解释器的 ThreadRetryHelper 语义一致）。
                    if (_exception is not null)
                    {
                        throw _exception;
                    }

                    retryThread?.SetResult(_result!);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (i < retryCount && delayMs > 0)
                    {
                        // 重试前延迟
                        Thread.Sleep(delayMs);
                    }
                }
            }

            // 重试次数耗尽，抛出最后一次异常
            throw lastException ?? new Exception("线程执行失败，重试次数耗尽");
        }, Position, tcs.Token)
        {
            // 设置外部管理器
            ExternalManager = ExternalManager
        };

        return retryThread;
    }

    /// <summary>
    /// 取消线程执行
    /// </summary>
    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// 带进度报告的Join方法
    /// </summary>
    public LangValueType JoinWithProgress(Action<object> progressAction)
    {
        ProgressReported += progressAction;

        try
        {
            return Join();
        }
        finally
        {
            ProgressReported -= progressAction;
        }
    }
}