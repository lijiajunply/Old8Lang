using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class ListInitStatement : OldStatement
{
    public OldID Id { get; set; }
    public List<OldLangTree> Values { get; set; }

    public ListInitStatement(OldID id, List<OldLangTree> values)
    {
        Id = id;
        Values = values;
    }

    public override void Run(ref VariateManager Manager)
    {
        var value = new List<OldValue>();
        foreach (var VARIABLE in Values)
        {
            var va = VARIABLE as OldExpr;
            value.Add(va.Run(ref Manager));
        }
        var result = new OldList(Id,value);
        Manager.Set(Id, result);
    }
}