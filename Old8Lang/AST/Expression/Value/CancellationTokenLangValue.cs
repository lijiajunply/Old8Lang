using Old8Lang.AST;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.LangParser;
using System.Reflection.Emit;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 取消令牌值类型
/// 表示一个用于取消异步操作的令牌
/// </summary>
public class CancellationTokenLangValue : LangValueType
{
    private readonly CancellationToken _token;

    /// <summary>
    /// 获取底层 CancellationToken 对象
    /// </summary>
    public CancellationToken Token => _token;

    /// <summary>
    /// 检查是否已请求取消
    /// </summary>
    public bool IsCancellationRequested => _token.IsCancellationRequested;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CancellationTokenLangValue(CancellationToken token, SourcePosition position = default)
        : base(position)
    {
        _token = token;
    }

    /// <summary>
    /// 注册取消回调
    /// </summary>
    public void Register(Action callback)
    {
        _token.Register(callback);
    }

    /// <summary>
    /// 抛出取消异常（如果已请求取消）
    /// </summary>
    public void ThrowIfCancellationRequested()
    {
        _token.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// 获取底层 CancellationToken 对象
    /// </summary>
    public override object GetValue() => _token;

    /// <summary>
    /// 类型字符串表示
    /// </summary>
    public override string TypeToString() => "CancellationToken";

    /// <summary>
    /// 值的字符串表示
    /// </summary>
    public override string ToString()
    {
        return IsCancellationRequested ? "CancellationToken(Cancelled)" : "CancellationToken(Active)";
    }

    /// <summary>
    /// Run 方法：返回自身
    /// </summary>
    public override LangValueType Run(VariateManager manager) => this;

    /// <summary>
    /// 生成 IL 代码（编译器模式暂不支持）
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedError(
            Position,
            "编译模式暂不支持 CancellationToken 类型"
        );
    }

    /// <summary>
    /// 获取 .NET 类型
    /// </summary>
    public override Type? OutputType(LocalManager local)
    {
        return typeof(CancellationToken);
    }
}