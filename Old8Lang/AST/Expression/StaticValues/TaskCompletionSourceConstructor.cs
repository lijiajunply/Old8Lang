using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.StaticValues;

/// <summary>
/// TaskCompletionSource 构造函数的全局对象
/// 允许在 Old8Lang 中创建 TaskCompletionSource 实例
/// 作为可调用的构造函数包装器
/// </summary>
public class TaskCompletionSourceConstructor : TaskStaticMethodWrapper
{
    private static readonly TaskCompletionSourceConstructor Instance = new();

    /// <summary>
    /// 获取 TaskCompletionSource 构造函数的全局单例
    /// </summary>
    public static TaskCompletionSourceConstructor GetInstance() => Instance;

    private TaskCompletionSourceConstructor()
        : base("TaskCompletionSource", CreateInstance)
    {
    }

    private static LangValueType CreateInstance(List<LangValueType> args, SourcePosition position)
    {
        // 验证参数：TaskCompletionSource() 不接受参数
        if (args.Count != 0)
        {
            throw new ArgumentError(position, $"TaskCompletionSource 构造函数期望 0 个参数，但提供了 {args.Count} 个");
        }

        // 创建并返回新的 TaskCompletionSource 实例
        return new TaskCompletionSourceLangValue(position);
    }

    public override string ToDisplayString() => "TaskCompletionSource";
}
