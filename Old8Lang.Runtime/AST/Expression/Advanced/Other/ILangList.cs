namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 列表接口
/// </summary>
public interface ILangList
{
    public IEnumerable<LangValueType> GetItems();
    public int GetLength();

    /// <summary>
    /// 切片操作，支持步长
    /// </summary>
    /// <param name="start">起始索引</param>
    /// <param name="end">结束索引</param>
    /// <param name="step">步长（默认为1，负数表示反向）</param>
    /// <returns>切片结果</returns>
    public LangValueType Slice(int start, int end, int step);

    /// <summary>
    /// 切片赋值操作：替换或删除指定范围的元素
    /// </summary>
    /// <param name="start">起始索引</param>
    /// <param name="end">结束索引</param>
    /// <param name="values">要替换的值（如果为空列表则删除）</param>
    public void SetSlice(int start, int end, IEnumerable<LangValueType> values);

    public void Set(LangValueType index, LangValueType value);
    public bool In(LangValueType value);
}