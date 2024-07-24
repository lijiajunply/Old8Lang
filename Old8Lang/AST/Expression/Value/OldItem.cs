using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class OldItem(OldID listId, OldExpr key) : ValueType
{
    private OldID ListID { get; } = listId;

    private OldExpr Key { get; } = key;

    public override ValueType Run(ref VariateManager Manager)
    {
        var a = Manager.GetValue(ListID);
        OldExpr result = Key.Run(ref Manager);
        if (a is ListValue list && result is IntValue intResult)
            return list.Get(intResult);
        if (a is ArrayValue array && result is IntValue i)
            return array.Get(i);
        if (a is DictionaryValue dir)
        {
            if (result is ValueType keyResult) return dir.Get(keyResult);
        }

        return new VoidValue();
    }

    public override string ToString() => $"the key: {Key} in {ListID}";
}