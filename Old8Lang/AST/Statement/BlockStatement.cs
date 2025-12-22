using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Generators;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 块
/// </summary>
public partial class BlockStatement : OldStatement
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
                case ImportStatement importStmt:
                    // 动态导入需要放在 OtherStatements 中，以便在正常语句流中执行
                    if (importStmt.IsDynamicImport)
                    {
                        OtherStatements.Add(statement);
                    }
                    else
                    {
                        ImportStatements.Add(statement);
                    }
                    break;
                case NativeStatement or FuncInit or ClassInit or AsyncFuncInit:
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

            // 检查 return 语句
            if (manager.IsReturn)
            {
                return;
            }

            // 检查 break 和 continue 语句
            // 如果在块内遇到 break 或 continue，应该立即停止执行后续语句
            if (manager.ControlFlowManager.BreakFlag || manager.ControlFlowManager.ContinueFlag)
            {
                return;
            }
        }
    }

    /// <summary>
    /// 使用新架构运行（基于路径的状态恢复）
    /// </summary>
    /// <param name="manager">变量管理器</param>
    private void RunWithGeneratorContext(VariateManager manager)
    {
        var context = manager.GeneratorContext!;

        // 先执行导入语句
        ImportRun(manager);

        // 检查是否需要恢复执行
        bool isResuming = !string.IsNullOrEmpty(context.ExecutionPath);

        // 从头开始执行，使用路径匹配决定是否跳过语句
        for (int i = 0; i < OtherStatements.Count; i++)
        {
            var statement = OtherStatements[i];

            // 构建当前语句的路径
            var statementPath = $"/block[{i}]";
            context.PathStack.Push(statementPath);

            try
            {
                var currentPath = context.GetCurrentPath();

                // 如果正在恢复执行，检查是否应该跳过当前语句
                if (isResuming)
                {
                    // 如果执行路径等于当前路径+"/yield"，说明这是上次yield的位置
                    // 应该跳过这个语句，从下一个语句开始执行
                    if (context.ExecutionPath == currentPath + "/yield")
                    {
                        // 清除恢复标志，从下一个语句开始正常执行
                        isResuming = false;
                        continue;
                    }

                    // 如果执行路径不以当前路径开头，说明这个语句在恢复点之前，跳过
                    if (!context.ExecutionPath!.StartsWith(currentPath))
                    {
                        continue;
                    }
                }

                // 执行语句
                statement.Run(manager);

                // 检查是否遇到yield
                if (context.HasYielded)
                {
                    return;
                }

                // 执行完语句后，如果 ExecutionPath 被清除，说明该语句已完成（比如循环结束）
                // 此时应该取消恢复模式，继续正常执行后续语句
                if (isResuming && string.IsNullOrEmpty(context.ExecutionPath))
                {
                    isResuming = false;
                }

                // 检查是否遇到return或其他控制流
                if (manager.IsReturn || context.IsCompleted)
                {
                    context.IsCompleted = true;
                    return;
                }

                // 检查 break 和 continue 语句
                if (manager.ControlFlowManager.BreakFlag || manager.ControlFlowManager.ContinueFlag)
                {
                    return;
                }
            }
            finally
            {
                // 弹出当前路径
                context.PathStack.Pop();
            }
        }

        // 执行完毕，只有主函数体的 BlockStatement 才设置 IsCompleted
        // 循环体等嵌套的 BlockStatement 不应该设置 IsCompleted
        // 通过检查路径栈来判断：如果栈为空，说明是主函数体
        if (context.PathStack.Count == 0)
        {
            context.IsCompleted = true;
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
                // 如果是函数重载，需要特殊处理
                if (IsFunction(expr) && c.ContainsKey(id))
                {
                    // 已有同名函数，创建函数重载
                    var existingExpr = c[id];
                    if (existingExpr is FuncLangValue existingFunc && expr is FuncLangValue currentFunc)
                    {
                        // 创建函数重载列表
                        var overloadList = new MethodOverloadList(new List<FuncLangValue> { existingFunc, currentFunc });
                        c[id] = overloadList;
                    }
                    else if (existingExpr is MethodOverloadList overloadList)
                    {
                        // 已有重载列表，添加新重载
                        if (expr is FuncLangValue funcToAdd)
                        {
                            overloadList.AddOverload(funcToAdd);
                        }
                    }
                }
                else
                {
                    // 普通成员或首次定义的函数
                    c[id] = expr;
                }
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
                // 如果是函数重载，需要特殊处理
                if (IsFunction(expr) && c.ContainsKey(id))
                {
                    // 已有同名函数，创建函数重载
                    var existingExpr = c[id];
                    if (existingExpr is FuncLangValue existingFunc && expr is FuncLangValue currentFunc)
                    {
                        // 创建函数重载列表
                        var overloadList = new MethodOverloadList(new List<FuncLangValue> { existingFunc, currentFunc });
                        c[id] = overloadList;
                    }
                    else if (existingExpr is MethodOverloadList overloadList)
                    {
                        // 已有重载列表，添加新重载
                        if (expr is FuncLangValue funcToAdd)
                        {
                            overloadList.AddOverload(funcToAdd);
                        }
                    }
                }
                else
                {
                    // 普通成员或首次定义的函数
                    c[id] = expr;
                }
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
            case ClassInit classInit:
                // 对于嵌套类，创建一个特殊的ClassMemberId来标识它
                var nestedClassId = new ClassMemberId(classInit.AnyLangValue.ClassName, "", [], classInit.Position);
                // 将嵌套类作为TypeTemplate存储
                return (id: nestedClassId, Expr: classInit.AnyLangValue);
            default:
                return (null, null);
        }
    }

    public override OldStatement this[int index] => OtherStatements[index];

    /// <summary>
    /// 检查表达式是否是函数
    /// </summary>
    /// <param name="expr">要检查的表达式</param>
    /// <returns>如果是函数返回true，否则返回false</returns>
    private static bool IsFunction(LangExpression expr)
    {
        return expr is FuncLangValue || expr is AsyncFuncLangValue;
    }
}