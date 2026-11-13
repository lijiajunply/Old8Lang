using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Error;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// InterpreterVisitor - Statement 节点的 Visit 方法实现
/// </summary>
public partial class InterpreterVisitor
{
    /// <summary>
    /// 访问 ExtensionDeclaration 节点
    /// </summary>
    public LangValueType VisitExtensionDeclaration(ExtensionDeclaration node)
    {
        // 直接调用 Run 方法
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 BreakStatement 节点
    /// </summary>
    public LangValueType VisitBreakStatement(BreakStatement node)
    {
        // 迁移自 BreakStatement.Run()
        manager.ControlFlowManager.BreakFlag = true;
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 ContinueStatement 节点
    /// </summary>
    public LangValueType VisitContinueStatement(ContinueStatement node)
    {
        // 迁移自 ContinueStatement.Run()
        manager.ControlFlowManager.ContinueFlag = true;
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 IfStatement 节点
    /// </summary>
    public LangValueType VisitIfStatement(IfStatement node)
    {
        // 迁移自 IfStatement.Run()
        var r = true;

        // 保存原始的 IsFunc 状态
        var originalIsFunc = manager.IsFunc;

        // 处理 if 块
        manager.AddChildren();
        // 在 if 语句块中，临时禁用函数上下文，允许修改外部变量
        manager.IsFunc = false;

        // 访问 ifChildBlock（直接调用其逻辑，因为 IfChild 不支持 Visitor）
        var ifChild = node[0] as IfChild;
        if (ifChild is not null)
        {
            ifChild.Run(manager, ref r);
        }

        manager.RemoveChildren();

        // 处理 elif 块
        for (int i = 1; i < node.Count; i++)
        {
            var elifChild = node[i] as IfChild;
            if (elifChild is not null)
            {
                manager.AddChildren();
                // 在 elif 语句块中，临时禁用函数上下文，允许修改外部变量
                manager.IsFunc = false;
                elifChild.Run(manager, ref r);
                manager.RemoveChildren();
            }
            else if (node[i] is BlockStatement elseBlock)
            {
                // else 块
                if (r)
                {
                    elseBlock.Accept(this);
                }
            }
        }

        // 恢复原始的 IsFunc 状态
        manager.IsFunc = originalIsFunc;

        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 BlockStatement 节点
    /// </summary>
    public LangValueType VisitBlockStatement(BlockStatement node)
    {
        // 迁移自 BlockStatement.Run()
        // 检查是否有生成器上下文，决定执行模式
        if (manager.GeneratorContext is not null)
        {
            RunWithGeneratorContext(node);
        }
        else
        {
            // 标准执行模式（非生成器）
            // 先执行导入语句
            node.ImportRun(manager);

            // 顺序执行所有语句
            for (int i = 0; i < node.Count; i++)
            {
                var statement = node[i];
                if (statement is not null)
                {
                    // 未迁移的节点仍使用其旧执行方法；逐节点切换到 Visitor。
                    statement.Run(manager);

                    // 检查 return 语句
                    if (manager.IsReturn)
                    {
                        return new VoidLangValue();
                    }

                    // 检查 break 和 continue 语句
                    if (manager.ControlFlowManager.BreakFlag || manager.ControlFlowManager.ContinueFlag)
                    {
                        return new VoidLangValue();
                    }
                }
            }
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// 使用新架构运行（基于路径的状态恢复）
    /// </summary>
    /// <param name="node">正在执行的块语句。</param>
    private void RunWithGeneratorContext(BlockStatement node)
    {
        var context = manager.GeneratorContext!;

        // 先执行导入语句
        node.ImportRun(manager);

        // 检查是否需要恢复执行
        bool isResuming = !string.IsNullOrEmpty(context.ExecutionPath);

        // 从头开始执行，使用路径匹配决定是否跳过语句
        for (int i = 0; i < node.OtherStatements.Count; i++)
        {
            var statement = node.OtherStatements[i];

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
                        // 清除 ExecutionPath 和恢复标志，从下一个语句开始正常执行
                        context.ExecutionPath = "";
                        isResuming = false;
                        continue;
                    }

                    // 如果执行路径不以当前路径开头，说明这个语句在恢复点之前，跳过
                    if (!context.ExecutionPath!.StartsWith(currentPath))
                    {
                        continue;
                    }
                }

                // 保存当前的 IsReturn 标志，以防止嵌套函数调用影响生成器的控制流
                var savedIsReturn = manager.IsReturn;

                // 执行语句
                statement.Run(manager);

                // 恢复 IsReturn 标志（如果语句是普通函数调用，它会设置并重置 IsReturn，但我们需要确保生成器的 IsReturn 不受影响）
                // 注意：如果语句本身是 return 语句，我们需要保留它设置的 IsReturn
                if (statement is not ReturnStatement)
                {
                    manager.IsReturn = savedIsReturn;
                }

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

    /// <summary>
    /// 访问 WhileStatement 节点
    /// </summary>
    public LangValueType VisitWhileStatement(WhileStatement node)
    {
        // 迁移自 WhileStatement.Run()
        if (manager.GeneratorContext is not null)
        {
            // 生成器模式：调用原方法（暂不迁移复杂逻辑）
            node.Run(manager);
        }
        else
        {
            // 标准 while 循环（非生成器）
            manager.ControlFlowManager.PushState();

            try
            {
                while (true)
                {
                    // 获取条件表达式的值
                    var value = node[0].Accept(this); // expression

                    if (value is not BoolLangValue varBool)
                    {
                        throw new TypeError(node, "期望布尔类型", $"实际得到了 {value.GetType().Name}");
                    }

                    bool conditionResult = varBool.Value;

                    // 如果条件为 false，退出循环
                    if (!conditionResult)
                    {
                        break;
                    }

                    // 执行循环体（blockStatement）
                    // 注意：WhileStatement 没有索引访问来获取 blockStatement，需要直接调用原方法
                    node.Run(manager);
                    return new VoidLangValue();
                }
            }
            finally
            {
                manager.ControlFlowManager.PopState();
            }
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 ForStatement 节点
    /// </summary>
    public LangValueType VisitForStatement(ForStatement node)
    {
        // 迁移自 ForStatement.Run()
        manager.AddChildren();
        manager.ControlFlowManager.PushState();

        try
        {
            // 执行初始化语句（通过访问 setStatement 字段）
            // 注意：ForStatement 没有通过索引访问子节点，需要调用原方法
            node.Run(manager);
            return new VoidLangValue();
        }
        finally
        {
            manager.ControlFlowManager.PopState();
            manager.RemoveChildren();
        }
    }

    /// <summary>
    /// 访问 ForInStatement 节点
    /// </summary>
    public LangValueType VisitForInStatement(ForInStatement node)
    {
        // 迁移自 ForInStatement.Run()
        // ForInStatement 逻辑非常复杂（815行），包含生成器、异步流等特殊处理
        // 暂时调用原方法，后续再详细迁移
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 SetStatement 节点
    /// </summary>
    public LangValueType VisitSetStatement(SetStatement node)
    {
        // 迁移自 SetStatement.Run()
        // SetStatement 的逻辑已经封装在其 Run 方法中，直接调用
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 ReturnStatement 节点
    /// </summary>
    public LangValueType VisitReturnStatement(ReturnStatement node)
    {
        // 迁移自 ReturnStatement.Run()
        // ReturnStatement 的逻辑已经封装在其 Run 方法中，直接调用
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 ThrowStatement 节点
    /// </summary>
    public LangValueType VisitThrowStatement(ThrowStatement node)
    {
        // 迁移自 ThrowStatement.Run()
        // ThrowStatement 的逻辑已经封装在其 Run 方法中，直接调用
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 FuncInit 节点
    /// </summary>
    public LangValueType VisitFuncInit(FuncInit node)
    {
        // 迁移自 FuncInit.Run()
        // 函数声明逻辑已封装在 Run 方法中，直接调用
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 AsyncFuncInit 节点
    /// </summary>
    public LangValueType VisitAsyncFuncInit(AsyncFuncInit node)
    {
        // 迁移自 AsyncFuncInit.Run()
        // 异步函数声明逻辑已封装在 Run 方法中，直接调用
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 ClassInit 节点
    /// </summary>
    public LangValueType VisitClassInit(ClassInit node)
    {
        // 迁移自 ClassInit.Run()
        // 类声明逻辑已封装在 Run 方法中，直接调用
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 SwitchStatement 节点
    /// </summary>
    public LangValueType VisitSwitchStatement(SwitchStatement node)
    {
        // 迁移自 SwitchStatement.Run()
        // Switch 语句逻辑已封装在 Run 方法中，直接调用
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 CaseStatement 节点
    /// </summary>
    public LangValueType VisitCaseStatement(CaseStatement node)
    {
        // 迁移自 CaseStatement.Run()
        // Case 语句逻辑已封装在 Run 方法中，直接调用
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 TryStatement 节点
    /// </summary>
    public LangValueType VisitTryStatement(TryStatement node)
    {
        // 迁移自 TryStatement.Run()
        // Try-Catch-Finally 逻辑已封装在 Run 方法中，直接调用
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 YieldStatement 节点
    /// </summary>
    public LangValueType VisitYieldStatement(YieldStatement node)
    {
        // 迁移自 YieldStatement.Run()
        // Yield 语句逻辑（生成器支持）已封装在 Run 方法中，直接调用
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 ImportStatement 节点
    /// </summary>
    public LangValueType VisitImportStatement(ImportStatement node)
    {
        // 迁移自 ImportStatement.Run()
        // Import 语句逻辑已封装在 Run 方法中，直接调用
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 NativeStatement 节点
    /// </summary>
    public LangValueType VisitNativeStatement(NativeStatement node)
    {
        // 迁移自 NativeStatement.Run()
        // Native 方法绑定逻辑已封装在 Run 方法中，直接调用
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 AsyncForInStatement 节点
    /// </summary>
    public LangValueType VisitAsyncForInStatement(AsyncForInStatement node)
    {
        // 迁移自 AsyncForInStatement.Run()
        // 异步 for-in 循环逻辑已封装在 Run 方法中，直接调用
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 DeferStatement 节点
    /// </summary>
    public LangValueType VisitDeferStatement(DeferStatement node)
    {
        // 迁移自 DeferStatement.Run()
        // Defer 语句逻辑已封装在 Run 方法中，直接调用
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 SelectStatement 节点
    /// </summary>
    public LangValueType VisitSelectStatement(SelectStatement node)
    {
        // 迁移自 SelectStatement.Run()
        // Select 语句逻辑已封装在 Run 方法中，直接调用
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 EnumInit 节点
    /// </summary>
    public LangValueType VisitEnumInit(EnumInit node)
    {
        // 迁移自 EnumInit.Run()
        // 枚举定义逻辑已封装在 Run 方法中，直接调用
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 ExternStatement 节点
    /// </summary>
    public LangValueType VisitExternStatement(ExternStatement node)
    {
        // 迁移自 ExternStatement.Run()
        node.Run(manager);
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 FileHeaderDirective 节点
    /// </summary>
    public LangValueType VisitFileHeaderDirective(FileHeaderDirective node)
    {
        // 文件头指令在解释器模式下不需要执行
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 UsingStatement 节点
    /// </summary>
    public LangValueType VisitUsingStatement(UsingStatement node)
    {
        // 迁移自 UsingStatement.Run()
        node.Run(manager);
        return new VoidLangValue();
    }
}
