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
            // 生成器模式需要调用原方法（暂不迁移复杂逻辑）
            node.Run(manager);
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
                    statement.Accept(this);

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
}
