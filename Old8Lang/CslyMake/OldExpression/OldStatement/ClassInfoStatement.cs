using System.Text;

namespace Old8Lang.CslyMake.OldExpression;

public class ClassInfoStatement : OldStatement
{
    public List<OldLangTree> Values { get; set; }
    public ClassInfoStatement(List<OldLangTree> values) => Values = values;
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var VARIABLE in Values)
        {
            sb.Append(VARIABLE);
        }

        return sb.ToString();
    }
}