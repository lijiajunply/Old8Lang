using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.StaticValues;

public partial class ThreadClassLangValue : LangValueType
{
    private static readonly ThreadClassLangValue Instance = new();

    /// <summary>
    /// 获取 Task 类的全局单例
    /// </summary>
    public static ThreadClassLangValue GetInstance() => Instance;

    public override string TypeToString() => "ThreadClass";

    public override string ToDisplayString() => "Thread";

    /// <summary>
    /// 外部管理器，用于访问外部变量
    /// </summary>
    public VariateManager? ExternalManager { get; set; }

    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        // 处理 Thread.WhenAll(...) 形式的调用
        if (dotExpression is Instance instance)
        {
            var methodName = instance.Id.IdName;

            // 返回一个包装函数,用于调用静态方法
            Func<List<LangValueType>, SourcePosition, LangValueType>? method = methodName switch
            {
                "WhenAll" => WhenAll,
                "WhenAny" => WhenAny,
                "Delay" => Delay,
                "Sleep" => Sleep,
                "CurrentThread" => CurrentThread,
                _ => null
            };

            if (method == null)
            {
                throw new AttributeError(dotExpression.Position, methodName, "Thread");
            }

            // 执行参数
            List<LangValueType> args;
            if (ExternalManager != null)
            {
                // 使用 ExternalManager 执行参数
                args = instance.Ids.Select(id => id.Run(ExternalManager)).ToList();
            }
            else
            {
                // 对于简单的静态方法（如 Sleep），直接执行参数
                args = instance.Ids.Select(id => id.Run(manager)).ToList();
            }

            return method(args, instance.Position);
        }

        // 处理 Thread.WhenAll 形式的访问（不带调用）
        if (dotExpression is ClassMemberId memberId)
        {
            var methodName = memberId.IdName;

            // 返回一个包装函数,用于调用静态方法
            return methodName switch
            {
                "WhenAll" => new ThreadStaticMethodWrapper("WhenAll", WhenAll),
                "WhenAny" => new ThreadStaticMethodWrapper("WhenAny", WhenAny),
                "Delay" => new ThreadStaticMethodWrapper("Delay", Delay),
                "Sleep" => new ThreadStaticMethodWrapper("Sleep", Sleep),
                "CurrentThread" => new ThreadStaticMethodWrapper("CurrentThread", CurrentThread),
                _ => throw new AttributeError(dotExpression.Position, methodName, "Thread")
            };
        }

        throw new AttributeError(dotExpression.Position,
            dotExpression.ToString() ?? "unknown", "Thread");
    }

    /// <summary>
    /// WhenAll 静态方法实现
    /// </summary>
    private static LangValueType WhenAll(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 1)
        {
            throw new ArgumentError(position,
                $"WhenAll 期望 1 个参数(任务数组),但提供了 {args.Count} 个");
        }

        var taskList = args[0];
        if (taskList is not ILangList list)
        {
            // 创建临时节点用于错误报告
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "Array/List", taskList.TypeToString());
        }

        var tasks = new List<ThreadLangValue>();
        foreach (var item in list.GetItems())
        {
            if (item is not ThreadLangValue task)
            {
                var tempNode = new NullLangValue(position);
                throw new TypeError(tempNode, "Task", item.TypeToString());
            }

            tasks.Add(task);
        }

        return ThreadLangValue.WhenAll(tasks, position);
    }

    /// <summary>
    /// WhenAny 静态方法实现
    /// </summary>
    private static LangValueType WhenAny(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 1)
        {
            throw new ArgumentError(position,
                $"WhenAny 期望 1 个参数(任务数组),但提供了 {args.Count} 个");
        }

        var taskList = args[0];
        if (taskList is not ILangList list)
        {
            // 创建临时节点用于错误报告
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "Array/List", taskList.TypeToString());
        }

        var tasks = new List<ThreadLangValue>();
        foreach (var item in list.GetItems())
        {
            if (item is not ThreadLangValue task)
            {
                var tempNode = new NullLangValue(position);
                throw new TypeError(tempNode, "Task", item.TypeToString());
            }

            tasks.Add(task);
        }

        return ThreadLangValue.WhenAny(tasks, position);
    }

    /// <summary>
    /// Delay 静态方法实现
    /// </summary>
    private static LangValueType Delay(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 1 or > 2)
        {
            throw new ArgumentError(position,
                $"Delay 期望 1-2 个参数,但提供了 {args.Count} 个");
        }

        if (args[0] is not IntLangValue delayMs)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "int", args[0].TypeToString());
        }

        return ThreadLangValue.Delay(delayMs.Value, CancellationToken.None, position);
    }

    /// <summary>
    /// Sleep 静态方法实现
    /// </summary>
    private static LangValueType Sleep(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 1)
        {
            throw new ArgumentError(position,
                $"Sleep 期望 1 个参数,但提供了 {args.Count} 个");
        }

        if (args[0] is not IntLangValue delayMs)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "int", args[0].TypeToString());
        }

        // 直接调用 .NET Thread.Sleep
        System.Threading.Thread.Sleep(delayMs.Value);

        return new VoidLangValue(position);
    }

    /// <summary>
    /// CurrentThread 静态方法实现 - 返回当前线程的 ThreadLangValue 包装
    /// </summary>
    private static LangValueType CurrentThread(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 0)
        {
            throw new ArgumentError(position,
                $"CurrentThread 不需要参数,但提供了 {args.Count} 个");
        }

        // 获取当前 .NET 线程
        var currentThread = System.Threading.Thread.CurrentThread;

        // 创建一个特殊的 ThreadLangValue，包装当前线程
        // 注意：由于 ThreadLangValue 构造函数需要 ThreadStart，我们需要提供一个空操作
        var threadValue = new ThreadLangValue(() => { }, position);

        // 使用反射设置内部的 Thread 字段为当前线程
        var threadField = typeof(ThreadLangValue).GetField("Thread",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        threadField?.SetValue(threadValue, currentThread);

        return threadValue;
    }
}

/// <summary>
/// Thread 静态方法的包装器
/// </summary>
public partial class ThreadStaticMethodWrapper(
    string methodName,
    Func<List<LangValueType>, SourcePosition, LangValueType> method)
    : LangValueType
{
    public override string TypeToString() => "ThreadStaticMethod";

    public override string ToDisplayString() => $"Thread.{methodName}";

    /// <summary>
    /// 执行静态方法
    /// </summary>
    public LangValueType Invoke(List<LangValueType> args, SourcePosition position)
    {
        return method(args, position);
    }
}