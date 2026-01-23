using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Bytecode.VM;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Bytecode.Generators;

/// <summary>
/// 异步流对象（字节码虚拟机模式）
/// 表示一个异步流，是 BytecodeAsyncGeneratorLangValue 的包装类
/// </summary>
public class BytecodeAsyncStreamLangValue(int asyncGeneratorId, VirtualMachine vm, SourcePosition position = default)
    : LangValueType(position), IAsyncEnumerable<LangValueType>
{
    /// <summary>内部的异步生成器</summary>
    private readonly BytecodeAsyncGeneratorLangValue _asyncGenerator = new(asyncGeneratorId, vm, position);

    /// <summary>虚拟机引用</summary>
    private readonly VirtualMachine _vm = vm;

    /// <summary>当前值</summary>
    public LangValueType? Current => _asyncGenerator.Current;

    /// <summary>异步流是否已完成</summary>
    public bool IsCompleted => _asyncGenerator.IsCompleted;

    /// <summary>异步流状态</summary>
    public BytecodeAsyncGeneratorLangValue.AsyncGeneratorState State => _asyncGenerator.State;

    /// <summary>
    /// 异步推进流到下一个值
    /// </summary>
    public async Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        return await _asyncGenerator.MoveNextAsync(cancellationToken);
    }

    /// <summary>
    /// 同步版本的MoveNext（阻塞等待）
    /// </summary>
    public bool MoveNext()
    {
        return _asyncGenerator.MoveNext();
    }

    /// <summary>
    /// 执行异步流，返回下一个值（同步等待）
    /// </summary>
    public override LangValueType Run(VariateManager manager)
    {
        if (MoveNext())
        {
            return Current ?? new VoidLangValue();
        }
        return new VoidLangValue();
    }

    /// <summary>
    /// 取消异步流的执行
    /// </summary>
    public void Cancel()
    {
        _asyncGenerator.Cancel();
    }

    /// <summary>
    /// 重置异步流状态
    /// </summary>
    public void Reset()
    {
        _asyncGenerator.Reset();
    }

    public override string ToString() => $"AsyncStream({_asyncGenerator.AsyncGeneratorId})";

    public override TResult Accept<TResult>(AST.Visitor.IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("AsyncStreamLangValue 不支持 Visitor 模式遍历");
    }

    public override object GetValue() => this;

    public override Type OutputType(LocalManager local) => typeof(BytecodeAsyncStreamLangValue);

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotSupportedException("异步流对象不支持IL代码生成");
    }

    // IAsyncEnumerable实现
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
        _asyncGenerator.Dispose();
    }

    // 不支持的操作
    public override LangValueType Plus(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "异步流对象不支持加法操作");

    public override LangValueType Minus(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "异步流对象不支持减法操作");

    public override LangValueType Times(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "异步流对象不支持乘法操作");

    public override LangValueType Divide(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "异步流对象不支持除法操作");

    public override LangValueType Mod(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "异步流对象不支持取模操作");

    public override LangValueType Power(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "异步流对象不支持幂运算");

    public override bool Less(LangValueType? otherValue)
        => throw new InvalidOperationError(this, "异步流对象不支持比较操作");

    public override bool Greater(LangValueType? otherValue)
        => throw new InvalidOperationError(this, "异步流对象不支持比较操作");

    public override bool LessEqual(LangValueType? otherValue)
        => throw new InvalidOperationError(this, "异步流对象不支持比较操作");

    public override bool GreaterEqual(LangValueType? otherValue)
        => throw new InvalidOperationError(this, "异步流对象不支持比较操作");

    public override bool Equal(LangValueType? otherValueType)
        => otherValueType is BytecodeAsyncStreamLangValue other &&
           _asyncGenerator.AsyncGeneratorId == other._asyncGenerator.AsyncGeneratorId;

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
        => throw new TypeError(this, "异步流对象不支持类型转换");
}
