using System.Reflection;
using Old8Lang.AST.Statement;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class FuncValue : ValueType
{
    public OldID Id { get; set; }

    private BlockStatement BlockStatement { get; set; }

    public List<OldID>? Ids { get; set; }

    private bool IsNative { get; set; }

    private MethodInfo Method { get; set; }

    private FuncValue Func { get; set; }

    public FuncValue(OldID id,List<OldID> ids,BlockStatement blockStatement)
    {
        Id             = id;
        Ids            = ids;
        BlockStatement = blockStatement;
    }

    public FuncValue(string idName,MethodInfo methodInfo,FuncValue func = null)
    {
        Id       = new OldID(idName);
        IsNative = true;
        Method   = methodInfo;
        Func     = func;
    }

    public override ValueType Run(ref VariateManager Manager) => this;

    public ValueType Run(ref VariateManager Manager,List<OldExpr>? ids,object obj = null)
    {
        if (IsNative)
        {
            var values = new List<ValueType>();
            foreach (var expr in ids)
                values.Add(expr.Run(ref Manager));
            var a   = APIs.ListToObjects(values).ToArray();
            var invoke = Method.Invoke(obj,a);

            var manager = new VariateManager();
            manager.Init(new Dictionary<string,ValueType> { { "base",ObjToValue(invoke) } });
            manager.IsClass = false;
            manager.Result  = ObjToValue(invoke);
            if (Func is not null) Func.Run(ref manager,ids);
            return manager.Result;
        }

        if (Manager.IsClass)
        {
            Manager.AddChildren();
            if (Ids != null && ids is not null && Ids.Count != 0)
                for (var i = 0; i < ids.Count; i++)
                    Manager.Set(Ids[i],ids[i].Run(ref Manager));
            BlockStatement.Run(ref Manager);
            Manager.RemoveChildren();
            return Manager.Result;
        }

        var variateManager = new VariateManager { AnyInfo = Manager.AnyInfo };
        if (Ids != null && ids is not null && Ids.Count != 0)
            for (var i = 0; i < ids.Count; i++)
                variateManager.Set(Ids[i],ids[i].Run(ref Manager));
        BlockStatement.Run(ref variateManager);
        return variateManager.Result;
    }

    public override string ToString() => IsNative ? $"{Method}" : $"{Id}({APIs.ListToString(Ids)}) => {BlockStatement}";

    public override object GetValue() => Value;
}