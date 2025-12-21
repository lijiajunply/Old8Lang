using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.StaticValues;

/// <summary>
/// TaskFactory 类的全局对象
/// </summary>
public class TaskFactoryClassLangValue : LangValueType
{
    public override string TypeToString() => "TaskFactoryClass";

    public override string ToDisplayString() => "TaskFactory";

    /// <summary>
    /// 外部管理器，用于访问外部变量
    /// </summary>
    public VariateManager? ExternalManager { get; set; }

    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        if (dotExpression is Instance instance)
        {
            var methodName = instance.Id.IdName;

            // 返回一个包装函数,用于调用静态方法
            Func<List<LangValueType>, SourcePosition, LangValueType>? method = methodName switch
            {
                "StartNew" => StartNew,
                _ => null
            };

            if (method == null)
            {
                throw new AttributeError(dotExpression.Position, methodName, "TaskFactory");
            }

            // 使用 ExternalManager 或传入的 manager 执行参数
            var currentManager = ExternalManager ?? manager;
            var args = instance.Ids.Select(id => id.Run(currentManager)).ToList();
            return method(args, instance.Position);
        }

        if (dotExpression is ClassMemberId memberId)
        {
            var methodName = memberId.IdName;

            return methodName switch
            {
                "StartNew" => new TaskFactoryStaticMethodWrapper("StartNew", StartNew),
                _ => throw new AttributeError(dotExpression.Position, methodName, "TaskFactory")
            };
        }

        throw new AttributeError(dotExpression.Position,
            dotExpression.ToString() ?? "unknown", "TaskFactory");
    }

    /// <summary>
    /// StartNew 静态方法实现
    /// </summary>
    private static LangValueType StartNew(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 1 or > 2)
        {
            throw new ArgumentError(position,
                $"StartNew 期望 1-2 个参数,但提供了 {args.Count} 个");
        }

        if (args[0] is not FuncLangValue funcValue)
        {
            var tempNode = new NullLangValue(position);
            throw new TypeError(tempNode, "function", args[0].TypeToString());
        }

        // 第二个参数是可选的 TaskScheduler (当前实现忽略，因为 TaskLangValue.Run 不支持)
        if (args.Count == 2)
        {
            if (args[1] is not TaskSchedulerLangValue)
            {
                var tempNode = new NullLangValue(position);
                throw new TypeError(tempNode, "TaskScheduler", args[1].TypeToString());
            }
            // 注意：当前实现忽略 TaskScheduler 参数，因为 TaskLangValue.Run 使用 Task.Run
            // 如果需要真正支持 TaskScheduler，需要使用 Task.Factory.StartNew
        }

        return TaskLangValue.Run(funcValue, System.Threading.CancellationToken.None, position);
    }
}

/// <summary>
/// TaskFactory 静态方法的包装器
/// </summary>
public class TaskFactoryStaticMethodWrapper(
    string methodName,
    Func<List<LangValueType>, SourcePosition, LangValueType> method)
    : LangValueType
{
    public override string TypeToString() => "TaskFactoryStaticMethod";

    public override string ToDisplayString() => $"TaskFactory.{methodName}";

    /// <summary>
    /// 执行静态方法
    /// </summary>
    public LangValueType Invoke(List<LangValueType> args, SourcePosition position)
    {
        return method(args, position);
    }

    /// <summary>
    /// 外部管理器，用于访问外部变量
    /// </summary>
    public VariateManager? ExternalManager { get; set; }
}