using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Expression.StaticValues;

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
    /// 生成 IL 代码，返回 Task 类型本身
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 对于 Task 类静态方法，我们不需要加载实例
        // 直接返回 Task 类型本身
        ilGenerator.Emit(OpCodes.Ldtoken, typeof(Task));
        ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")!);
    }

    /// <summary>
    /// 获取输出类型
    /// </summary>
    public override Type? OutputType(LocalManager local)
    {
        return typeof(Type);
    }

    /// <summary>
    /// 外部管理器，用于访问外部变量
    /// </summary>
    public VariateManager? ExternalManager { get; set; }

    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
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
                "FromResult" => FromResult,
                "Run" => Run,
                _ => null
            };

            if (method == null)
            {
                throw new AttributeError(dotExpression.Position, methodName, "Task");
            }

            // 使用 ExternalManager 或传入的 manager 执行参数
            var currentManager = ExternalManager ?? manager;
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
                "FromResult" => new TaskStaticMethodWrapper("FromResult", FromResult),
                "Run" => new TaskStaticMethodWrapper("Run", Run),
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
    
    /// <summary>
    /// FromResult 静态方法实现
    /// </summary>
    private static LangValueType FromResult(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 1)
        {
            throw new ArgumentError(position,
                $"FromResult 期望 1 个参数,但提供了 {args.Count} 个");
        }

        var resultValue = args[0];
        return TaskLangValue.FromResult(resultValue, position);
    }
    
    /// <summary>
    /// Run 静态方法实现
    /// </summary>
    private static LangValueType Run(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 1)
        {
            throw new ArgumentError(position,
                $"Run 期望 1 个参数,但提供了 {args.Count} 个");
        }

        if (args[0] is not FuncLangValue funcValue)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "function", args[0].TypeToString());
        }

        return TaskLangValue.Run(funcValue, CancellationToken.None, position);
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

    /// <summary>
    /// 生成 IL 代码，返回对应Task静态方法的委托
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 根据方法名生成对应的Task静态方法委托
        switch (methodName)
        {
            case "WhenAll":
                // 获取Task.WhenAll方法
                var whenAllMethod = typeof(Task).GetMethod("WhenAll", new[] { typeof(IEnumerable<Task<object>>) })!;
                ilGenerator.Emit(OpCodes.Ldnull);
                break;
            case "WhenAny":
                // 获取Task.WhenAny方法
                var whenAnyMethod = typeof(Task).GetMethod("WhenAny", new[] { typeof(IEnumerable<Task<object>>) })!;
                ilGenerator.Emit(OpCodes.Ldnull);
                break;
            case "Delay":
                // 获取Task.Delay方法
                var delayMethod = typeof(Task).GetMethod("Delay", new[] { typeof(int) })!;
                ilGenerator.Emit(OpCodes.Ldnull);
                break;
            default:
                throw new InvalidOperationError(this.Position, $"不支持的Task静态方法: {methodName}");
        }
    }

    /// <summary>
    /// 获取输出类型
    /// </summary>
    public override Type? OutputType(LocalManager local)
    {
        return typeof(Delegate);
    }
}