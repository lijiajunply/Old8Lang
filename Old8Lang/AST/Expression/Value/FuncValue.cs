using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class FuncValue : ValueType
{
    public readonly OldID? Id;
    public readonly BlockStatement BlockStatement = new([]);

    public readonly List<OldID>? Ids;

    public readonly MethodInfo? Method;

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
            var values = ids.Select(expr => expr.Run(Manager)).ToList();
            var a = Apis.ListToObjects(values).ToArray();
            var invoke = Method?.Invoke(obj, a);

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

    public override Type OutputType(LocalManager local)
    {
        var idType = Id?.OutputType(local);
        if (idType != null && idType != typeof(object)) return idType;
        var a = GetItemType(BlockStatement, local);
        return a;
    }

    private static Type GetItemType(OldStatement statement, LocalManager local)
    {
        for (var i = 0; i < statement.Count; i++)
        {
            var item = statement[i];

            if (item is ReturnStatement returnStatement)
            {
                return returnStatement.OutputType(local);
            }

            if (item == null || item.Count == 0)
            {
                continue;
            }

            return GetItemType(item, local);
        }

        return typeof(void);
    }

    public override string ToString()
    {
        if (Method != null)
        {
            return $"{Method}";
        }

        var builder = new StringBuilder();
        for (var i = 0; i < Ids!.Count; i++)
            builder.Append("dynamic " + Ids[i] + (i == Ids.Count - 1 ? "" : ","));
        return $"public static dynamic {Id} ({builder}) \n {{ {BlockStatement} }}";
    }

    public void LoadIL(MethodBuilder methodBuilder,LocalManager local)
    {
        //var funcLocal = new LocalManager();
        var parameterTypes = Ids!.Select(item => item.OutputType(local)).ToArray();

        // 创建方法的 IL 发射器
        var methodIL = methodBuilder.GetILGenerator();

        methodIL.Emit(OpCodes.Ldarg_0);
        var thisType = methodIL.DeclareLocal(typeof(object));
        methodIL.Emit(OpCodes.Stloc, thisType);
        local.AddLocalVar("this", thisType);
        for (var i = 1; i <= Ids!.Count; i++)
        {
            var id = Ids[i-1];
            var localVar = methodIL.DeclareLocal(parameterTypes[i-1]);
            local.AddLocalVar(id.IdName, localVar);
            methodIL.Emit(OpCodes.Ldarg, i);

            methodIL.Emit(OpCodes.Stloc, localVar);
        }

        local.DelegateVar.Add(Id!.IdName, methodBuilder);

        // 生成方法体的 IL 代码
        BlockStatement.GenerateIL(methodIL, local);

        // 返回
        methodIL.Emit(OpCodes.Ret);
    }
}