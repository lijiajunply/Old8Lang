using System.Text;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;
/// <summary>
/// 块
/// </summary>
public class BlockStatement : OldStatement
{
    public List<OldStatement> Statements       { get; set; }
    private List<OldStatement> ImportStatements { get; set; }
    private List<OldStatement> OtherStatements  { get; set; }

    public BlockStatement(List<OldLangTree> statements)
    {
        ImportStatements = new List<OldStatement>();
        OtherStatements  = new List<OldStatement>();
        Statements       = statements.OfType<OldStatement>().ToList();
        foreach (var statement in Statements)
        {
            if (statement is ImportStatement or NativeStatement or FuncInit or ClassInit)
                ImportStatements.Add(statement);
            else
                OtherStatements.Add(statement);
        }
    }
    public override void Run(ref VariateManager Manager)
    {
        foreach (var statement in ImportStatements)
        {
            statement.Run(ref Manager);
            if (Manager.IsReturn) return;
        }
        foreach (var statement in OtherStatements)
        {
            statement.Run(ref Manager);
            if (Manager.IsReturn) return;
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var t in Statements)
            sb.Append(t+"\n");
        return sb.ToString();
    }
    public string ToCode()
    {
        StringBuilder sb     = new StringBuilder();
        var           import = ImportStatements.OfType<ImportStatement>().ToList();
        var           func   = ImportStatements.Where(x => x is ClassInit or FuncInit).ToList();
        foreach (var importStatement in import)
        {
            sb.Append(importStatement+"\n");
        }
        sb.Append("namespace Code\n{\n");
        foreach (var statement in func)
        {
            sb.Append(statement+"\n");
        }
        sb.Append($"class Project{{\n{{\npublic void Main()\n{{\n");
        foreach (var statement in OtherStatements)
        {
            sb.Append(statement+"\n");
        }
        sb.Append("}");
        sb.Append("\n}");
        return sb.ToString();
    }
}