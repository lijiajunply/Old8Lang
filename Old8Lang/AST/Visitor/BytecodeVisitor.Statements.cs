using Old8Lang.AST;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Expression;

namespace Old8Lang.Bytecode;

/// <summary>
/// BytecodeVisitor - Statement节点的实现
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitBlockStatement(BlockStatement node)
    {
        // 遍历所有语句
        for (int i = 0; i < node.Count; i++)
        {
            var statement = node[i];
            statement.Accept(this);
        }

        return null;
    }

    public Instruction? VisitSetStatement(SetStatement node)
    {
        string varName = node.Id.IdName;

        // 生成右侧表达式的代码
        node.Value.Accept(this);

        // 检查是否是局部变量
        if (_compiler.IsLocalVariable(varName))
        {
            int localIndex = _compiler.GetLocalIndex(varName);
            Emit(OpCode.StoreLocal, localIndex);
        }
        else
        {
            // 声明为全局变量
            _compiler.DeclareGlobalVariable(varName);
            Emit(OpCode.StoreGlobal, varName);
        }

        return null;
    }

    public Instruction? VisitIfStatement(IfStatement node)
    {
        var elseLabel = new List<int>();
        var endLabel = -1;

        // 处理第一个if分支
        var ifChild = node.GetType().GetProperty("ifChildBlock")?.GetValue(node) as IfChild;
        if (ifChild != null)
        {
            ifChild.Accept(this);
            elseLabel.Add(GetCurrentPosition());
            Emit(OpCode.Jump, -1); // 跳转到结束,稍后修补
        }

        // 处理elif分支
        var elifBlocks = node.GetType().GetProperty("elifBlock")?.GetValue(node) as List<IfChild?> ?? new List<IfChild?>();
        foreach (var elif in elifBlocks.OfType<IfChild>())
        {
            // 修补上一个分支的跳转目标
            if (elseLabel.Count > 0)
            {
                int lastJump = elseLabel[elseLabel.Count - 1];
                PatchJump(lastJump, GetCurrentPosition());
            }

            elif.Accept(this);
            elseLabel.Add(GetCurrentPosition());
            Emit(OpCode.Jump, -1); // 跳转到结束
        }

        // 处理else分支
        var elseBlock = node.GetType().GetProperty("elseBlockStatement")?.GetValue(node) as BlockStatement;
        if (elseBlock != null)
        {
            // 修补最后一个分支的跳转
            if (elseLabel.Count > 0)
            {
                int lastJump = elseLabel[elseLabel.Count - 1];
                PatchJump(lastJump, GetCurrentPosition());
                elseLabel.RemoveAt(elseLabel.Count - 1);
            }

            elseBlock.Accept(this);
        }

        // 修补所有跳转到结束的指令
        endLabel = GetCurrentPosition();
        foreach (var jumpIndex in elseLabel)
        {
            PatchJump(jumpIndex, endLabel);
        }

        return null;
    }

    public Instruction? VisitIfChild(IfChild node)
    {
        // 获取expression和blockStatement属性
        var expression = node.GetType().GetProperty("expression")?.GetValue(node) as LangExpression;
        var blockStatement = node.GetType().GetProperty("blockStatement")?.GetValue(node) as BlockStatement;

        if (expression == null || blockStatement == null)
        {
            throw new Exception("IfChild节点缺少必要的属性");
        }

        // 生成条件表达式代码
        expression.Accept(this);

        // 如果条件为false,跳转到下一个分支
        int jumpIfFalseIndex = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1);

        // 生成if块代码
        blockStatement.Accept(this);

        // 修补跳转目标
        PatchJump(jumpIfFalseIndex, GetCurrentPosition());

        return null;
    }

    public Instruction? VisitWhileStatement(WhileStatement node)
    {
        // 获取expression和blockStatement属性
        var expression = node.GetType().GetProperty("expression")?.GetValue(node) as LangExpression;
        var blockStatement = node.GetType().GetProperty("blockStatement")?.GetValue(node) as OldStatement;

        if (expression == null || blockStatement == null)
        {
            throw new Exception("WhileStatement节点缺少必要的属性");
        }

        int loopStart = GetCurrentPosition();

        // 生成条件代码
        expression.Accept(this);

        // 如果条件为false,跳出循环
        int jumpIfFalseIndex = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1);

        // 生成循环体代码
        blockStatement.Accept(this);

        // 跳回循环开始
        Emit(OpCode.Jump, loopStart);

        // 修补跳出循环的跳转
        PatchJump(jumpIfFalseIndex, GetCurrentPosition());

        return null;
    }

    public Instruction? VisitForStatement(ForStatement node)
    {
        // 获取ForStatement的属性(主构造函数参数)
        var setStatement = node.GetType().GetProperty("setStatement")?.GetValue(node) as SetStatement;
        var expression = node.GetType().GetProperty("expression")?.GetValue(node) as LangExpression;
        var statement = node.GetType().GetProperty("statement")?.GetValue(node) as OldStatement;
        var blockStatement = node.GetType().GetProperty("blockStatement")?.GetValue(node) as BlockStatement;

        // 初始化
        if (setStatement != null)
            setStatement.Accept(this);

        int loopStart = GetCurrentPosition();

        // 条件
        if (expression != null)
        {
            expression.Accept(this);

            int jumpIfFalseIndex = GetCurrentPosition();
            Emit(OpCode.JumpIfFalse, -1);

            // 生成循环体代码
            blockStatement?.Accept(this);

            // 增量
            if (statement != null)
                statement.Accept(this);

            // 跳回循环开始
            Emit(OpCode.Jump, loopStart);

            // 修补跳出循环
            PatchJump(jumpIfFalseIndex, GetCurrentPosition());
        }
        else
        {
            // 无条件循环
            blockStatement?.Accept(this);

            if (statement != null)
                statement.Accept(this);

            Emit(OpCode.Jump, loopStart);
        }

        return null;
    }

    public Instruction? VisitReturnStatement(ReturnStatement node)
    {
        // 获取returnExpression属性(主构造函数参数)
        var returnExpression = node.GetType().GetProperty("returnExpression")?.GetValue(node) as LangExpression;

        if (returnExpression != null)
        {
            returnExpression.Accept(this);
            Emit(OpCode.Return);
        }
        else
        {
            Emit(OpCode.ReturnVoid);
        }

        return null;
    }

    public Instruction? VisitBreakStatement(BreakStatement node)
    {
        // TODO: 实现break跳转
        Emit(OpCode.Nop);
        return null;
    }

    public Instruction? VisitContinueStatement(ContinueStatement node)
    {
        // TODO: 实现continue跳转
        Emit(OpCode.Nop);
        return null;
    }

    public Instruction? VisitFuncInit(FuncInit node)
    {
        // 编译函数定义
        var funcValue = node.FuncLangValue;
        var funcName = funcValue.Id?.IdName ?? "<lambda>";
        var paramNames = funcValue.Ids?.Select(id => id.IdName).ToList() ?? new List<string>();

        _compiler.CompileFunction(funcName, paramNames, funcValue.BlockStatement);
        return null;
    }

    // ===== 其他语句 - 默认实现 =====

    public Instruction? VisitForInStatement(ForInStatement node)
    {
        // For-in 循环：for item in collection { ... }
        // 获取属性
        var id = node.GetType().GetProperty("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(node) as LangId;
        var expression = node.GetType().GetProperty("expression", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(node) as LangExpression;
        var body = node.GetType().GetProperty("body", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(node) as OldStatement;

        if (id == null || expression == null || body == null)
        {
            return null;
        }

        string varName = id.IdName;

        // 生成集合表达式的代码
        expression.Accept(this);

        // TODO: 完整实现需要：
        // 1. 调用集合的迭代器方法
        // 2. 在循环中调用 MoveNext 和获取 Current
        // 3. 将当前元素存储到循环变量
        //
        // 简化实现：暂时不支持 for-in 循环
        // 需要在 VM 中添加迭代器支持

        Emit(OpCode.Pop); // 弹出集合

        return null;
    }

    public Instruction? VisitSwitchStatement(SwitchStatement node)
    {
        // 获取属性
        var switchExpression = node.GetType().GetProperty("switchExpression", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(node) as LangExpression;
        var switchCaseList = node.GetType().GetProperty("switchCaseList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(node) as List<CaseStatement>;
        var defaultBlockStatement = node.GetType().GetProperty("defaultBlockStatement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(node) as BlockStatement;

        if (switchExpression == null || switchCaseList == null)
        {
            return null;
        }

        // 生成 switch 表达式的代码
        switchExpression.Accept(this);

        var caseEndLabels = new List<int>();
        var nextCaseLabels = new List<int>();

        // 为每个 case 生成代码
        foreach (var caseStmt in switchCaseList)
        {
            // 复制 switch 值用于比较
            Emit(OpCode.Dup);

            // 生成 case 表达式的代码
            caseStmt.Accept(this);

            // 比较是否相等
            Emit(OpCode.Equal);

            // 如果不相等，跳转到下一个 case
            int nextCaseLabel = GetCurrentPosition();
            Emit(OpCode.JumpIfFalse, -1);
            nextCaseLabels.Add(nextCaseLabel);

            // 弹出 switch 值（已经匹配成功）
            Emit(OpCode.Pop);

            // 执行 case 块
            var blockStmt = caseStmt.GetType().GetProperty("BlockStatement")?.GetValue(caseStmt) as BlockStatement;
            blockStmt?.Accept(this);

            // 跳转到 switch 结束
            int endLabel = GetCurrentPosition();
            Emit(OpCode.Jump, -1);
            caseEndLabels.Add(endLabel);

            // 修补下一个 case 的跳转标签
            PatchJump(nextCaseLabel, GetCurrentPosition());
        }

        // 如果有 default 块
        if (defaultBlockStatement != null)
        {
            // 弹出 switch 值
            Emit(OpCode.Pop);

            // 执行 default 块
            defaultBlockStatement.Accept(this);
        }
        else
        {
            // 没有 default，弹出 switch 值
            Emit(OpCode.Pop);
        }

        // 修补所有跳转到结束的标签
        int endPosition = GetCurrentPosition();
        foreach (var label in caseEndLabels)
        {
            PatchJump(label, endPosition);
        }

        return null;
    }

    public Instruction? VisitCaseStatement(CaseStatement node)
    {
        // CaseStatement 的表达式在 SwitchStatement 中处理
        // 这里只需要生成 case 表达式的代码
        var expression = node.GetType().GetProperty("expression", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(node) as LangExpression;
        expression?.Accept(this);
        return null;
    }

    public Instruction? VisitClassInit(ClassInit node)
    {
        // 类定义编译
        // TODO: 完整的类定义支持需要：
        // 1. 在字节码文件中添加类元数据
        // 2. 编译类的构造函数和方法
        // 3. 支持类的继承和多态
        //
        // 简化实现：暂时不支持类定义
        // 类定义在字节码层面是一个复杂的特性

        return null;
    }

    public Instruction? VisitImportStatement(ImportStatement node)
    {
        // 导入语句
        // 在字节码层面，导入通常在编译时处理
        // 运行时不需要生成指令
        return null;
    }

    public Instruction? VisitNativeStatement(NativeStatement node)
    {
        // 原生绑定语句
        // 在字节码层面，原生函数注册在编译时处理
        // 运行时不需要生成指令
        return null;
    }
    public Instruction? VisitTryStatement(TryStatement node)
    {
        // Try-Catch-Finally 异常处理
        // 简化实现：在字节码层面暂不支持完整的异常处理机制
        // 只执行 try 块，暂时忽略 catch 和 finally
        // TODO: 实现完整的异常处理机制，需要在 VM 中添加异常表支持

        // 执行 try 块
        node.TryBlock.Accept(this);

        // 注意：这是一个简化实现
        // 完整实现需要：
        // 1. 在字节码文件中添加异常表
        // 2. 记录 try 块的起始和结束位置
        // 3. 记录 catch 块的位置和捕获的异常类型
        // 4. 在 VM 中实现异常分发机制

        return null;
    }

    public Instruction? VisitThrowStatement(ThrowStatement node)
    {
        // 获取 expression 属性
        var expression = node.GetType().GetProperty("expression", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(node) as LangExpression;

        if (expression != null)
        {
            // 计算异常表达式的值
            expression.Accept(this);

            // 抛出异常
            Emit(OpCode.Throw);
        }

        return null;
    }
    public Instruction? VisitYieldStatement(YieldStatement node)
    {
        // Yield 语句（生成器）
        // TODO: 完整的生成器支持需要：
        // 1. 在字节码层面实现协程/生成器机制
        // 2. 保存和恢复执行状态
        // 3. 支持 yield 暂停和恢复
        //
        // 简化实现：暂时不支持生成器
        // 生成器在字节码层面是一个复杂的特性

        return null;
    }

    public Instruction? VisitAsyncForInStatement(AsyncForInStatement node)
    {
        // 异步 for-in 循环
        // TODO: 完整的异步支持需要：
        // 1. 异步迭代器支持
        // 2. await 表达式支持
        //
        // 简化实现：暂时不支持异步 for-in

        return null;
    }

    public Instruction? VisitAsyncFuncInit(AsyncFuncInit node)
    {
        // 异步函数定义
        // TODO: 完整的异步函数支持需要：
        // 1. 状态机转换
        // 2. Task/Promise 机制
        // 3. await 点的暂停和恢复
        //
        // 简化实现：暂时不支持异步函数

        return null;
    }

    public Instruction? VisitSelectStatement(SelectStatement node)
    {
        // Select 语句（Channel 多路选择）
        // TODO: 完整的 select 支持需要：
        // 1. Channel 操作的轮询机制
        // 2. 非阻塞的发送和接收
        // 3. 默认分支支持
        //
        // 简化实现：暂时不支持 select 语句

        return null;
    }
}
