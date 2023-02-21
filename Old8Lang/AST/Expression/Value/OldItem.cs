using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class OldItem : ValueType
{
    private OldID   ListID { get; set; }

    private OldExpr Key { get; set; }

    public OldItem(OldID listId , OldExpr key)
    {
        ListID = listId;
        Key = key;
    }

    public override ValueType Run(ref VariateManager Manager)
    {
        var a = Manager.GetValue(ListID);
        var result = new OldExpr();
        result = Key.Run(ref Manager);
        if (a is ListValue list && result is IntValue intResult)
            return list.Get(intResult);
        if (a is ArrayValue array && result is IntValue i)
            return array.Get(i);
        if (a is DictionaryValue dir)
        {
            var keyResult = result as ValueType;
            return dir.Get(keyResult);
        }
        return null;
    }

    public override string ToString() => $"the key: {Key} in {ListID}";
}