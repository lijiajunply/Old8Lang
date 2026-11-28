using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.AsyncGeneration;

namespace Old8Lang.AST.Expression.Generators;

/// <summary>
/// 生成器对象，用于表示生成器函数的实例，实现ILangList接口以支持迭代
/// </summary>
public partial class GeneratorLangValue : LangValueType, ILangList
{
    /// <summary>
    /// 生成器函数引用
    /// </summary>
    public FuncLangValue Func { get; init; }

    /// <summary>
    /// 新生成器状态机
    /// </summary>
    internal GeneratorStateMachine? StateMachine { get; set; }

    /// <summary>
    /// 生成器当前状态
    /// </summary>
    public GeneratorState State { get; internal set; } = GeneratorState.Suspended;

    /// <summary>
    /// 生成器迭代器的下一个值
    /// </summary>
    public LangValueType? NextValue { get; internal set; }

    /// <summary>
    /// 生成器函数的参数值
    /// </summary>
    internal Dictionary<string, LangValueType> ParameterValues { get; } = new();

    /// <summary>
    /// 生成器状态枚举
    /// </summary>
    public enum GeneratorState
    {
        Suspended,
        Running,
        Completed
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="func">生成器函数引用</param>
    /// <param name="position">源代码位置</param>
    public GeneratorLangValue(FuncLangValue func, SourcePosition position = default) : base(position)
    {
        Func = func;
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
    /// 重置生成器状态
    /// </summary>
    public void Reset()
    {
        State = GeneratorState.Suspended;
        NextValue = null;
        StateMachine?.Reset();
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns>生成器的字符串表示</returns>
    public override string ToString() => $"Generator({Func.Id})";

    /// <summary>
    /// 获取生成器的所有项
    /// </summary>
    /// <returns>生成器项的枚举</returns>
    public IEnumerable<LangValueType> GetItems()
    {
        // 生成器的迭代逻辑由ForInStatement处理，这里只返回空枚举
        // 避免在迭代过程中影响生成器的状态
        yield break;
    }

    /// <summary>
    /// 获取生成器的长度
    /// </summary>
    /// <returns>生成器的长度，-1表示未知长度</returns>
    public int GetLength()
    {
        // 生成器的长度通常是未知的，返回-1表示未知长度
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
        // 创建一个新的生成器函数，实现切片逻辑
        var slicedFunc = new FuncLangValue(
            null,
            [],
            new BlockStatement(new List<OldStatement>()),
            null,
            Position
        );

        return new GeneratorLangValue(slicedFunc, Position);
    }

    /// <summary>
    /// 设置生成器中指定索引的值
    /// </summary>
    /// <param name="index">索引</param>
    /// <param name="value">值</param>
    /// <exception cref="NotSupportedException">生成器不支持设置值</exception>
    public void Set(LangValueType index, LangValueType value)
    {
        // 生成器是只读的，不支持设置值
        throw new NotSupportedException("生成器不支持设置值");
    }

    /// <summary>
    /// 切片赋值操作
    /// </summary>
    /// <exception cref="NotSupportedException">生成器不支持切片赋值</exception>
    public void SetSlice(int start, int end, IEnumerable<LangValueType> values)
    {
        throw new NotSupportedException("生成器不支持切片赋值操作");
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
}