using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;
/// <summary>
/// 块
/// </summary>
public class OldBlock : OldStatement
{
    public string Location { get; set; }
    public List<OldLangTree> Statements { get; set; }
    public OldBlock(List<OldLangTree> statements) => Statements = statements;
    public override void Run(ref VariateManager Manager)
    {
        foreach (var VARIABLE in Statements)
        {
            var a = VARIABLE as OldStatement;
            a.Run(ref Manager);
        }
    }
}