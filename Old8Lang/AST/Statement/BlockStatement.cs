using System.Text;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;
/// <summary>
/// 块
/// </summary>
public class BlockStatement : OldStatement
{
    public List<OldLangTree> Statements { get; set; }
    public BlockStatement(List<OldLangTree> statements) => Statements = statements;
    public override void Run(ref VariateManager Manager)
    {
        foreach (var VARIABLE in Statements)
        {
            var a = VARIABLE as OldStatement;
            a.Run(ref Manager);
            if (Manager.IsReturn) return;
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < Statements.Count; i++)
        {
            string a = i == 0 || i == Statements.Count-1 ? "" : "\n";
            sb.Append(Statements[i]+a);
        }
        return sb.ToString();
    }
}