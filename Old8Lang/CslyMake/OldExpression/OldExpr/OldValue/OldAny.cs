using System.Text;
using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldAny : OldValue
{
    public Dictionary<OldID,OldExpr>  Variates { get; set; }
    public Dictionary<string,OldValue> Result   { get; set; }
    public OldID                      Id       { get; set; }

    public VariateManager Manager;
    public OldAny(OldID id, Dictionary<OldID,OldExpr> variates)
    {
        Variates = variates; 
        Id       = id;
        Result   = new Dictionary<string,OldValue>();
        Manager  = new VariateManager();
        Run(ref Manager);
        Manager.Init(Result);
        Manager.isClass = true;
    }

    public override OldValue Run(ref VariateManager Manager)
    {
        foreach (var VARIABLE in Variates.Keys)
            Result.Add(VARIABLE.IdName,Variates[VARIABLE].Run(ref Manager));
        return this;
    }

    public OldValue Dot(OldExpr dotExpr,List<OldExpr> c)
    {
        if (dotExpr is OldID)
        {
            var a = new OldExpr();
            var DotID = dotExpr as OldID;
            foreach (var VARIABLE in Variates)
                if (DotID.IdName == VARIABLE.Key.IdName)
                    a = VARIABLE.Value;
            return a.Run(ref Manager);
        }
        if (dotExpr is OldFunc)
        {
            var a = dotExpr as OldFunc;
            return a.Run(ref Manager,c);
        }
        return dotExpr.Run(ref Manager);
    }
    public void Post(OldID id,OldValue value)
    {
        Manager.Set(id,value);
    }

    public override string ToString() => $"class {Id} : \n{Manager}";
    public override void Init()
    {
        Manager = new VariateManager();
        Manager.Init(Result);
    }
}