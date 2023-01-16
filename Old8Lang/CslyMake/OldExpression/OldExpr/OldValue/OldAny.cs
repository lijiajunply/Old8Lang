using System.Text;
using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldAny : OldValue
{
    public Dictionary<OldID,OldExpr>  Variates { get; set; }
    public Dictionary<OldID,OldValue> Result   { get; set; }
    public OldID                      Id       { get; set; }

    public OldAny(OldID id, Dictionary<OldID,OldExpr> variates)
    {
        Variates = variates; 
        Id       = id;
        var man = new VariateManager();
        Result = new Dictionary<OldID,OldValue>();
        Run(ref man);
    }

    public override OldValue Run(ref VariateManager Manager)
    {
        foreach (var VARIABLE in Variates.Keys)
        {
            var a = Variates[VARIABLE].Run(ref Manager);
            Result.Add(VARIABLE,a);
        }
        return this;
    }

    public override OldValue Dot(OldExpr Dot)
    {
        var a = new OldExpr();
        if (Dot is OldID)
        {
            var DotID = Dot as OldID;
            foreach (var VARIABLE in Variates)
                if (DotID.IdName == VARIABLE.Key.IdName)
                    a = VARIABLE.Value;
        }
        if (Dot is OldInstance)
        {
            a = Dot as OldInstance;
        }
        VariateManager manager = new VariateManager();
        manager.Init(Result);
        return a.Run(ref manager);
    }

    public override string ToString() => $"class {Id} : {DicToString()}";
    public string DicToString()
    {
        StringBuilder builder = new StringBuilder();
        foreach (var VARIABLE in Variates)
        {
            builder.Append(VARIABLE.Key+":"+VARIABLE.Value);
        }
        return builder.ToString();
    }
}