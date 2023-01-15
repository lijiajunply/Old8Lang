using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldAny : OldValue
{
    public Dictionary<OldID,OldExpr> Variates { get; set; }
    public OldID Id { get; set; }

    public OldAny(OldID id, Dictionary<OldID,OldExpr> variates)
    {
        Variates = variates; 
        Id       = id;
    }

    public override OldValue Dot(OldID DotID)
    {
        OldExpr a = new OldExpr();
        foreach (var VARIABLE in Variates)
            if (DotID.IdName == VARIABLE.Key.IdName)
                a = VARIABLE.Value;
        VariateManager manager = new VariateManager();
        manager.Init(Variates);
        return a.Run();
    }

    public override string ToString() => $"class {Id} : {Variates}";
}