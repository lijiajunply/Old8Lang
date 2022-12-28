namespace Old8Lang.CslyMake.OldExpression;

public class OldBlock : OldLangTree
{
    public string Location { get; set; }
    public List<OldLangTree> Statements { get; set; }
    public OldBlock(List<OldLangTree> statements) => Statements = statements;
}