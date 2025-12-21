using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.StaticValues;

/// <summary>
/// TaskCompletionSource 值类型
/// 表示一个可以手动控制完成的 Task
/// </summary>
public class TaskCompletionSourceLangValue : LangValueType
{
    private readonly TaskCompletionSource<LangValueType> Tcs;

    /// <summary>
    /// 获取关联的 Task
    /// </summary>
    public TaskLangValue Task
    {
        get
        {
            field ??= new TaskLangValue(Tcs.Task, CancellationToken.None, Position);

            return field;
        }
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskCompletionSourceLangValue(SourcePosition position = default)
        : base(position)
    {
        Tcs = new TaskCompletionSource<LangValueType>();
    }

    /// <summary>
    /// 设置任务结果（成功完成）
    /// </summary>
    public bool SetResult(LangValueType result)
    {
        return Tcs.TrySetResult(result);
    }

    /// <summary>
    /// 设置任务异常（失败）
    /// </summary>
    public bool SetException(Exception exception)
    {
        return Tcs.TrySetException(exception);
    }

    /// <summary>
    /// 设置任务为取消状态
    /// </summary>
    public bool SetCanceled()
    {
        return Tcs.TrySetCanceled();
    }

    /// <summary>
    /// 获取底层 TaskCompletionSource 对象
    /// </summary>
    public override object GetValue() => Tcs;

    /// <summary>
    /// 类型字符串表示
    /// </summary>
    public override string TypeToString() => "TaskCompletionSource";

    /// <summary>
    /// 值的字符串表示
    /// </summary>
    public override string ToString()
    {
        var taskStatus = Tcs.Task.Status;
        return $"TaskCompletionSource(Task Status: {taskStatus})";
    }

    /// <summary>
    /// Run 方法：返回自身
    /// </summary>
    public override LangValueType Run(VariateManager manager) => this;

    /// <summary>
    /// Dot 方法：支持属性访问和方法调用
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        // 处理属性访问
        if (dotExpression is LangId id)
        {
            var propertyName = id.IdName;

            return propertyName switch
            {
                "Task" => Task,
                _ => throw new AttributeError(dotExpression.Position, propertyName, "TaskCompletionSource")
            };
        }

        // 处理方法调用
        if (dotExpression is Instance instance)
        {
            var methodName = instance.Id.IdName;

            return methodName switch
            {
                "SetResult" => CallSetResult(instance.Ids, manager),
                "SetException" => CallSetException(instance.Ids, manager),
                "SetCanceled" => CallSetCanceled(instance.Ids, manager),
                "TrySetResult" => CallTrySetResult(instance.Ids, manager),
                "TrySetException" => CallTrySetException(instance.Ids, manager),
                "TrySetCanceled" => CallTrySetCanceled(instance.Ids, manager),
                _ => throw new AttributeError(instance.Position, methodName, "TaskCompletionSource")
            };
        }

        return base.Dot(dotExpression, manager);
    }

    private LangValueType CallSetResult(List<LangExpression> args, VariateManager manager)
    {
        if (args.Count != 1)
        {
            throw new ArgumentError(Position, $"SetResult 期望 1 个参数，但提供了 {args.Count} 个");
        }

        var resultValue = args[0].Run(manager);
        SetResult(resultValue);
        return new VoidLangValue(Position);
    }

    private LangValueType CallSetException(List<LangExpression> args, VariateManager manager)
    {
        if (args.Count != 1)
        {
            throw new ArgumentError(Position, $"SetException 期望 1 个参数，但提供了 {args.Count} 个");
        }

        var exceptionArg = args[0].Run(manager);
        string exceptionMessage;

        if (exceptionArg is StringLangValue stringValue)
        {
            exceptionMessage = stringValue.Value;
        }
        else
        {
            exceptionMessage = exceptionArg.ToDisplayString();
        }

        SetException(new Exception(exceptionMessage));
        return new VoidLangValue(Position);
    }

    private LangValueType CallSetCanceled(List<LangExpression> args, VariateManager manager)
    {
        if (args.Count != 0)
        {
            throw new ArgumentError(Position, $"SetCanceled 期望 0 个参数，但提供了 {args.Count} 个");
        }

        SetCanceled();
        return new VoidLangValue(Position);
    }

    private LangValueType CallTrySetResult(List<LangExpression> args, VariateManager manager)
    {
        if (args.Count != 1)
        {
            throw new ArgumentError(Position, $"TrySetResult 期望 1 个参数，但提供了 {args.Count} 个");
        }

        var resultValue = args[0].Run(manager);
        var success = SetResult(resultValue);
        return new BoolLangValue(success, Position);
    }

    private LangValueType CallTrySetException(List<LangExpression> args, VariateManager manager)
    {
        if (args.Count != 1)
        {
            throw new ArgumentError(Position, $"TrySetException 期望 1 个参数，但提供了 {args.Count} 个");
        }

        var exceptionArg = args[0].Run(manager);
        string exceptionMessage;

        if (exceptionArg is StringLangValue stringValue)
        {
            exceptionMessage = stringValue.Value;
        }
        else
        {
            exceptionMessage = exceptionArg.ToDisplayString();
        }

        var success = SetException(new Exception(exceptionMessage));
        return new BoolLangValue(success, Position);
    }

    private LangValueType CallTrySetCanceled(List<LangExpression> args, VariateManager manager)
    {
        if (args.Count != 0)
        {
            throw new ArgumentError(Position, $"TrySetCanceled 期望 0 个参数，但提供了 {args.Count} 个");
        }

        var success = SetCanceled();
        return new BoolLangValue(success, Position);
    }

    /// <summary>
    /// 生成 IL 代码（编译器模式暂不支持）
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedError(
            Position,
            "编译模式暂不支持 TaskCompletionSource 类型"
        );
    }

    /// <summary>
    /// 获取 .NET 类型
    /// </summary>
    public override Type OutputType(LocalManager local)
    {
        return typeof(TaskCompletionSource<LangValueType>);
    }
}