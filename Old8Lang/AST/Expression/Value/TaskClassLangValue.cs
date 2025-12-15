using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// Task 类的全局对象,提供静态方法访问
/// </summary>
public class TaskClassLangValue : LangValueType
{
    private static readonly TaskClassLangValue Instance = new();

    /// <summary>
    /// 获取 Task 类的全局单例
    /// </summary>
    public static TaskClassLangValue GetInstance() => Instance;

    public override string TypeToString() => "TaskClass";

    public override string ToDisplayString() => "Task";

    /// <summary>
    /// 外部管理器，用于访问外部变量
    /// </summary>
    public VariateManager? ExternalManager { get; set; }

    public override LangValueType Dot(LangExpression dotExpression)
    {
        // 处理 Task.WhenAll(...) 形式的调用
        if (dotExpression is Instance instance)
        {
            var methodName = instance.Id.IdName;

            // 返回一个包装函数,用于调用静态方法
            Func<List<LangValueType>, SourcePosition, LangValueType>? method = methodName switch
            {
                "WhenAll" => WhenAll,
                "WhenAny" => WhenAny,
                "Delay" => Delay,
                _ => null
            };

            if (method == null)
            {
                throw new AttributeError(dotExpression.Position, methodName, "Task");
            }

            // 使用 ExternalManager 执行参数
            var currentManager = ExternalManager ?? throw new InvalidOperationError(dotExpression.Position, "未设置外部管理器");
            var args = instance.Ids.Select(id => id.Run(currentManager)).ToList();
            return method(args, instance.Position);
        }

        // 处理 Task.WhenAll 形式的访问（不带调用）
        if (dotExpression is ClassMemberId memberId)
        {
            var methodName = memberId.IdName;

            // 返回一个包装函数,用于调用静态方法
            return methodName switch
            {
                "WhenAll" => new TaskStaticMethodWrapper("WhenAll", WhenAll),
                "WhenAny" => new TaskStaticMethodWrapper("WhenAny", WhenAny),
                "Delay" => new TaskStaticMethodWrapper("Delay", Delay),
                _ => throw new AttributeError(dotExpression.Position, methodName, "Task")
            };
        }

        throw new AttributeError(dotExpression.Position,
            dotExpression.ToString() ?? "unknown", "Task");
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

        var tasks = new List<TaskLangValue>();
        foreach (var item in list.GetItems())
        {
            if (item is not TaskLangValue task)
            {
                var tempNode = new NullLangValue(position);
                throw new TypeError(tempNode, "Task", item.TypeToString());
            }

            tasks.Add(task);
        }

        return TaskLangValue.WhenAll(tasks, position);
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

        var tasks = new List<TaskLangValue>();
        foreach (var item in list.GetItems())
        {
            if (item is not TaskLangValue task)
            {
                var tempNode = new NullLangValue(position);
                throw new TypeError(tempNode, "Task", item.TypeToString());
            }

            tasks.Add(task);
        }

        return TaskLangValue.WhenAny(tasks, position);
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

        return TaskLangValue.Delay(delayMs.Value, CancellationToken.None, position);
    }
}

/// <summary>
/// Task 静态方法的包装器
/// </summary>
public class TaskStaticMethodWrapper(
    string methodName,
    Func<List<LangValueType>, SourcePosition, LangValueType> method)
    : LangValueType
{
    public override string TypeToString() => "TaskStaticMethod";

    public override string ToDisplayString() => $"Task.{methodName}";

    /// <summary>
    /// 执行静态方法
    /// </summary>
    public LangValueType Invoke(List<LangValueType> args, SourcePosition position)
    {
        return method(args, position);
    }
}