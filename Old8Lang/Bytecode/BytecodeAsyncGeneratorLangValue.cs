using System.Collections;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Bytecode;

/// <summary>
/// 异步生成器对象（字节码虚拟机模式）
/// 表示一个可以暂停和恢复执行的异步生成器函数
/// 类似于 C# 的 IAsyncEnumerable&lt;T&gt;
/// </summary>
public class BytecodeAsyncGeneratorLangValue : LangValueType, IAsyncEnumerable<LangValueType>
{
    /// <summary>异步生成器ID</summary>
    public int AsyncGeneratorId { get; }

    /// <summary>虚拟机引用</summary>
    private readonly VirtualMachine _vm;

    /// <summary>当前值</summary>
    public LangValueType? Current { get; private set; }

    /// <summary>异步生成器是否已完成</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>取消令牌源</summary>
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>异步生成器状态</summary>
    public AsyncGeneratorState State { get; private set; } = AsyncGeneratorState.Suspended;

    /// <summary>
    /// 异步生成器状态枚举
    /// </summary>
    public enum AsyncGeneratorState
    {
        Suspended,  // 已暂停，等待下一个值
        Running,    // 正在执行
        Completed   // 已完成
    }

    public BytecodeAsyncGeneratorLangValue(int asyncGeneratorId, VirtualMachine vm, SourcePosition position = default)
        : base(position)
    {
        AsyncGeneratorId = asyncGeneratorId;
        _vm = vm;
        IsCompleted = false;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// 异步推进生成器到下一个yield点
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果成功yield返回true，如果生成器已完成返回false</returns>
    public async Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (IsCompleted)
            return false;

        State = AsyncGeneratorState.Running;

        try
        {
            // 调用虚拟机的ResumeAsyncGenerator方法来恢复异步生成器执行
            var result = await _vm.ResumeAsyncGeneratorAsync(AsyncGeneratorId, cancellationToken);

            if (result != null)
            {
                Current = result;
                State = AsyncGeneratorState.Suspended;
                return true;
            }
            else
            {
                IsCompleted = true;
                State = AsyncGeneratorState.Completed;
                Current = null;
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            IsCompleted = true;
            State = AsyncGeneratorState.Completed;
            throw;
        }
    }

    /// <summary>
    /// 同步版本的MoveNext（阻塞等待）
    /// </summary>
    public bool MoveNext()
    {
        return MoveNextAsync(_cancellationTokenSource?.Token ?? CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// 取消异步生成器的执行
    /// </summary>
    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// 重置生成器状态
    /// </summary>
    public void Reset()
    {
        State = AsyncGeneratorState.Suspended;
        Current = null;
        IsCompleted = false;

        // 创建新的取消令牌源
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public override string ToString() => $"AsyncGenerator({AsyncGeneratorId})";

    public override TResult Accept<TResult>(AST.Visitor.IVisitor<TResult> visitor)
    {
        // AsyncGeneratorLangValue 是运行时值，不参与 Visitor 遍历
        throw new NotSupportedException("AsyncGeneratorLangValue 不支持 Visitor 模式遍历");
    }

    public override object GetValue() => this;

    public override Type OutputType(LocalManager local) => typeof(BytecodeAsyncGeneratorLangValue);

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotSupportedException("异步生成器对象不支持IL代码生成");
    }

    // IAsyncEnumerable实现，支持异步for-in循环
    public async IAsyncEnumerator<LangValueType> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        while (await MoveNextAsync(cancellationToken))
        {
            if (Current != null)
                yield return Current;
        }
    }

    // 同步枚举器（向后兼容）
    public IEnumerator<LangValueType> GetEnumerator()
    {
        while (MoveNext())
        {
            if (Current != null)
                yield return Current;
        }
    }

    // 释放资源
    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }

    // 不支持的操作
    public override LangValueType Plus(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "异步生成器对象不支持加法操作");

    public override LangValueType Minus(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "异步生成器对象不支持减法操作");

    public override LangValueType Times(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "异步生成器对象不支持乘法操作");

    public override LangValueType Divide(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "异步生成器对象不支持除法操作");

    public override LangValueType Mod(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "异步生成器对象不支持取模操作");

    public override LangValueType Power(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "异步生成器对象不支持幂运算");

    public override bool Less(LangValueType? otherValue)
        => throw new InvalidOperationError(this, "异步生成器对象不支持比较操作");

    public override bool Greater(LangValueType? otherValue)
        => throw new InvalidOperationError(this, "异步生成器对象不支持比较操作");

    public override bool LessEqual(LangValueType? otherValue)
        => throw new InvalidOperationError(this, "异步生成器对象不支持比较操作");

    public override bool GreaterEqual(LangValueType? otherValue)
        => throw new InvalidOperationError(this, "异步生成器对象不支持比较操作");

    public override bool Equal(LangValueType? otherValueType)
        => otherValueType is BytecodeAsyncGeneratorLangValue other && AsyncGeneratorId == other.AsyncGeneratorId;

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
        => throw new TypeError(this, "异步生成器对象不支持类型转换");
}
