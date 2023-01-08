namespace Old8Lang.CslyMake.OldExpression;

public class OldAny : OldValue
{
    public Dictionary<string,OldExpr> Variates { get; set; }
    public OldID Id { get; set; }

    public OldAny(OldID id, Dictionary<OldID,OldExpr> variates)
    {
        foreach (var VARIABLE in variates)
        {
            Variates.Add(VARIABLE.Key.IdName,VARIABLE.Value);
        }
        Id = id;
    }

    public override OldValue Dot(OldID DotID)
    {
        OldExpr a = new OldExpr();
        foreach (var VARIABLE in Variates)
        {
            if (DotID.IdName == VARIABLE.Key)
            {
                a = VARIABLE.Value;
            }
        }
        return (OldValue)a;
    }
}