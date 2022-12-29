namespace Old8Lang.CslyMake.OldExpression;

public class OldAny : OldValue
{
    public List<OldExpr> Variates { get; set; }
    public OldID Id { get; set; }

    public OldAny(OldID id, List<OldExpr> variates)
    {
        Variates = variates;
        Id = id;
    }
}