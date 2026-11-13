using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.StaticValues;

/// <summary>
/// TaskScheduler 类的全局对象
/// </summary>
public partial class TaskSchedulerClassLangValue : LangValueType
{
    private static readonly TaskSchedulerClassLangValue Instance = new();

    /// <summary>
    /// 获取 TaskScheduler 类的全局单例
    /// </summary>
    public static TaskSchedulerClassLangValue GetInstance() => Instance;

    public override string TypeToString() => "TaskSchedulerClass";

    public override string ToDisplayString() => "TaskScheduler";

    /// <summary>
    /// 外部管理器，用于访问外部变量
    /// </summary>
    public VariateManager? ExternalManager { get; set; }

    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        if (dotExpression is LangId id)
        {
            var propertyName = id.IdName;
            
            return propertyName switch
            {
                "Default" => new TaskSchedulerLangValue(TaskScheduler.Default, id.Position),
                _ => throw new AttributeError(dotExpression.Position, propertyName, "TaskScheduler")
            };
        }

        throw new AttributeError(dotExpression.Position,
            dotExpression.ToString() ?? "unknown", "TaskScheduler");
    }
}

/// <summary>
/// TaskScheduler 实例值类型
/// </summary>
public partial class TaskSchedulerLangValue(TaskScheduler scheduler, SourcePosition position = default)
    : LangValueType(position)
{
    public override string TypeToString() => "TaskScheduler";

    public override string ToDisplayString() => "TaskScheduler";

    /// <summary>
    /// 获取底层的 TaskScheduler 对象
    /// </summary>
    public override object GetValue() => scheduler;

    /// <summary>
    /// 外部管理器，用于访问外部变量
    /// </summary>
    public VariateManager? ExternalManager { get; set; }
}