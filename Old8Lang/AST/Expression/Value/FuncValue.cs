using System.Reflection;
using System.Text;
using Old8Lang.AST.Statement;
using Old8Lang.CslyParser;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class FuncValue : ValueType
{
    public readonly OldID? Id;
    private readonly BlockStatement BlockStatement = new([]);

    public readonly List<OldID>? Ids;

    private readonly MethodInfo? Method;

    private readonly FuncValue? Func;

    public FuncValue(OldID? id, List<OldID> ids, BlockStatement blockStatement)
    {
        Id = id;
        Ids = ids;
        BlockStatement = blockStatement;
    }

    public FuncValue(string idName, MethodInfo methodInfo, FuncValue? func = null)
    {
        Id = new OldID(idName);
        Method = methodInfo;
        Func = func;
    }

    public override ValueType Run(VariateManager Manager) => this;

    public ValueType Run(VariateManager Manager, List<OldExpr> ids, object? obj = null)
    {
        if (Method != null)
        {
            var values = new List<ValueType>();
            foreach (var expr in ids)
                values.Add(expr.Run(Manager));
            var a = Apis.ListToObjects(values).ToArray();
            object? invoke;
            try
            {
                invoke = Method?.Invoke(obj, a);
            }
            catch
            {
                throw new ErrorException(this, this);
            }

            if (invoke is null)
                return new VoidValue();

            var manager = new VariateManager();
            manager.Init(new Dictionary<string, ValueType> { { "base", ObjToValue(invoke) } });
            manager.IsClass = false;
            manager.Result = ObjToValue(invoke);
            Func?.Run(manager, ids);
            return manager.Result;
        }

        if (Manager.IsClass)
        {
            Manager.AddChildren();
            if (Ids != null && Ids.Count != 0)
                for (var i = 0; i < ids.Count; i++)
                    Manager.Set(Ids[i], ids[i].Run(Manager));
            BlockStatement.Run(Manager);
            Manager.RemoveChildren();
            return Manager.Result;
        }

        var variateManager = Manager.NewManger();
        if (Ids != null && Ids.Count != 0)
            for (var i = 0; i < ids.Count; i++)
                variateManager.Set(Ids[i], ids[i].Run(Manager));
        BlockStatement.Run(variateManager);
        return variateManager.Result;
    }

    public override string ToString()
    {
        if (Method != null)
        {
            return $"{Method}";
        }

        var builder = new StringBuilder();
        for (var i = 0; i < Ids!.Count; i++)
            builder.Append("dynamic "+Ids[i] + (i == Ids.Count - 1 ? "" : ","));
        return $"public static dynamic {Id} ({builder}) \n {{ {BlockStatement} }}";
    }
}