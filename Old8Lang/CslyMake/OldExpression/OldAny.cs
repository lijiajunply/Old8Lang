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
}