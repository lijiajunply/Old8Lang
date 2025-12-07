namespace Old8Lang.AST.Expression.Intermediates;

public interface IOldList
{
    public IEnumerable<ValueType> GetItems();
    public int GetLength();
    public ValueType Slice(int start, int end);
    public Type GetChildType();
}