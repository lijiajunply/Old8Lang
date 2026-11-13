using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.StaticValues;

/// <summary>
/// CancellationTokenSource 构造函数的全局对象
/// 允许在 Old8Lang 中创建 CancellationTokenSource 实例
/// 作为可调用的构造函数包装器
/// </summary>
public class CancellationTokenSourceConstructor : TaskStaticMethodWrapper
{
    private static readonly CancellationTokenSourceConstructor Instance = new();

    /// <summary>
    /// 获取 CancellationTokenSource 构造函数的全局单例
    /// </summary>
    public static CancellationTokenSourceConstructor GetInstance() => Instance;

    private CancellationTokenSourceConstructor()
        : base("CancellationTokenSource", CreateInstance)
    {
    }

    private static LangValueType CreateInstance(List<LangValueType> args, SourcePosition position)
    {
        // 验证参数：CancellationTokenSource() 或 CancellationTokenSource(timeout)
        if (args.Count > 1)
        {
            throw new ArgumentError(position, $"CancellationTokenSource 构造函数期望 0 或 1 个参数，但提供了 {args.Count} 个");
        }

        // 无参构造函数
        if (args.Count == 0)
        {
            return new CancellationTokenSourceLangValue(position);
        }

        // 带超时参数的构造函数
        var timeoutArg = args[0];
        if (timeoutArg is not IntLangValue intValue)
        {
            throw new ArgumentError(position, "CancellationTokenSource 构造函数的超时参数必须是整数类型");
        }

        // 创建带超时的 CancellationTokenSource
        return new CancellationTokenSourceLangValue(position, intValue.Value);
    }

    public override string ToDisplayString() => "CancellationTokenSource";
}
