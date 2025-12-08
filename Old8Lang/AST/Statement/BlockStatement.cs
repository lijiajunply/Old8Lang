using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;

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
                manager.AddClassAndFunc(new FuncLangValue(info.Key, info.Value));
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
        // 输出所有导入和初始化语句
        foreach (var statement in ImportStatements)
            sb.AppendLine(statement.ToString());
        // 输出其他语句
        foreach (var statement in OtherStatements)
            sb.AppendLine(statement.ToString());
        return sb.ToString();
    }

    public Dictionary<LangId, OldExpr> ToAnyData()
    {
        var c = new Dictionary<LangId, OldExpr>();
        OtherStatements.ForEach(x =>
        {
            var (id, expr) = GetTuple(x);
            if (id != null! && expr != null!)
            {
                c.TryAdd(id, expr);
            }
        });
        ImportStatements.ForEach(x =>
        {
            var (id, expr) = GetTuple(x);
            if (id != null! && expr != null!)
            {
                c.TryAdd(id, expr);
            }
        });
        return c;
    }

    private static (LangId? id, OldExpr? Expr) GetTuple(IOldLangTree a)
    {
        return a switch
        {
            SetStatement statement => (id: statement.Id, Expr: statement.Value),
            FuncInit init => (init.FuncLangValue.Id!, Expr: init.FuncLangValue),
            // 对于 ClassInit，我们不需要将其转换为字典中的键值对
            ClassInit => (null, null),
            _ => (null, null)
        };
    }

    public override OldStatement this[int index] => OtherStatements[index];
}