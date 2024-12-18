using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 块
/// </summary>
public class BlockStatement : OldStatement
{
    private readonly List<OldStatement> ImportStatements = [];
    private readonly List<OldStatement> OtherStatements = [];
    public override int Count => OtherStatements.Count;

    public BlockStatement(IEnumerable<OldLangTree> statements)
    {
        foreach (var statement in statements.OfType<OldStatement>())
        {
            switch (statement)
            {
                case ImportStatement or NativeStatement or FuncInit or ClassInit:
                    ImportStatements.Add(statement);
                    break;
                case ReturnStatement:
                    OtherStatements.Add(statement);
                    return;
                default:
                    OtherStatements.Add(statement);
                    break;
            }
        }
    }

    public override void Run(VariateManager Manager)
    {
        foreach (var statement in ImportStatements)
        {
            statement.Run(Manager);
            if (Manager.IsReturn) return;
        }

        foreach (var statement in OtherStatements)
        {
            statement.Run(Manager);
            if (Manager.IsReturn) return;
        }
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        foreach (var statement in ImportStatements)
        {
            statement.GenerateIL(ilGenerator, local);
        }

        foreach (var statement in OtherStatements)
        {
            statement.GenerateIL(ilGenerator, local);
        }
    }

    public void ImportRun(VariateManager Manager)
    {
        foreach (var statement in ImportStatements)
        {
            statement.Run(Manager);
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
        sb.AppendLine("using System;");
        foreach (var importStatement in import)
            sb.AppendLine(importStatement.ToString());
        sb.AppendLine("static class Program");
        sb.AppendLine("{");
        foreach (var statement in func)
            sb.AppendLine(statement.ToString());
        sb.AppendLine("public static void Main(string[] args)");
        sb.AppendLine("{");
        foreach (var statement in OtherStatements)
            sb.AppendLine(statement.ToString());
        sb.AppendLine("}");
        sb.AppendLine("}");
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

    public override OldStatement this[int index] => OtherStatements[index];
}