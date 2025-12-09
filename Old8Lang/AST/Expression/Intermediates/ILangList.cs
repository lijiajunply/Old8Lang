namespace Old8Lang.AST.Expression.Intermediates;

public interface ILangList
{
    public IEnumerable<LangValueType> GetItems();
    public int GetLength();
    public LangValueType Slice(int start, int end);
    public Type GetChildType();
    public void Set(LangValueType index, LangValueType value);
}