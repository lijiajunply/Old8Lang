using System.Reflection;
using Old8Lang.AST.Statement;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class OldFunc : OldValue
{
    public OldID Id { get; set; }

    private BlockStatement BlockStatement { get; set; }

    public List<OldID>? Ids { get; set; }

    private bool IsNative { get; set; }

    private MethodInfo Method { get; set; }

    private OldFunc Func { get; set; }

    public OldFunc(OldID id,List<OldID> ids,BlockStatement blockStatement)
    {
        Id             = id;
        Ids            = ids;
        BlockStatement = blockStatement;
    }

    public OldFunc(string idName,MethodInfo methodInfo,OldFunc func)
    {
        IsNative = true;
        Method   = methodInfo;
        Func     = func;
    }

    public override OldValue Run(ref VariateManager Manager) => this;


    public OldValue Run(ref VariateManager Manager,List<OldExpr>? ids)
    {
        if (IsNative)
        {
            var values = new List<OldValue>();
            foreach (var expr in ids)
                values.Add(expr.Run(ref Manager));
            var obj = Method.Invoke(null,APIs.ListToObjects(values).ToArray());

            var manager = new VariateManager();
            manager.Init(new Dictionary<string,OldValue> { { "base",ObjToValue(obj) } });
            return manager.Result;
        }

        if (Manager.IsClass)
        {
            Manager.AddChildren();
            if (ids is not null && Ids is not null)
                for (var i = 0; i < ids.Count; i++)
                    Manager.Set(Ids[i],ids[i].Run(ref Manager));
            BlockStatement.Run(ref Manager);
            Manager.RemoveChildren();
            return Manager.Result;
        }
        else
        {
            var manager = new VariateManager { AnyInfo = Manager.AnyInfo };
            if (ids is not null && Ids is not null)
                for (var i = 0; i < ids.Count; i++)
                    manager.Set(Ids[i],ids[i].Run(ref Manager));
            BlockStatement.Run(ref manager);
            return manager.Result;
        }

    }

    public override string ToString() => IsNative ? $"{Method}" : $"{Id}({APIs.ListToString(Ids)}) => {BlockStatement}";
    
    public override object GetValue() => Value;
}