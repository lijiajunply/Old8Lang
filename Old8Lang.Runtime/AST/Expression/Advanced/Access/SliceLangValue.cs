using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Helpers;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 切片表达式
/// </summary>
/// <param name="id"></param>
/// <param name="start"></param>
/// <param name="end"></param>
/// <param name="step"></param>
public partial class SliceLangValue(LangId id, LangExpression? start = null, LangExpression? end = null, LangExpression? step = null) : LangValueType
{
    /// <summary>
    /// 集合标识符
    /// </summary>
    public LangId Id { get; } = id;

    /// <summary>
    /// 起始索引表达式
    /// </summary>
    public LangExpression? Start { get; } = start;

    /// <summary>
    /// 结束索引表达式
    /// </summary>
    public LangExpression? End { get; } = end;

    /// <summary>
    /// 步长表达式
    /// </summary>
    public LangExpression? Step { get; } = step;

    public override string ToString()
    {
        if (Start is not null && End is not null && Step is not null)
            return $"{Id}[{Start}:{End}:{Step}]";
        if (Start is not null && End is not null)
            return $"{Id}[{Start}:{End}]";
        if (Start is not null)
            return $"{Id}[{Start}:]";
        if (End is not null)
            return $"{Id}[:{End}]";
        return $"{Id}[:]"; // Old8Lang 风格的切片表达式
    }
}