using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 块
/// </summary>
public class BlockStatement : OldStatement
{
    private List<OldStatement> ImportStatements { get; } = [];
    private List<OldStatement> OtherStatements { get; } = [];

    public BlockStatement(List<OldLangTree> statements)
    {
        foreach (var statement in statements.OfType<OldStatement>())
        {
            switch (statement)
            {
                case ImportStatement or NativeStatement or FuncInit or ClassInit:
                    ImportStatements.Add(statement);
                    break;
                default:
                    OtherStatements.Add(statement);
                    break;
            }
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

    public void ImportRun(ref VariateManager Manager)
    {
        foreach (var statement in ImportStatements)
        {
            statement.Run(ref Manager);
            if (Manager.IsReturn) return;
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var statement in ImportStatements)
            sb.Append(statement + Environment.NewLine);
        foreach (var statement in OtherStatements)
            sb.Append(statement + Environment.NewLine);
        
        return sb.ToString();
    }

    public string ToCode()
    {
        var sb = new StringBuilder();
        var import = ImportStatements.OfType<ImportStatement>().ToList();
        var func = ImportStatements.Where(x => x is ClassInit or FuncInit).ToList();
        foreach (var importStatement in import)
            sb.Append(importStatement);
        foreach (var statement in func)
            sb.Append(statement);
        sb.Append("class Project{public void Main(){");
        foreach (var statement in OtherStatements)
            sb.Append(statement);
        sb.Append("}}");
        return sb.ToString();
    }

    public Dictionary<OldID, OldExpr> ToAnyData()
    {
        var c = new Dictionary<OldID, OldExpr>();
        OtherStatements.ForEach(x =>
        {
            var result = GetTuple(x);
            c.Add(result.id, result.Expr);
        });
        ImportStatements.ForEach(x =>
        {
            var result = GetTuple(x);
            c.Add(result.id, result.Expr);
        });
        return c;
    }

    private static (OldID id, OldExpr Expr) GetTuple(OldLangTree a)
    {
        return a switch
        {
            SetStatement statement => (id: statement.Id, Expr: statement.Value),
            FuncInit init => (init.FuncValue.Id!, init.FuncValue),
            _ => (null!, null!)
        };
    }
}