using System.Reflection;
using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldFunc : OldValue
{
    public OldID ID { get; set; }
    public BlockStatement BlockStatement { get; set; }
    public List<OldID> IDs { get; set; }
    public OldExpr Return { get; set; }
    public bool isNative { get; set; }
    public MethodInfo Method { get; set; }

    public OldFunc(OldID id,List<OldID>ids,BlockStatement blockStatement,OldExpr _return)
    {
        ID = id;
        IDs = ids;
        BlockStatement = blockStatement;
        Return = _return;
    }

    public OldFunc(string IDName, MethodInfo methodInfo)
    {
        isNative = true;
        Method = methodInfo;
    }

    public OldExpr Run(ref VariateManager Manager,List<OldID> ids)
    {
        if (!isNative)
        {
            VariateManager manager = new VariateManager();
            manager.Init();
            manager.AddChildren();
            var ValueFromFunc = new List<OldValue>();
            foreach (var VARIABLE in ids)
            {
                ValueFromFunc.Add(Manager.GetValue(VARIABLE));
            }

            for (int i = 0; i < ids.Count; i++)
            {
                manager.Set(IDs[i], ValueFromFunc[i]);
            }
            BlockStatement.Run(ref manager);
            var a = Return as BinaryOperation;
            var b = a.Run(ref manager);
            manager.RemoveChildren();
            return b as OldValue;
        }
        else
        {
            List<object> a = new List<object>();
            foreach (var VARIABLE in ids)
            {
                a.Add(Manager.GetValue(VARIABLE));
            }

            var b = a.ToArray();
            var r = Method.Invoke(null,b);
            if (r is string)
                return new OldString(r.ToString());
            if (r is int)
                return new OldInt((int)r);
            if (r is double)
                return new OldDouble((double)r);
            if (r is char)
                return new OldChar(r.ToString()[0]);
        }
        return null;
    }

    public OldExpr Run(ref VariateManager Manager,List<OldID> ids, Dictionary<OldID, OldValue> dictionary)
    {
        VariateManager manager = new VariateManager();
        manager.Init(dictionary);
        manager.AddChildren();
        var ValueFromFunc = new List<OldValue>();
        foreach (var VARIABLE in ids)
        {
            ValueFromFunc.Add(Manager.GetValue(VARIABLE));
        }

        for (int i = 0; i < ids.Count; i++)
        {
            manager.Set(IDs[i], ValueFromFunc[i]);
        }
        BlockStatement.Run(ref manager);
        var a = Return as BinaryOperation;
        var b = a.Run(ref manager);
        manager.RemoveChildren();
        return b as OldValue;
    }
}