using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class OldItem : OldValue
{
    private OldID   ListID { get; set; }

    private OldExpr Key { get; set; }

    public OldItem(OldID listId , OldExpr key)
    {
        ListID = listId;
        Key = key;
    }

    public override OldValue Run(ref VariateManager Manager)
    {
        var a = Manager.GetValue(ListID);
        var result = new OldExpr();
        result = Key.Run(ref Manager);
        if (a is OldList list && result is OldInt intResult)
            return list.Get(intResult);
        if (a is OldArray array && result is OldInt i)
            return array.Get(i);
        if (a is OldDictionary dir)
        {
            var keyResult = result as OldValue;
            return dir.Get(keyResult);
        }
        return null;
    }

    public override string ToString() => $"the key: {Key} in {ListID}";
}