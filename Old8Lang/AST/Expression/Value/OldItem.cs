using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class OldItem : OldValue
{
    public OldID ListID { get; set; }
    public OldExpr Key { get; set; }

    public OldItem(OldID listId , OldExpr key)
    {
        ListID = listId;
        Key = key;
    }

    public override OldValue Run(ref VariateManager Manager)
    {
        var a = Manager.GetValue(ListID);
        var result = new OldExpr();
        if (Key is OldValue)
            result = Key;
        else
            result = Key.Run(ref Manager);
        if (a is OldList && result is OldInt)
        {
            var intResult = result as OldInt;
            var alist = a as OldList;
            return alist.Values[intResult.Value];
        }

        if (a is OldDictionary && result is not null)
        {
            var keyResult = result as OldValue;
            var aDir = a as OldDictionary;
            return aDir.GetValue(keyResult);
        }
        return null;
    }

    public override string ToString() => $"the key: {Key} in {ListID}";
}