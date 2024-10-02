namespace Old8Lang.AST.Expression.Value;

public interface IOldList
{
    public IEnumerable<ValueType> GetItems();
    public int GetLength();
    public ValueType Slice(int start, int end);
}