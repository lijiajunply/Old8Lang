namespace Old8Lang.CslyMake.OldExpression;
/// <summary>
/// 块
/// </summary>
public class OldBlock : OldStatement
{
    public string Location { get; set; }
    public List<OldLangTree> Statements { get; set; }
    public OldBlock(List<OldLangTree> statements) => Statements = statements;
}