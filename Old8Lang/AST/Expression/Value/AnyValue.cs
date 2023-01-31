using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class AnyValue : ValueType
{
    public Dictionary<OldID,OldExpr>  Variates { get; set; }
    public Dictionary<string,ValueType> Result   { get; set; }
    public OldID                      Id       { get; set; }

    public VariateManager manager;
    public AnyValue(OldID id, Dictionary<OldID,OldExpr> variates)
    {
        Variates = variates; 
        Id       = id;
        Result   = new Dictionary<string,ValueType>();
        manager  = new VariateManager();
        Run(ref manager);
        manager.Init(Result);
        manager.IsClass = true;
    }

    public override ValueType Run(ref VariateManager Manager)
    {
        manager.AnyInfo = Manager.AnyInfo.Where(x => x is not FuncValue).ToList();
        foreach (var variable in Variates.Keys)
            Result.Add(variable.IdName,Variates[variable].Run(ref Manager));
        return this;
    }

    public ValueType Dot(OldExpr dotExpr,List<OldExpr> c)
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
                return func.Run(ref manager,c);
            default:
                return dotExpr.Run(ref manager);
        }
    }
    public void Set(OldID id,ValueType valueType) => manager.Set(id,valueType);

    public override string ToString() => $"class {Id} : \n{manager}";
    public override void Init()
    {
        manager = new VariateManager();
        manager.Init(Result);
    }
}