using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class OldAny : OldValue
{
    public Dictionary<OldID,OldExpr>  Variates { get; set; }
    public Dictionary<string,OldValue> Result   { get; set; }
    public OldID                      Id       { get; set; }

    public VariateManager manager;
    public OldAny(OldID id, Dictionary<OldID,OldExpr> variates)
    {
        Variates = variates; 
        Id       = id;
        Result   = new Dictionary<string,OldValue>();
        manager  = new VariateManager();
        Run(ref manager);
        manager.Init(Result);
        manager.IsClass = true;
    }

    public override OldValue Run(ref VariateManager Manager)
    {
        foreach (var variable in Variates.Keys)
            Result.Add(variable.IdName,Variates[variable].Run(ref Manager));
        return this;
    }

    public OldValue Dot(OldExpr dotExpr,List<OldExpr> c)
    {
        if (dotExpr is OldID id)
        {
            var a = new OldExpr();
            foreach (var variable in Variates.Where(variable => id.IdName == variable.Key.IdName))
                a = variable.Value;
            return a.Run(ref manager);
        }
        if (dotExpr is OldFunc func)
            return func.Run(ref manager,c);
        return dotExpr.Run(ref manager);
    }
    public void Set(OldID id,OldValue value) => manager.Set(id,value);

    public override string ToString() => $"class {Id} : \n{manager}";
    public override void Init()
    {
        manager = new VariateManager();
        manager.Init(Result);
    }
}