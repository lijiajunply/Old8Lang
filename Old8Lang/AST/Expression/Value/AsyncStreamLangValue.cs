using System.Reflection.Emit;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 异步流值类型
/// 表示 async { } 语法创建的异步流（异步生成器）
/// 这是 AsyncGeneratorLangValue 的包装类，专门用于 async { } 语法
/// </summary>
public partial class AsyncStreamLangValue : LangValueType, ILangList
{
    /// <summary>
    /// 内部的异步生成器
    /// </summary>
    private readonly AsyncGeneratorLangValue _generator;

    /// <summary>
    /// 构造函数（从 AsyncFuncLangValue 创建）
    /// </summary>
    /// <param name="asyncFunc">异步函数（包含 async { } 块的函数）</param>
    /// <param name="position">源代码位置</param>
    public AsyncStreamLangValue(AsyncFuncLangValue asyncFunc, SourcePosition position = default)
        : base(position)
    {
        _generator = new AsyncGeneratorLangValue(asyncFunc, position);
    }

    /// <summary>
    /// 构造函数（从 AsyncGeneratorLangValue 创建）
    /// </summary>
    /// <param name="asyncGenerator">异步生成器</param>
    /// <param name="position">源代码位置</param>
    public AsyncStreamLangValue(AsyncGeneratorLangValue asyncGenerator, SourcePosition position = default)
        : base(position)
    {
        _generator = asyncGenerator;
    }

    /// <summary>
    /// 执行异步流，返回下一个值
    /// </summary>
    public override LangValueType Run(VariateManager manager)
    {
        return _generator.Run(manager);
    }

    /// <summary>
    /// 异步执行，返回包含下一个值的 Task
    /// </summary>
    public TaskLangValue RunAsync(VariateManager manager)
    {
        return _generator.RunAsync(manager);
    }

    /// <summary>
    /// 取消异步流的执行
    /// </summary>
    public void Cancel()
    {
        _generator.Cancel();
    }

    /// <summary>
    /// 重置异步流状态
    /// </summary>
    public void Reset()
    {
        _generator.Reset();
    }

    /// <summary>
    /// 获取异步流的当前状态
    /// </summary>
    public AsyncGeneratorLangValue.AsyncGeneratorState State => _generator.State;

    /// <summary>
    /// 获取异步流的下一个值
    /// </summary>
    public LangValueType? NextValue => _generator.NextValue;

    #region ILangList 实现

    /// <summary>
    /// 获取异步流的所有项
    /// </summary>
    public IEnumerable<LangValueType> GetItems()
    {
        return _generator.GetItems();
    }

    /// <summary>
    /// 获取异步流的长度（异步流长度未知，返回 -1）
    /// </summary>
    public int GetLength()
    {
        return _generator.GetLength();
    }

    /// <summary>
    /// 对异步流进行切片（不支持）
    /// </summary>
    public LangValueType Slice(int start, int end, int step)
    {
        return _generator.Slice(start, end, step);
    }

    /// <summary>
    /// 设置异步流中指定索引的值（不支持）
    /// </summary>
    public void Set(LangValueType index, LangValueType value)
    {
        _generator.Set(index, value);
    }

    /// <summary>
    /// 切片赋值操作（不支持）
    /// </summary>
    public void SetSlice(int start, int end, IEnumerable<LangValueType> values)
    {
        _generator.SetSlice(start, end, values);
    }

    /// <summary>
    /// 检查值是否在异步流中
    /// </summary>
    public bool In(LangValueType value)
    {
        return _generator.In(value);
    }

    #endregion

    #region 编译器支持

    /// <summary>
    /// 获取输出类型
    /// </summary>
    public override Type OutputType(LocalManager local)
    {
        return typeof(AsyncStreamLangValue);
    }

    /// <summary>
    /// 生成 IL 代码
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 异步流的 IL 代码生成（后续实现）
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    /// <summary>
    /// 设置值到 IL 代码
    /// </summary>
    public override void SetValueToIl(ILGenerator ilGenerator, LocalManager local, string idName)
    {
        // 异步流的 IL 代码生成（后续实现）
    }

    #endregion

    /// <summary>
    /// 转换为字符串
    /// </summary>
    public override string ToString()
    {
        return $"AsyncStream({_generator})";
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _generator.Dispose();
    }
}
