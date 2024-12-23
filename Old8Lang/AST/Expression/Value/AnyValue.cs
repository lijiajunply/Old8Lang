using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class AnyValue : ValueType
{
    public readonly Dictionary<OldID, OldExpr> Variates;
    public readonly Dictionary<string, ValueType> Result = new();
    public readonly OldID Id;

    public readonly VariateManager manager;

    public AnyValue(OldID id, Dictionary<OldID, OldExpr> variates)
    {
        Variates = variates;
        Id = id;
        manager = new VariateManager();
        Run(manager);
        manager.Init(Result);
        manager.IsClass = true;
    }

    public AnyValue(Dictionary<OldID, OldExpr> variates)
    {
        Variates = variates;
        Id = new OldID("JsonNative");
        manager = new VariateManager();
        foreach (var variate in variates)
        {
            if (variate.Value is ValueType valueType) Result.Add(variate.Key.IdName, valueType);
        }

        manager.Init(Result);
        manager.IsClass = true;
    }

    public sealed override ValueType Run(VariateManager Manager)
    {
        manager.AnyInfo.AddRange(Manager.AnyInfo.Where(x => x is not FuncValue).ToList());
        foreach (var variable in Variates.Keys)
            Result.Add(variable.IdName, Variates[variable].Run(Manager));
        return this;
    }

    public override ValueType Dot(OldExpr dotExpr)
    {
        switch (dotExpr)
        {
            case OldID id:
            {
                var a = manager.GetValue(id);
                if (a == null) throw new Exception("not found");
                return a.Run(manager);
            }
            case FuncValue func:
            {
                if (func.Id?.IdName == "GetType")
                    return new TypeValue(TypeToString());
                return func.Run(manager);
            }
            default:
                return dotExpr.Run(manager);
        }
    }

    public void Set(OldID id, ValueType valueType) => manager.Set(id, valueType);

    public override ValueType Converse(ValueType otherValueType, VariateManager Manager)
    {
        if (otherValueType is not AnyValue typeAny) return new VoidValue();

        foreach (var a in Result)
        {
            typeAny.Set(new OldID(a.Key), a.Value);
        }

        return typeAny;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append('{');
        for (var i = 0; i < Variates.Count; i++)
        {
            var variable = Variates.ElementAt(i);
            builder.Append($"{(i == 0 ? "" : ",")}\"{variable.Key}\":{variable.Value}");
        }

        builder.Append('}');
        return builder.ToString();
    }

    public override void LoadILValue(ILGenerator ilGenerator, LocalManager local)
    {
    }

    public override Type? OutputType(LocalManager local)
    {
        return local.ClassVar.GetValueOrDefault(Id.IdName);
    }
}