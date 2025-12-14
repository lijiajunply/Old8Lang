namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 列表接口
/// </summary>
public interface ILangList
{
    public IEnumerable<LangValueType> GetItems();
    public int GetLength();
    public LangValueType Slice(int start, int end);
    public void Set(LangValueType index, LangValueType value);
    public bool In(LangValueType value);
}