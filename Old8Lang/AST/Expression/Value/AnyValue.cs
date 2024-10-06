using System.Text;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class AnyValue : ValueType
{
    public Dictionary<OldID, OldExpr> Variates { get; }
    public Dictionary<string, ValueType> Result { get; } = new();
    public OldID Id { get; }

    public VariateManager manager;

    public AnyValue(OldID id, Dictionary<OldID, OldExpr> variates)
    {
        Variates = variates;
        Id = id;
        manager = new VariateManager();
        Run(ref manager);
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

    public sealed override ValueType Run(ref VariateManager Manager)
    {
        manager.AnyInfo.AddRange(Manager.AnyInfo.Where(x => x is not FuncValue).ToList());
        foreach (var variable in Variates.Keys)
            Result.Add(variable.IdName, Variates[variable].Run(ref Manager));
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
                return a.Run(ref manager);
            }
            case FuncValue func:
            {
                if (func.Id?.IdName == "GetType")
                    return new TypeValue(TypeToString());
                return func.Run(ref manager);
            }
            default:
                return dotExpr.Run(ref manager);
        }
    }

    public void Set(OldID id, ValueType valueType) => manager.Set(id, valueType);

    public override ValueType Converse(ValueType otherValueType, ref VariateManager Manager)
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
}