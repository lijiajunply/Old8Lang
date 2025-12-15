using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression;
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

    /// <summary>
    /// 生成器执行位置，用于恢复执行（仅在生成器上下文中使用）
    /// 每个 BlockStatement 实例有自己的执行位置
    /// </summary>
    private int _generatorExecutionPosition = 0;

    /// <summary>
    /// 重置生成器执行位置（用于循环语句在每次迭代开始时重置）
    /// </summary>
    public void ResetGeneratorPosition()
    {
        _generatorExecutionPosition = 0;
    }

    public BlockStatement(IEnumerable<IOldLangTree> statements, SourcePosition position = default) : base(position)
    {
        // 遍历所有语句
        foreach (var statement in statements.OfType<OldStatement>())
        {
            // 根据语句类型添加到不同的列表中
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

        // 从当前执行位置开始执行，用于生成器恢复执行
        for (int i = _generatorExecutionPosition; i < OtherStatements.Count; i++)
        {
            var statement = OtherStatements[i];
            statement.Run(manager);

            if (manager.IsReturn)
            {
                // 返回时重置执行位置，因为函数已经结束
                _generatorExecutionPosition = 0;
                return;
            }

            // 遇到yield，不管是什么语句，都立即返回
            if (manager.IsYield)
            {
                // 检查当前语句是否是yield语句本身，或者是循环语句
                if (statement is YieldStatement)
                {
                    // 对于直接yield语句，保存下一个语句的位置
                    _generatorExecutionPosition = i + 1;
                }
                else if (statement is WhileStatement || statement is ForStatement || statement is ForInStatement)
                {
                    // 对于循环语句中的yield，保持当前语句位置，让循环语句继续执行
                    _generatorExecutionPosition = i;
                }
                else
                {
                    // 对于其他语句（如if语句）中的yield，保存下一个语句的位置
                    _generatorExecutionPosition = i + 1;
                }
                return;
            }
        }

        // 执行完毕，重置执行位置
        _generatorExecutionPosition = 0;
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
            // 不要在ImportRun中检查IsReturn，因为这会导致主函数体无法执行
            // if (manager.IsReturn) return;
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
    public Dictionary<ClassMemberId, LangExpression> ToAnyData()
    {
        var c = new Dictionary<ClassMemberId, LangExpression>();

        // 处理所有语句，筛选出非静态成员
        foreach (var x in OtherStatements.Concat(ImportStatements))
        {
            var (id, expr) = GetTuple(x);
            if (id == null! || expr == null!) continue;
            // 只添加非静态成员
            if (!id.HasModifier(AccessModifierType.Static))
            {
                c.TryAdd(id, expr);
            }
        }

        return c;
    }

    /// <summary>
    /// 获取静态成员字典
    /// </summary>
    /// <returns>静态成员字典</returns>
    public Dictionary<ClassMemberId, LangExpression> ToStaticData()
    {
        var c = new Dictionary<ClassMemberId, LangExpression>();

        // 处理所有语句，筛选出静态成员
        foreach (var x in OtherStatements.Concat(ImportStatements))
        {
            var (id, expr) = GetTuple(x);
            if (id == null! || expr == null!) continue;
            // 只添加静态成员
            if (id.HasModifier(AccessModifierType.Static))
            {
                c.TryAdd(id, expr);
            }
        }

        return c;
    }

    private static (ClassMemberId? id, LangExpression? Expr) GetTuple(IOldLangTree a)
    {
        switch (a)
        {
            case SetStatement statement:
                if (statement.Id == null) return (null, null);
                // 如果是 ClassMemberId 直接使用，否则转换
                var memberId1 = statement.Id as ClassMemberId ?? new ClassMemberId(statement.Id);
                return (id: memberId1, Expr: statement.Value);
            case ClassFieldSetStatement classFieldSet:
                // 直接使用 ClassFieldSetStatement 中的 ClassMemberId
                return (id: classFieldSet.Id, Expr: classFieldSet.Value);
            case FuncInit init:
                if (init.FuncLangValue.Id == null) return (null, null);
                // 如果是 ClassMemberId 直接使用，否则转换
                var memberId2 = init.FuncLangValue.Id as ClassMemberId ?? new ClassMemberId(init.FuncLangValue.Id);
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