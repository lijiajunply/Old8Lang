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
                    // 所有成员都添加到其他语句列表中，通过修饰符区分静态和实例成员
                    OtherStatements.Add(statement);
                    break;
            }
        }
    }

    public override void Run(VariateManager manager)
    {
        // 先执行 ImportStatements 列表中的语句，包括 ClassInit 和 FuncInit 语句
        // 这样，当执行 OtherStatements 列表中的语句时，类和函数已经被添加到 ImportInfos 中了
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
        // 直接运行ImportStatements列表中的语句，不管IsCompileOptimization属性的值是什么
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

    /// <summary>
    /// 获取实例成员字典
    /// </summary>
    /// <returns>实例成员字典</returns>
    public Dictionary<ClassMemberId, OldExpr> ToAnyData()
    {
        var c = new Dictionary<ClassMemberId, OldExpr>();
        
        // 处理所有语句，筛选出非静态成员
        foreach (var x in OtherStatements.Concat(ImportStatements))
        {
            var (id, expr) = GetTuple(x);
            if (id != null! && expr != null!)
            {
                // 只添加非静态成员
                if (!id.HasModifier(AccessModifierType.Static))
                {
                    c.TryAdd(id, expr);
                }
            }
        }
        
        return c;
    }
    
    /// <summary>
    /// 获取静态成员字典
    /// </summary>
    /// <returns>静态成员字典</returns>
    public Dictionary<ClassMemberId, OldExpr> ToStaticData()
    {
        var c = new Dictionary<ClassMemberId, OldExpr>();
        
        // 处理所有语句，筛选出静态成员
        foreach (var x in OtherStatements.Concat(ImportStatements))
        {
            var (id, expr) = GetTuple(x);
            if (id != null! && expr != null!)
            {
                // 只添加静态成员
                if (id.HasModifier(AccessModifierType.Static))
                {
                    c.TryAdd(id, expr);
                }
            }
        }
        
        return c;
    }

    private static (ClassMemberId? id, OldExpr? Expr) GetTuple(IOldLangTree a)
    {
        switch (a)
        {
            case SetStatement statement:
                if (statement.Id == null) return (null, null);
                // 如果是 ClassMemberId 直接使用，否则转换
                var memberId1 = statement.Id is ClassMemberId classMemberId1 ? 
                    classMemberId1 : 
                    new ClassMemberId(statement.Id);
                return (id: memberId1, Expr: statement.Value);
            case ClassFieldSetStatement classFieldSet:
                // 直接使用 ClassFieldSetStatement 中的 ClassMemberId
                return (id: classFieldSet.Id, Expr: classFieldSet.Value);
            case FuncInit init:
                if (init.FuncLangValue.Id == null) return (null, null);
                // 如果是 ClassMemberId 直接使用，否则转换
                var memberId2 = init.FuncLangValue.Id is ClassMemberId classMemberId2 ? 
                    classMemberId2 : 
                    new ClassMemberId(init.FuncLangValue.Id);
                return (memberId2, Expr: init.FuncLangValue);
            case ClassFuncInitStatement classFuncInit:
                // 直接使用 ClassFuncInitStatement 中的 ClassMemberId
                return (id: classFuncInit.Id, Expr: classFuncInit.FuncValue);
            case ClassInit:
                // 对于 ClassInit，我们不需要将其转换为字典中的键值对
                return (null, null);
            default:
                return (null, null);
        }
    }

    public override OldStatement this[int index] => OtherStatements[index];
}