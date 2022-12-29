namespace Old8Lang.CslyMake.OldExpression;

public class OldAny : OldValue
{
    public Dictionary<OldID,OldExpr> Variates { get; set; }
    public OldID Id { get; set; }

    public OldAny(OldID id, Dictionary<OldID,OldExpr> variates)
    {
        Variates = variates;
        Id = id;
    }
}