using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.StaticValues;

/// <summary>
/// 取消令牌源值类型
/// 表示一个可以生成取消令牌的源，用于取消异步操作
/// </summary>
public partial class CancellationTokenSourceLangValue : LangValueType
{
    private readonly CancellationTokenSource _cts;

    /// <summary>
    /// 获取取消令牌
    /// </summary>
    public CancellationTokenLangValue Token => new(_cts.Token, Position);

    /// <summary>
    /// 构造函数（无参）
    /// </summary>
    public CancellationTokenSourceLangValue(SourcePosition position = default)
        : base(position)
    {
        _cts = new CancellationTokenSource();
    }

    /// <summary>
    /// 构造函数（带超时时间）
    /// </summary>
    /// <param name="position">源代码位置</param>
    /// <param name="millisecondsDelay">超时时间（毫秒）</param>
    public CancellationTokenSourceLangValue(SourcePosition position, int millisecondsDelay)
        : base(position)
    {
        _cts = new CancellationTokenSource(millisecondsDelay);
    }

    /// <summary>
    /// 取消关联的异步操作
    /// </summary>
    public void Cancel()
    {
        _cts.Cancel();
    }

    /// <summary>
    /// 取消关联的异步操作（带延迟）
    /// </summary>
    public void CancelAfter(int delayMs)
    {
        _cts.CancelAfter(delayMs);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _cts.Dispose();
    }

    /// <summary>
    /// 获取底层 CancellationTokenSource 对象
    /// </summary>
    public override object GetValue() => _cts;

    /// <summary>
    /// 类型字符串表示
    /// </summary>
    public override string TypeToString() => "CancellationTokenSource";

    /// <summary>
    /// 值的字符串表示
    /// </summary>
    public override string ToString()
    {
        return "CancellationTokenSource";
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
                "Token" => Token,
                "IsCancellationRequested" => new BoolLangValue(_cts.IsCancellationRequested, Position),
                _ => throw new AttributeError(dotExpression.Position, propertyName, "CancellationTokenSource")
            };
        }

        // 处理方法调用
        if (dotExpression is Instance instance)
        {
            var methodName = instance.Id.IdName;

            return methodName switch
            {
                "Cancel" => CallCancel(instance.Ids),
                "CancelAfter" => CallCancelAfter(instance.Ids, manager),
                "Dispose" => CallDispose(instance.Ids),
                _ => throw new AttributeError(instance.Position, methodName, "CancellationTokenSource")
            };
        }

        return base.Dot(dotExpression, manager);
    }

    private LangValueType CallCancel(List<LangExpression> args)
    {
        if (args.Count != 0)
        {
            throw new ArgumentError(Position, $"Cancel 期望 0 个参数，但提供了 {args.Count} 个");
        }

        Cancel();
        return new VoidLangValue(Position);
    }

    private LangValueType CallCancelAfter(List<LangExpression> args, VariateManager manager)
    {
        if (args.Count != 1)
        {
            throw new ArgumentError(Position, $"CancelAfter 期望 1 个参数，但提供了 {args.Count} 个");
        }

        var delayArg = args[0].Run(manager);
        if (delayArg is not IntLangValue delayMs)
        {
            throw new TypeError(args[0], "int", delayArg.TypeToString());
        }

        CancelAfter(delayMs.Value);
        return new VoidLangValue(Position);
    }

    private LangValueType CallDispose(List<LangExpression> args)
    {
        if (args.Count != 0)
        {
            throw new ArgumentError(Position, $"Dispose 期望 0 个参数，但提供了 {args.Count} 个");
        }

        Dispose();
        return new VoidLangValue(Position);
    }

    /// <summary>
    /// 生成 IL 代码（编译器模式暂不支持）
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedError(
            Position,
            "编译模式暂不支持 CancellationTokenSource 类型"
        );
    }

    /// <summary>
    /// 获取 .NET 类型
    /// </summary>
    public override Type OutputType(LocalManager local)
    {
        return typeof(CancellationTokenSource);
    }
}