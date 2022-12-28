using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;
/// <summary>
/// 块
/// </summary>
public class OldBlock : OldStatement
{
    public string Location { get; set; }
    public List<OldStatement> Statements { get; set; }
    public OldBlock(List<OldStatement> statements) => Statements = statements;
    public override void Run(ref VariateManager Manager)
    {
        foreach (var VARIABLE in Statements)
        {
            VARIABLE.Run(ref Manager);
        }
    }
}