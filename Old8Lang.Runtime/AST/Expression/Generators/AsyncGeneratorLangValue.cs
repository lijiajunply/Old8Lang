using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Generators;

/// <summary>
/// 异步生成器对象，用于表示异步生成器函数的实例，实现ILangList接口以支持异步迭代
/// 类似于 C# 的 IAsyncEnumerable&lt;T&gt;
/// </summary>
public partial class AsyncGeneratorLangValue : LangValueType, ILangList
{
    /// <summary>
    /// 异步函数引用
    /// </summary>
    public AsyncFuncLangValue AsyncFunc { get; init; }

    /// <summary>
    /// 异步生成器状态机
    /// </summary>
    internal AsyncGeneratorStateMachine? StateMachine { get; set; }

    /// <summary>
    /// 生成器当前状态
    /// </summary>
    public AsyncGeneratorState State { get; set; } = AsyncGeneratorState.Suspended;

    /// <summary>
    /// 生成器迭代器的下一个值
    /// </summary>
    public LangValueType? NextValue { get; set; }

    /// <summary>
    /// 生成器函数的参数值
    /// </summary>
    internal Dictionary<string, LangValueType> ParameterValues { get; } = new();

    /// <summary>
    /// 取消令牌源，用于取消异步操作
    /// </summary>
    internal CancellationTokenSource? CancellationTokenSource { get; set; }

    /// <summary>
    /// 异步生成器状态枚举
    /// </summary>
    public enum AsyncGeneratorState
    {
        Suspended, // 已暂停，等待下一个值
        Running, // 正在执行
        Completed // 已完成
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="asyncFunc">异步函数引用</param>
    /// <param name="position">源代码位置</param>
    public AsyncGeneratorLangValue(AsyncFuncLangValue asyncFunc, SourcePosition position = default) : base(position)
    {
        AsyncFunc = asyncFunc;
        CancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// 设置生成器函数的参数值
    /// </summary>
    /// <param name="paramName">参数名称</param>
    /// <param name="value">参数值</param>
    public void SetParameter(string paramName, LangValueType value)
    {
        ParameterValues[paramName] = value;
    }



    /// <summary>
    /// 取消异步生成器的执行
    /// </summary>
    public void Cancel()
    {
        CancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// 重置生成器状态
    /// </summary>
    public void Reset()
    {
        State = AsyncGeneratorState.Suspended;
        NextValue = null;
        StateMachine?.Reset();

        // 创建新的取消令牌源
        CancellationTokenSource?.Dispose();
        CancellationTokenSource = new CancellationTokenSource();
    }


    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns>异步生成器的字符串表示</returns>
    public override string ToString() => $"AsyncGenerator({AsyncFunc.Id?.IdName ?? "anonymous"})";

    /// <summary>
    /// 获取生成器的所有项
    /// </summary>
    /// <returns>生成器项的枚举</returns>
    public IEnumerable<LangValueType> GetItems()
    {
        // 异步生成器的迭代逻辑由AsyncForInStatement处理，这里只返回空枚举
        // 避免在迭代过程中影响生成器的状态
        yield break;
    }

    /// <summary>
    /// 获取生成器的长度
    /// </summary>
    /// <returns>生成器的长度，-1表示未知长度</returns>
    public int GetLength()
    {
        // 异步生成器的长度通常是未知的，返回-1表示未知长度
        return -1;
    }

    /// <summary>
    /// 对生成器进行切片（带步长）
    /// </summary>
    /// <param name="start">起始索引</param>
    /// <param name="end">结束索引</param>
    /// <param name="step">步长</param>
    /// <returns>切片后的生成器</returns>
    public LangValueType Slice(int start, int end, int step)
    {
        // 异步生成器不支持切片
        throw new NotSupportedException("异步生成器不支持切片操作");
    }

    /// <summary>
    /// 设置生成器中指定索引的值
    /// </summary>
    /// <param name="index">索引</param>
    /// <param name="value">值</param>
    /// <exception cref="NotSupportedException">异步生成器不支持设置值</exception>
    public void Set(LangValueType index, LangValueType value)
    {
        // 异步生成器是只读的，不支持设置值
        throw new NotSupportedException("异步生成器不支持设置值");
    }

    /// <summary>
    /// 切片赋值操作
    /// </summary>
    /// <exception cref="NotSupportedException">异步生成器不支持切片赋值</exception>
    public void SetSlice(int start, int end, IEnumerable<LangValueType> values)
    {
        throw new NotSupportedException("异步生成器不支持切片赋值操作");
    }

    /// <summary>
    /// 检查值是否在生成器中
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <returns>如果值在生成器中则返回true，否则返回false</returns>
    public bool In(LangValueType value)
    {
        // 迭代生成器，检查是否包含指定值
        return GetItems().Any(item => item.ToString() == value.ToString());
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        CancellationTokenSource?.Dispose();
    }
}

/// <summary>
/// 空的异步可枚举实现，用于 IL 模式下的异步生成器
/// </summary>
public class EmptyAsyncEnumerable : IAsyncEnumerable<object>
{
    /// <summary>
    /// 获取异步枚举器
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步枚举器</returns>
    public IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new EmptyAsyncEnumerator();
    }

    /// <summary>
    /// 空的异步枚举器实现
    /// </summary>
    internal class EmptyAsyncEnumerator : IAsyncEnumerator<object>
    {
        /// <summary>
        /// 当前值
        /// </summary>
        public object Current => null!;

        /// <summary>
        /// 移动到下一个元素
        /// </summary>
        /// <returns>总是返回false，表示没有更多元素</returns>
        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(false);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}