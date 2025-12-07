using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 块
/// </summary>
public class BlockStatement : OldStatement
{
    private readonly List<OldStatement> ImportStatements = [];
    private readonly List<OldStatement> OtherStatements = [];
    public override int Count => OtherStatements.Count;

    public BlockStatement(IEnumerable<IOldLangTree> statements, SourcePosition position = default) : base(position)
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

    public override void Run(VariateManager manager)
    {
        ImportRun(manager);

        foreach (var statement in OtherStatements)
        {
            statement.Run(manager);
            if (manager.IsReturn) return;
        }
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        foreach (var statement in ImportStatements)
        {
            statement.GenerateIl(ilGenerator, local);
        }

        foreach (var statement in OtherStatements)
        {
            statement.GenerateIl(ilGenerator, local);
        }
    }
    
    public void GenerateImportIl(ILGenerator ilGenerator, LocalManager local)
    {
        foreach (var statement in ImportStatements)
        {
            statement.GenerateIl(ilGenerator, local);
        }
    }

    public void ImportRun(VariateManager manager)
    {
        if (manager.Interpreter is { IsCompileOptimization: true })
        {
            var dynamicMethod = new DynamicMethod("OldLangRun", null, null, true);
            var ilGenerator = dynamicMethod.GetILGenerator();
            var local = new LocalManager();
            var block = new BlockStatement(ImportStatements, Position);
            block.GenerateIl(ilGenerator, local);
            ilGenerator.Emit(OpCodes.Ret);
            foreach (var info in local.DelegateVar)
            {
                manager.AddClassAndFunc(new FuncValue(info.Key, info.Value));
            }

            return;
        }

        foreach (var statement in ImportStatements)
        {
            statement.Run(manager);
            if (manager.IsReturn) return;
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

    public Dictionary<OldId, OldExpr> ToAnyData()
    {
        var c = new Dictionary<OldId, OldExpr>();
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

    private static (OldId id, OldExpr Expr) GetTuple(IOldLangTree a)
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