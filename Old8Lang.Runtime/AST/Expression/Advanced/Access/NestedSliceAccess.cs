namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 嵌套切片访问表达式，用于处理 matrix[index][start:end] 或 matrix[index][start:end:step] 的情况
/// </summary>
/// <param name="baseExpression">基础表达式，可以是任何返回集合的表达式</param>
/// <param name="sliceStart">切片起始索引</param>
/// <param name="sliceEnd">切片结束索引（可选）</param>
/// <param name="sliceStep">切片步长（可选）</param>
/// <param name="position">源代码位置</param>
public partial class NestedSliceAccess(
    LangExpression baseExpression,
    LangExpression sliceStart,
    LangExpression? sliceEnd = null,
    LangExpression? sliceStep = null,
    SourcePosition position = default)
    : LangValueType(position)
{

    public readonly LangExpression BaseExpression = baseExpression;
    public readonly LangExpression SliceStart = sliceStart;
    public readonly LangExpression? SliceEnd = sliceEnd;
    public readonly LangExpression? SliceStep = sliceStep;

    public override string ToString()
    {
        var sliceStr = SliceEnd is not null
            ? (SliceStep is not null ? $"{SliceStart}:{SliceEnd}:{SliceStep}" : $"{SliceStart}:{SliceEnd}")
            : (SliceStep is not null ? $"{SliceStart}::{SliceStep}" : $"{SliceStart}:");
        return $"{BaseExpression}[{sliceStr}]";
    }

}