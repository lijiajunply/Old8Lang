using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

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
        // 验证参数：CancellationTokenSource() 不接受参数
        if (args.Count != 0)
        {
            throw new ArgumentError(position, $"CancellationTokenSource 构造函数期望 0 个参数，但提供了 {args.Count} 个");
        }

        // 创建并返回新的 CancellationTokenSource 实例
        return new CancellationTokenSourceLangValue(position);
    }

    public override string ToDisplayString() => "CancellationTokenSource";
}
