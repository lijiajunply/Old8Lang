namespace Old8Lang.AST.Expression.Value;

public interface IOldList
{
    public IEnumerable<ValueType> GetItems();
    public int GetLength();
}