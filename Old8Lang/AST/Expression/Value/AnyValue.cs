using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class AnyValue : ValueType
{
    private Dictionary<OldID, OldExpr> Variates { get; set; }
    public Dictionary<string, ValueType> Result { get; set; }
    public OldID Id { get; set; }

    private VariateManager manager;

    public AnyValue(OldID id, Dictionary<OldID, OldExpr> variates)
    {
        Variates = variates;
        Id = id;
        Result = new Dictionary<string, ValueType>();
        manager = new VariateManager();
        Run(ref manager);
        manager.Init(Result);
        manager.IsClass = true;
    }

    public override ValueType Run(ref VariateManager Manager)
    {
        manager.AnyInfo = Manager.AnyInfo.Where(x => x is not FuncValue).ToList();
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
                var a = new OldExpr();
                foreach (var variable in Variates.Where(variable => id.IdName == variable.Key.IdName))
                    a = variable.Value;
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

    public override string ToString() => $"class {Id} " + "{" + "\n{manager}" + "\n}";

    public void Init()
    {
        manager = new VariateManager();
        manager.Init(Result);
        manager.IsClass = true;
    }
}