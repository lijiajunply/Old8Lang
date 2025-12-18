using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;

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
        // 检查是否有生成器上下文，决定执行模式
        if (manager.GeneratorContext != null)
        {
            RunWithGeneratorContext(manager);
        }
        else
        {
            RunStandard(manager);
        }
    }

    /// <summary>
    /// 标准执行模式（非生成器）
    /// </summary>
    /// <param name="manager">变量管理器</param>
    private void RunStandard(VariateManager manager)
    {
        // 先执行导入语句
        ImportRun(manager);

        // 顺序执行所有语句
        foreach (var statement in OtherStatements)
        {
            statement.Run(manager);

            if (manager.IsReturn)
            {
                return;
            }
        }
    }

    /// <summary>
    /// 使用新的生成器上下文运行（新架构）
    /// </summary>
    /// <param name="manager">变量管理器</param>
    private void RunWithGeneratorContext(VariateManager manager)
    {
        var context = manager.GeneratorContext!;

        // 先执行导入语句
        ImportRun(manager);

        // 检查执行栈，如果栈不为空，说明需要跳转到栈顶指定的语句
        int startIndex = context.CurrentStatementIndex;
        if (context.ExecutionStack.Count > 0)
        {
            var frame = context.ExecutionStack.Peek();  // 查看栈顶，不弹出
            // 如果 StatementIndex 不是 -1，说明是真正的位置，需要跳转
            // 如果是 -1，说明是标志（如循环体标志），使用 CurrentStatementIndex
            if (frame.StatementIndex >= 0)
            {
                startIndex = frame.StatementIndex;
            }
        }

        // 从保存的位置开始执行
        for (int i = startIndex; i < OtherStatements.Count; i++)
        {
            var statement = OtherStatements[i];
            statement.Run(manager);

            // 检查是否遇到yield（通过生成器上下文而非全局标志）
            if (context.HasYielded)
            {
                // 保存当前位置，以便下次恢复
                if (statement is YieldStatement)
                {
                    // 对于直接yield语句，保存下一个语句的位置
                    context.CurrentStatementIndex = i + 1;
                }
                else if (statement is WhileStatement || statement is ForStatement || statement is ForInStatement)
                {
                    // 对于循环语句中的yield，将循环位置压栈
                    context.ExecutionStack.Push(new GeneratorExecutionContext.BlockExecutionFrame
                    {
                        StatementIndex = i,  // 保存循环语句的索引
                        BlockId = "loop"
                    });
                }
                else
                {
                    // 对于其他语句中的yield，保存下一个语句的位置
                    context.CurrentStatementIndex = i + 1;
                }
                return;
            }

            // 检查是否遇到return或其他控制流
            if (manager.IsReturn || context.IsCompleted)
            {
                // 标记生成器完成
                context.IsCompleted = true;
                context.CurrentStatementIndex = 0;
                return;
            }
        }

        // 执行完毕
        // 只有顶层的 BlockStatement 才标记为完成
        // 嵌套的 BlockStatement（如循环体）执行完毕不应该设置 IsCompleted
        if (context.ExecutionStack.Count == 0)
        {
            // 顶层 BlockStatement 执行完毕，标记为完成
            context.IsCompleted = true;
            context.CurrentStatementIndex = 0;
        }
        else
        {
            // 嵌套 BlockStatement 执行完毕，不设置 IsCompleted
            // 只重置 CurrentStatementIndex，准备下一次迭代
            context.CurrentStatementIndex = 0;
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
            // 不要在ImportRun中检查IsReturn，因为这会导致主函数体无法执行
            // if (manager.IsReturn) return;
        }
    }
    
    /// <summary>
    /// 执行模块的非导入语句，包括函数定义、类定义和变量赋值
    /// 但跳过导入语句，避免递归导入
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="skipFunctionClassInit">是否跳过函数定义和类定义，只执行变量赋值语句</param>
    public void ExecuteModule(VariateManager manager, bool skipFunctionClassInit = false)
    {
        // 执行ImportStatements中的非导入语句（函数定义、类定义）
        foreach (var statement in ImportStatements)
        {
            // 只跳过 ImportStatement 和 NativeStatement
            if (statement is not ImportStatement && statement is not NativeStatement)
            {
                // 如果跳过函数和类定义，则只执行其他类型的语句
                if (skipFunctionClassInit)
                {
                    // 跳过 FuncInit 和 ClassInit，只执行其他类型的语句
                    if (statement is not FuncInit && statement is not ClassInit)
                    {
                        statement.Run(manager);
                    }
                }
                else
                {
                    // 执行所有非导入语句，包括 FuncInit 和 ClassInit
                    statement.Run(manager);
                }
            }
        }
        
        // 执行所有OtherStatements（变量赋值等）
        foreach (var statement in OtherStatements)
        {
            statement.Run(manager);
            
            if (manager.IsReturn)
            {
                return;
            }
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