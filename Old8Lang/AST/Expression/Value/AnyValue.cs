using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;


namespace Old8Lang.AST.Expression.Value;

public class AnyValue : ValueType
{
    public readonly Dictionary<OldId, OldExpr> Variates;
    public readonly Dictionary<string, ValueType> Result = new();
    public readonly OldId Id;

    public readonly VariateManager Manager;

    public AnyValue(OldId id, Dictionary<OldId, OldExpr> variates)
    {
        Variates = variates;
        Id = id;
        Manager = new VariateManager();
        Run(Manager);
        Manager.Init(Result);
        Manager.IsClass = true;
    }

    public AnyValue(Dictionary<OldId, OldExpr> variates)
    {
        Variates = variates;
        Id = new OldId("JsonNative");
        Manager = new VariateManager();
        foreach (var variate in variates)
        {
            if (variate.Value is ValueType valueType) Result.Add(variate.Key.IdName, valueType);
        }

        Manager.Init(Result);
        Manager.IsClass = true;
    }

    public sealed override ValueType Run(VariateManager manager)
    {
        Manager.AnyInfo.AddRange(manager.AnyInfo.Where(x => x is not FuncValue).ToList());
        foreach (var variable in Variates.Keys)
            Result.Add(variable.IdName, Variates[variable].Run(manager));
        return this;
    }

    public override ValueType Dot(OldExpr dotExpr)
    {
        switch (dotExpr)
        {
            case OldId id:
            {
                var a = Manager.GetValue(id);
                if (a == null) throw new Exception("not found");
                return a.Run(Manager);
            }
            case FuncValue func:
            {
                if (func.Id?.IdName == "GetType")
                    return new TypeValue(TypeToString());
                return func.Run(Manager);
            }
            default:
                return dotExpr.Run(Manager);
        }
    }

    public void Set(OldId id, ValueType valueType) => Manager.Set(id, valueType);

    public override ValueType Converse(ValueType otherValueType, VariateManager manager)
    {
        if (otherValueType is not AnyValue typeAny) return new VoidValue();

        foreach (var a in Result)
        {
            typeAny.Set(new OldId(a.Key), a.Value);
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

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
    }

    public override Type? OutputType(LocalManager local)
    {
        return local.ClassVar.GetValueOrDefault(Id.IdName);
    }
}