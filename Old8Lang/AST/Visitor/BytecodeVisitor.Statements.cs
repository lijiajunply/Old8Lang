using Old8Lang.AST;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.LangParser;

namespace Old8Lang.Bytecode;

/// <summary>
/// BytecodeVisitor - Statement节点的实现
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitBlockStatement(BlockStatement node)
    {
        // 先处理导入语句（函数定义、类定义等）
        foreach (var statement in node.ImportStatements)
        {
            if (statement is OldStatement oldStatement)
            {
                oldStatement.Accept(this);
            }
        }

        // 再处理其他语句
        foreach (var statement in node.OtherStatements)
        {
            statement.Accept(this);
        }

        return null;
    }

    public Instruction? VisitSetStatement(SetStatement node)
    {
        // 检查是普通变量赋值还是索引/成员访问赋值
        if (node.Id != null)
        {
            // 普通变量赋值: x <- value
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
        }
        else
        {
            // 索引/成员访问赋值: array[i] <- value 或 obj.field <- value
            // 使用反射直接访问 LeftExpression 字段
            var leftExprField = node.GetType().GetField("LeftExpression",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var leftExpr = leftExprField?.GetValue(node) as LangExpression;

            if (leftExpr is LangListItem listItem)
            {
                // 数组/列表索引赋值: array[index] <- value
                // SetIndex期望栈布局(从栈顶到栈底): value, index, collection
                // 所以我们需要按相反顺序压栈: collection, index, value

                // 加载集合
                listItem.ListId.Accept(this);

                // 加载索引
                listItem.Key.Accept(this);

                // 加载值
                node.Value.Accept(this);

                // 发出SetIndex指令
                Emit(OpCode.SetIndex);
            }
            else if (leftExpr is Operation operation && operation.Opera == LangTokenType.Dot)
            {
                // 成员访问赋值: obj.field <- value 或 super.field <- value

                // 获取字段名
                string fieldName;
                if (operation.Right is LangId rightId)
                {
                    fieldName = rightId.IdName;
                }
                else
                {
                    // 字节码模式目前只支持简单的成员访问（obj.field）
                    // 不支持复杂的成员访问表达式（如 obj.method().field）
                    throw new NotSupportedException($"字节码模式下不支持的成员访问右侧类型: {operation.Right?.GetType().Name}，只支持简单标识符");
                }

                // 检查是否是 super.field <- value
                if (operation.Left is SuperExpression)
                {
                    // super.field <- value
                    // SetSuperField期望栈布局(从栈顶到栈底): value, this

                    // 加载 this (通过访问 SuperExpression,它会发出 LoadSuper 指令)
                    operation.Left.Accept(this);

                    // 加载值
                    node.Value.Accept(this);

                    // 发出SetSuperField指令
                    Emit(OpCode.SetSuperField, fieldName);
                }
                else
                {
                    // obj.field <- value
                    // SetField期望栈布局(从栈顶到栈底): value, object
                    // 所以我们需要按相反顺序压栈: object, value

                    // 加载对象
                    operation.Left?.Accept(this);

                    // 加载值
                    node.Value.Accept(this);

                    // 发出SetField指令
                    Emit(OpCode.SetField, fieldName);
                }
            }
            else if (leftExpr != null)
            {
                // 字节码模式目前只支持以下赋值类型：
                // 1. 简单变量赋值 (x <- value)
                // 2. 索引赋值 (arr[i] <- value)
                // 3. 成员访问赋值 (obj.field <- value)
                throw new NotSupportedException($"字节码模式下不支持的赋值左侧表达式类型: {leftExpr.GetType().Name}");
            }
        }

        return null;
    }

    public Instruction? VisitIfStatement(IfStatement node)
    {
        var elseLabel = new List<int>();
        var endLabel = -1;

        // 处理第一个if分支（主构造函数参数）
        var ifChild = GetPrimaryConstructorParameter<IfChild>(node, "ifChildBlock");
        if (ifChild != null)
        {
            // 手动处理IfChild（不使用Accept，因为IfChild不支持Visitor）
            var expression = GetPrimaryConstructorParameter<LangExpression>(ifChild, "expression");
            var blockStatement = GetPrimaryConstructorParameter<BlockStatement>(ifChild, "blockStatement");

            if (expression != null && blockStatement != null)
            {
                // 生成条件表达式代码
                expression.Accept(this);

                // 如果条件为false,跳转到下一个分支
                int jumpIfFalseIndex = GetCurrentPosition();
                Emit(OpCode.JumpIfFalse, -1);

                // 生成if块代码
                blockStatement.Accept(this);

                // 跳转到结束
                elseLabel.Add(GetCurrentPosition());
                Emit(OpCode.Jump, -1);

                // 修补跳转目标（跳到下一个分支）
                PatchJump(jumpIfFalseIndex, GetCurrentPosition());
            }
        }

        // 处理elif分支（主构造函数参数）
        var elifBlocks = GetPrimaryConstructorParameter<List<IfChild?>>(node, "elifBlock") ?? new List<IfChild?>();
        foreach (var elif in elifBlocks.OfType<IfChild>())
        {
            // 手动处理IfChild
            var expression = GetPrimaryConstructorParameter<LangExpression>(elif, "expression");
            var blockStatement = GetPrimaryConstructorParameter<BlockStatement>(elif, "blockStatement");

            if (expression != null && blockStatement != null)
            {
                // 生成条件表达式代码
                expression.Accept(this);

                // 如果条件为false,跳转到下一个分支
                int jumpIfFalseIndex = GetCurrentPosition();
                Emit(OpCode.JumpIfFalse, -1);

                // 生成elif块代码
                blockStatement.Accept(this);

                // 跳转到结束
                elseLabel.Add(GetCurrentPosition());
                Emit(OpCode.Jump, -1);

                // 修补跳转目标（跳到下一个分支）
                PatchJump(jumpIfFalseIndex, GetCurrentPosition());
            }
        }

        // 处理else分支（主构造函数参数）
        var elseBlock = GetPrimaryConstructorParameter<BlockStatement>(node, "elseBlockStatement");
        if (elseBlock != null)
        {
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
        // 获取expression和blockStatement字段（主构造函数参数）
        var expression = GetPrimaryConstructorParameter<LangExpression>(node, "expression");
        var blockStatement = GetPrimaryConstructorParameter<BlockStatement>(node, "blockStatement");

        if (expression == null || blockStatement == null)
        {
            throw new Exception("IfChild节点缺少必要的字段");
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
        // 获取expression和blockStatement字段（主构造函数参数）
        var expression = GetPrimaryConstructorParameter<LangExpression>(node, "expression");
        var blockStatement = GetPrimaryConstructorParameter<OldStatement>(node, "blockStatement");

        if (expression == null || blockStatement == null)
        {
            throw new Exception("WhileStatement节点缺少必要的字段");
        }

        // 创建循环标签
        var loopLabels = new LoopLabels();
        _loopLabels.Push(loopLabels);

        int loopStart = GetCurrentPosition();
        loopLabels.ContinueTarget = loopStart;

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
        int loopEnd = GetCurrentPosition();
        PatchJump(jumpIfFalseIndex, loopEnd);

        // 修补所有break跳转
        foreach (var breakJump in loopLabels.BreakJumps)
        {
            PatchJump(breakJump, loopEnd);
        }

        // 修补所有continue跳转
        foreach (var continueJump in loopLabels.ContinueJumps)
        {
            PatchJump(continueJump, loopLabels.ContinueTarget);
        }

        _loopLabels.Pop();

        return null;
    }

    public Instruction? VisitForStatement(ForStatement node)
    {
        // 获取ForStatement的字段（主构造函数参数）
        var setStatement = GetPrimaryConstructorParameter<SetStatement>(node, "setStatement");
        var expression = GetPrimaryConstructorParameter<LangExpression>(node, "expression");
        var statement = GetPrimaryConstructorParameter<OldStatement>(node, "statement");
        var blockStatement = GetPrimaryConstructorParameter<BlockStatement>(node, "blockStatement");

        // 创建循环标签
        var loopLabels = new LoopLabels();
        _loopLabels.Push(loopLabels);

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

            // continue跳转到这里(增量语句之前)
            int continueTarget = GetCurrentPosition();
            loopLabels.ContinueTarget = continueTarget;

            // 增量
            if (statement != null)
                statement.Accept(this);

            // 跳回循环开始
            Emit(OpCode.Jump, loopStart);

            // 修补跳出循环
            int loopEnd = GetCurrentPosition();
            PatchJump(jumpIfFalseIndex, loopEnd);

            // 修补所有break跳转
            foreach (var breakJump in loopLabels.BreakJumps)
            {
                PatchJump(breakJump, loopEnd);
            }

            // 修补所有continue跳转
            foreach (var continueJump in loopLabels.ContinueJumps)
            {
                PatchJump(continueJump, continueTarget);
            }
        }
        else
        {
            // 无条件循环
            blockStatement?.Accept(this);

            // continue跳转到这里(增量语句之前)
            int continueTarget = GetCurrentPosition();
            loopLabels.ContinueTarget = continueTarget;

            if (statement != null)
                statement.Accept(this);

            Emit(OpCode.Jump, loopStart);

            // 修补所有break跳转(无条件循环的break跳到循环后)
            int loopEnd = GetCurrentPosition();
            foreach (var breakJump in loopLabels.BreakJumps)
            {
                PatchJump(breakJump, loopEnd);
            }

            // 修补所有continue跳转
            foreach (var continueJump in loopLabels.ContinueJumps)
            {
                PatchJump(continueJump, continueTarget);
            }
        }

        _loopLabels.Pop();

        return null;
    }

    public Instruction? VisitReturnStatement(ReturnStatement node)
    {
        // 获取returnExpression字段（主构造函数参数）
        var returnExpression = GetPrimaryConstructorParameter<LangExpression>(node, "returnExpression");

        // 在返回前执行所有 defer 块
        Emit(OpCode.ExecuteDefers);

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
        // 检查是否在循环内部
        if (_loopLabels.Count == 0)
        {
            throw new Exception("break语句只能在循环内部使用");
        }

        // 记录需要修补的跳转位置
        var currentLoop = _loopLabels.Peek();
        currentLoop.BreakJumps.Add(GetCurrentPosition());

        // 发出跳转指令(目标位置稍后修补)
        Emit(OpCode.Jump, -1);

        return null;
    }

    public Instruction? VisitContinueStatement(ContinueStatement node)
    {
        // 检查是否在循环内部
        if (_loopLabels.Count == 0)
        {
            throw new Exception("continue语句只能在循环内部使用");
        }

        // 记录需要修补的跳转位置
        var currentLoop = _loopLabels.Peek();
        currentLoop.ContinueJumps.Add(GetCurrentPosition());

        // 发出跳转指令(目标位置稍后修补)
        Emit(OpCode.Jump, -1);

        return null;
    }

    public Instruction? VisitFuncInit(FuncInit node)
    {
        // 编译函数定义
        var funcValue = node.FuncLangValue;
        var funcName = funcValue.Id?.IdName ?? "<lambda>";
        var paramNames = funcValue.Ids?.Select(id => id.IdName).ToList() ?? new List<string>();

        // 提取默认参数值
        var defaultValues = new List<object?>();
        if (funcValue.Ids != null)
        {
            foreach (var param in funcValue.Ids)
            {
                if (param.DefaultValue != null)
                {
                    // 尝试计算默认值（仅支持常量表达式）
                    var defaultValue = EvaluateConstantExpression(param.DefaultValue);
                    defaultValues.Add(defaultValue);
                }
                else
                {
                    defaultValues.Add(null);
                }
            }
        }

        _compiler.CompileFunction(funcName, paramNames, defaultValues, funcValue.BlockStatement);
        return null;
    }

    // ===== 其他语句 - 默认实现 =====

    public Instruction? VisitForInStatement(ForInStatement node)
    {
        // For-in 循环：for item in collection { ... }
        // 获取字段（主构造函数参数）
        var id = GetPrimaryConstructorParameter<LangId>(node, "id");
        var expression = GetPrimaryConstructorParameter<LangExpression>(node, "expression");
        var body = GetPrimaryConstructorParameter<OldStatement>(node, "body");

        if (id == null || expression == null || body == null)
        {
            return null;
        }

        string varName = id.IdName;

        // 创建循环标签
        var loopLabels = new LoopLabels();
        _loopLabels.Push(loopLabels);

        // 生成集合表达式的代码（栈上现在有集合）
        expression.Accept(this);

        // 获取迭代器（栈上现在有迭代器）
        Emit(OpCode.GetIterator);

        // 将迭代器保存到一个临时局部变量
        int iteratorLocalIndex = _compiler.AllocateLocal("<iterator>");
        Emit(OpCode.StoreLocal, iteratorLocalIndex);

        // 循环开始标签
        int loopStart = GetCurrentPosition();
        loopLabels.ContinueTarget = loopStart;

        // 加载迭代器到栈
        Emit(OpCode.LoadLocal, iteratorLocalIndex);

        // 调用 MoveNext（栈：迭代器 → 迭代器, hasNext）
        Emit(OpCode.IteratorMoveNext);

        // 如果 MoveNext 返回 false，跳出循环
        int jumpIfFalse = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1);

        // 加载迭代器到栈
        Emit(OpCode.LoadLocal, iteratorLocalIndex);

        // 获取当前元素（栈：迭代器 → 迭代器, current）
        Emit(OpCode.IteratorCurrent);

        // 将当前元素存储到循环变量
        if (_compiler.IsLocalVariable(varName))
        {
            int localIndex = _compiler.GetLocalIndex(varName);
            Emit(OpCode.StoreLocal, localIndex);
        }
        else
        {
            // 声明为局部变量
            int localIndex = _compiler.DeclareLocalVariable(varName);
            Emit(OpCode.StoreLocal, localIndex);
        }

        // 执行循环体
        body.Accept(this);

        // 跳回循环开始
        Emit(OpCode.Jump, loopStart);

        // 修补跳出循环的跳转
        int loopEnd = GetCurrentPosition();
        PatchJump(jumpIfFalse, loopEnd);

        // 修补所有break跳转
        foreach (var breakJump in loopLabels.BreakJumps)
        {
            PatchJump(breakJump, loopEnd);
        }

        // 修补所有continue跳转
        foreach (var continueJump in loopLabels.ContinueJumps)
        {
            PatchJump(continueJump, loopLabels.ContinueTarget);
        }

        _loopLabels.Pop();

        // 释放迭代器局部变量
        _compiler.FreeLocal(iteratorLocalIndex);

        return null;
    }

    public Instruction? VisitSwitchStatement(SwitchStatement node)
    {
        // 获取字段（主构造函数参数）
        var switchExpression = GetPrimaryConstructorParameter<LangExpression>(node, "switchExpression");
        var switchCaseList = GetPrimaryConstructorParameter<List<CaseStatement>>(node, "switchCaseList");
        var defaultBlockStatement = GetPrimaryConstructorParameter<BlockStatement>(node, "defaultBlockStatement");

        if (switchExpression == null || switchCaseList == null)
        {
            return null;
        }

        // 生成 switch 表达式的代码（栈上有 switch 值）
        switchExpression.Accept(this);

        var caseEndLabels = new List<int>();

        // 为每个 case 生成代码
        for (int i = 0; i < switchCaseList.Count; i++)
        {
            var caseStmt = switchCaseList[i];

            // 复制 switch 值用于比较（栈上现在有 2 个 switch 值）
            Emit(OpCode.Dup);

            // 生成 case 表达式的代码（栈上现在有 2 个 switch 值 + case 值）
            // 直接访问 expression 属性而不是调用 Accept
            caseStmt.expression.Accept(this);

            // 比较是否相等（弹出 2 个值，栈上现在有 1 个 switch 值 + 比较结果）
            Emit(OpCode.Equal);

            // 如果不相等，跳转到下一个 case（弹出比较结果，栈上还有 1 个 switch 值）
            int jumpIfFalse = GetCurrentPosition();
            Emit(OpCode.JumpIfFalse, -1);

            // 匹配成功：弹出 switch 值（栈为空）
            Emit(OpCode.Pop);

            // 执行 case 块：直接访问 BlockStatement 属性
            caseStmt.BlockStatement.Accept(this);

            // 跳转到 switch 结束
            int jumpEnd = GetCurrentPosition();
            Emit(OpCode.Jump, -1);
            caseEndLabels.Add(jumpEnd);

            // 修补"不匹配"的跳转：跳到这里时栈上还有 switch 值
            PatchJump(jumpIfFalse, GetCurrentPosition());
        }

        // 所有 case 都不匹配，执行 default 块
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

        // 修补所有"匹配成功后跳转到结束"的指令
        int endPosition = GetCurrentPosition();
        foreach (var label in caseEndLabels)
        {
            PatchJump(label, endPosition);
        }

        return null;
    }

    public Instruction? VisitCaseStatement(CaseStatement node)
    {
        // CaseStatement 在 SwitchStatement 中已完整处理
        // 这个方法不应该被直接调用
        return null;
    }

    public Instruction? VisitClassInit(ClassInit node)
    {
        // 类定义编译
        // 从 TypeTemplate 中提取类名、字段和方法
        var typeTemplate = node.AnyLangValue;
        string className = typeTemplate.ClassName;
        var fields = new List<string>();
        var methods = new List<(string methodName, FuncLangValue funcValue, bool isStatic)>();

        // 遍历实例成员，提取字段和方法
        foreach (var (memberId, memberExpr) in typeTemplate.Variates)
        {
            if (memberExpr is FuncLangValue funcValue)
            {
                // 这是一个实例方法
                methods.Add((memberId.IdName, funcValue, false));
            }
            else
            {
                // 这是一个实例字段
                fields.Add(memberId.IdName);
            }
        }

        // 遍历静态成员，提取静态方法
        foreach (var (memberId, memberExpr) in typeTemplate.StaticVariates)
        {
            if (memberExpr is FuncLangValue funcValue)
            {
                // 这是一个静态方法
                methods.Add((memberId.IdName, funcValue, true));
            }
        }

        // 在编译器中注册类定义（包括方法）
        _compiler.DeclareClass(className, fields, methods, typeTemplate.ParentClassName);

        // 类定义本身不生成运行时指令
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
        // 完整实现：生成异常表和相应的字节码指令

        // 记录 try 块的起始位置
        int tryStart = _instructions.Count;

        // 生成 try 块的字节码
        node.TryBlock.Accept(this);

        // 记录 try 块的结束位置
        int tryEnd = _instructions.Count;

        // 处理 catch 块
        int catchStart = -1;
        int catchEnd = -1;
        string? exceptionType = null;
        string? exceptionVariable = null;
        int exceptionVariableIndex = -1;

        if (node.CatchBlocks.Count > 0)
        {
            // 目前只支持第一个 catch 块（简化实现）
            var (catchExceptionType, catchExceptionVar, catchBlock) = node.CatchBlocks[0];

            catchStart = _instructions.Count;
            exceptionType = catchExceptionType;

            // 如果有异常变量，分配局部变量
            if (catchExceptionVar != null && !string.IsNullOrEmpty(catchExceptionVar.IdName))
            {
                exceptionVariable = catchExceptionVar.IdName;
                exceptionVariableIndex = _compiler.AllocateLocal(exceptionVariable);

                // 将栈顶的异常对象存储到局部变量
                Emit(OpCode.StoreLocal, exceptionVariableIndex);
            }
            else
            {
                // 如果没有异常变量，弹出栈顶的异常对象
                Emit(OpCode.Pop);
            }

            // 生成 catch 块的字节码
            catchBlock.Accept(this);

            catchEnd = _instructions.Count;
        }

        // 处理 finally 块
        int finallyStart = -1;
        int finallyEnd = -1;

        if (node.FinallyBlock != null)
        {
            finallyStart = _instructions.Count;

            // 生成 finally 块的字节码
            node.FinallyBlock.Accept(this);

            finallyEnd = _instructions.Count;
        }

        // 创建异常表条目
        var exceptionEntry = new ExceptionTableEntry
        {
            TryStart = tryStart,
            TryEnd = tryEnd,
            CatchStart = catchStart,
            CatchEnd = catchEnd,
            FinallyStart = finallyStart,
            FinallyEnd = finallyEnd,
            ExceptionType = exceptionType,
            ExceptionVariable = exceptionVariable,
            ExceptionVariableIndex = exceptionVariableIndex
        };

        // 将异常表条目添加到当前函数的异常表
        _compiler.AddExceptionTableEntry(exceptionEntry);

        return null;
    }

    public Instruction? VisitThrowStatement(ThrowStatement node)
    {
        // 获取 expression 字段（主构造函数参数）
        var expression = GetPrimaryConstructorParameter<LangExpression>(node, "expression");

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
        // 1. 生成 yield 表达式的字节码（将值压入栈）
        node.YieldExpression.Accept(this);

        // 2. 根据当前函数是否是异步函数，生成不同的指令
        if (_compiler.IsCurrentFunctionAsync)
        {
            // 异步生成器：生成 AwaitYield 指令
            Emit(OpCode.AwaitYield);
        }
        else
        {
            // 普通生成器：生成 Yield 指令
            Emit(OpCode.Yield);
        }

        return null;
    }

    public Instruction? VisitAsyncForInStatement(AsyncForInStatement node)
    {
        // 异步 for-in 循环：async for item in asyncGenerator { ... }
        // 获取字段（主构造函数参数）
        var id = GetPrimaryConstructorParameter<LangId>(node, "id");
        var expression = GetPrimaryConstructorParameter<LangExpression>(node, "expression");
        var body = GetPrimaryConstructorParameter<OldStatement>(node, "body");

        if (id == null || expression == null || body == null)
        {
            return null;
        }

        string varName = id.IdName;

        // 创建循环标签
        var loopLabels = new LoopLabels();
        _loopLabels.Push(loopLabels);

        // 生成异步生成器表达式的代码（栈上现在有异步生成器）
        expression.Accept(this);

        // 将异步生成器保存到一个临时局部变量
        int asyncGenLocalIndex = _compiler.AllocateLocal("<async_generator>");
        Emit(OpCode.StoreLocal, asyncGenLocalIndex);

        // 循环开始标签
        int loopStart = GetCurrentPosition();
        loopLabels.ContinueTarget = loopStart;

        // 加载异步生成器到栈
        Emit(OpCode.LoadLocal, asyncGenLocalIndex);

        // 调用异步生成器的 MoveNextAsync（这会返回一个 Task）
        // 注意：这里需要虚拟机支持异步迭代器的 MoveNext 操作
        // 简化实现：使用同步的 MoveNext
        Emit(OpCode.IteratorMoveNext);

        // 如果 MoveNext 返回 false，跳出循环
        int jumpIfFalse = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1);

        // 加载异步生成器到栈
        Emit(OpCode.LoadLocal, asyncGenLocalIndex);

        // 获取当前元素
        Emit(OpCode.IteratorCurrent);

        // 将当前元素存储到循环变量
        if (_compiler.IsLocalVariable(varName))
        {
            int localIndex = _compiler.GetLocalIndex(varName);
            Emit(OpCode.StoreLocal, localIndex);
        }
        else
        {
            // 声明为局部变量
            int localIndex = _compiler.DeclareLocalVariable(varName);
            Emit(OpCode.StoreLocal, localIndex);
        }

        // 执行循环体
        body.Accept(this);

        // 跳回循环开始
        Emit(OpCode.Jump, loopStart);

        // 修补跳出循环的跳转
        int loopEnd = GetCurrentPosition();
        PatchJump(jumpIfFalse, loopEnd);

        // 修补所有break跳转
        foreach (var breakJump in loopLabels.BreakJumps)
        {
            PatchJump(breakJump, loopEnd);
        }

        // 修补所有continue跳转
        foreach (var continueJump in loopLabels.ContinueJumps)
        {
            PatchJump(continueJump, loopStart);
        }

        _loopLabels.Pop();

        return null;
    }

    public Instruction? VisitAsyncFuncInit(AsyncFuncInit node)
    {
        // 编译异步函数定义
        var funcValue = node.AsyncFuncValue;
        var funcName = funcValue.Id?.IdName ?? "<async_lambda>";
        var paramNames = funcValue.Ids?.Select(id => id.IdName).ToList() ?? new List<string>();

        // 提取默认参数值
        var defaultValues = new List<object?>();
        if (funcValue.Ids != null)
        {
            foreach (var param in funcValue.Ids)
            {
                if (param.DefaultValue != null)
                {
                    var defaultValue = EvaluateConstantExpression(param.DefaultValue);
                    defaultValues.Add(defaultValue);
                }
                else
                {
                    defaultValues.Add(null);
                }
            }
        }

        // 检测函数体是否包含 yield 语句
        bool containsYield = _compiler.ContainsYieldStatement(funcValue.BlockStatement);

        // 根据是否包含 yield 调用不同的编译方法
        if (containsYield)
        {
            // 异步生成器函数
            _compiler.CompileAsyncGeneratorFunction(funcName, paramNames, defaultValues, funcValue.BlockStatement);
        }
        else
        {
            // 普通异步函数
            _compiler.CompileAsyncFunction(funcName, paramNames, defaultValues, funcValue.BlockStatement);
        }

        return null;
    }

    public Instruction? VisitSelectStatement(SelectStatement node)
    {
        // Select 语句（Channel 多路选择）
        // 实现轮询策略：
        // 1. 循环检查所有 case
        // 2. 如果任意 case 就绪，执行对应块并退出
        // 3. 如果有 default 且所有 case 未就绪，执行 default 并退出
        // 4. 否则短暂休眠后继续循环

        int loopStart = GetCurrentPosition();
        int loopEnd = -1; // 稍后修补

        // 遍历所有 case
        foreach (var selectCase in node.Cases)
        {
            if (selectCase.IsReceive)
            {
                // 接收 case: 尝试非阻塞接收
                // 栈布局: channelId, timeoutMs -> ChannelReceiveResult

                // 加载 channelId
                selectCase.ChannelExpression.Accept(this);

                // 加载超时时间 0（非阻塞）
                Emit(OpCode.LoadConst, _compiler.ConstantPool.AddConstant(0));

                // 调用 ChannelTryReceive
                Emit(OpCode.ChannelTryReceive);

                // 结果在栈顶，需要检查 Success 属性
                // 由于虚拟机不支持直接访问对象属性，我们需要使用 CallNative
                // 暂时使用简化方案：将 ChannelReceiveResult 存储到临时变量

                // 分配临时局部变量存储结果
                int resultVarIndex = _compiler.AllocateLocal("$temp_result_" + GetCurrentPosition());
                Emit(OpCode.StoreLocal, resultVarIndex);

                // 加载结果并检查 Success（通过 GetField）
                Emit(OpCode.LoadLocal, resultVarIndex);
                Emit(OpCode.GetField, "Success");

                // 如果 Success == false，跳过此 case
                int skipCaseIndex = GetCurrentPosition();
                Emit(OpCode.JumpIfFalse, -1);

                // Success == true: 设置变量（如果有）并执行块
                if (selectCase.VariableName != null)
                {
                    // 获取 Value 字段
                    Emit(OpCode.LoadLocal, resultVarIndex);
                    Emit(OpCode.GetField, "Value");

                    // 存储到变量
                    if (_compiler.IsLocalVariable(selectCase.VariableName))
                    {
                        int varIndex = _compiler.GetLocalIndex(selectCase.VariableName);
                        Emit(OpCode.StoreLocal, varIndex);
                    }
                    else
                    {
                        int varIndex = _compiler.DeclareLocalVariable(selectCase.VariableName);
                        Emit(OpCode.StoreLocal, varIndex);
                    }
                }

                // 执行 case 块
                selectCase.BlockStatement.Accept(this);

                // 跳转到循环结束
                int jumpToEndIndex = GetCurrentPosition();
                Emit(OpCode.Jump, -1);
                if (loopEnd == -1)
                {
                    loopEnd = jumpToEndIndex;
                }

                // 修补跳过此 case 的跳转
                PatchJump(skipCaseIndex, GetCurrentPosition());
            }
            else
            {
                // 发送 case: 尝试非阻塞发送
                // 栈布局: channelId, value, timeoutMs -> bool

                // 加载 channelId
                selectCase.ChannelExpression.Accept(this);

                // 加载要发送的值
                selectCase.SendValueExpression!.Accept(this);

                // 加载超时时间 0（非阻塞）
                Emit(OpCode.LoadConst, _compiler.ConstantPool.AddConstant(0));

                // 调用 ChannelTrySend
                Emit(OpCode.ChannelTrySend);

                // 如果返回 false，跳过此 case
                int skipCaseIndex = GetCurrentPosition();
                Emit(OpCode.JumpIfFalse, -1);

                // 返回 true: 执行 case 块
                selectCase.BlockStatement.Accept(this);

                // 跳转到循环结束
                int jumpToEndIndex = GetCurrentPosition();
                Emit(OpCode.Jump, -1);
                if (loopEnd == -1)
                {
                    loopEnd = jumpToEndIndex;
                }

                // 修补跳过此 case 的跳转
                PatchJump(skipCaseIndex, GetCurrentPosition());
            }
        }

        // 所有 case 都未就绪
        if (node.DefaultCase != null)
        {
            // 有 default 分支：执行 default 并退出
            node.DefaultCase.Accept(this);

            // 跳转到循环结束
            int jumpToEndIndex = GetCurrentPosition();
            Emit(OpCode.Jump, -1);
            if (loopEnd == -1)
            {
                loopEnd = jumpToEndIndex;
            }
        }
        else
        {
            // 无 default 分支：休眠 1ms 后继续轮询
            // Thread.Sleep(1)
            Emit(OpCode.LoadConst, _compiler.ConstantPool.AddConstant(1));

            // 调用原生函数 Sleep
            int sleepFuncIndex = _compiler.ConstantPool.AddConstant("Sleep");
            Emit(OpCode.CallNative, new object[] { 1, sleepFuncIndex });

            // 继续循环
            Emit(OpCode.Jump, loopStart);
        }

        // 修补所有跳转到循环结束的指令
        int actualLoopEnd = GetCurrentPosition();
        if (loopEnd != -1)
        {
            // 遍历所有指令，修补跳转到 loopEnd 的指令
            for (int i = loopStart; i < actualLoopEnd; i++)
            {
                var instruction = _instructions[i];
                if (instruction.OpCode == OpCode.Jump && instruction.Operand is int target && target == -1)
                {
                    PatchJump(i, actualLoopEnd);
                }
            }
        }

        return null;
    }

    public Instruction? VisitDeferStatement(DeferStatement node)
    {
        // Defer 语句（延迟执行）
        // 实现策略：
        // 1. 跳过 defer 块的代码（不立即执行）
        // 2. 将 defer 块的起始位置记录到 CallFrame 的 DeferStack
        // 3. 在函数返回时，虚拟机会按 LIFO 顺序执行所有 defer 块

        // 跳过 defer 块（使用 Jump 指令）
        int jumpOverDeferIndex = GetCurrentPosition();
        Emit(OpCode.Jump, -1); // 跳转目标稍后修补

        // 记录 defer 块的起始位置
        int deferStartPos = GetCurrentPosition();

        // 生成 defer 块的代码
        node.Statement.Accept(this);

        // defer 块结束后返回（不是函数返回，而是从 defer 块返回）
        Emit(OpCode.ReturnVoid);

        // 记录 defer 块的结束位置
        int deferEndPos = GetCurrentPosition();

        // 修补跳转指令，跳过 defer 块
        PatchJump(jumpOverDeferIndex, deferEndPos);

        // 发出 Defer 指令，将 defer 块的起始位置压入 DeferStack
        Emit(OpCode.Defer, deferStartPos);

        return null;
    }

    public Instruction? VisitEnumInit(EnumInit node)
    {
        // 枚举定义在字节码模式下暂不支持
        // TODO: 实现枚举的字节码生成
        return null;
    }

    public Instruction? VisitExternStatement(ExternStatement node)
    {
        // 使用反射获取 ExternStatement 的私有字段
        var nodeType = node.GetType();
        var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        var dllNameField = nodeType.GetField("DllName", bindingFlags);
        var dllName = dllNameField?.GetValue(node) as string
            ?? throw new InvalidOperationException("无法获取 DLL 名称");

        var functionsField = nodeType.GetField("Functions", bindingFlags);
        var functions = functionsField?.GetValue(node) as List<ExternFunctionDeclaration>
            ?? throw new InvalidOperationException("无法获取函数列表");

        var externTypeField = nodeType.GetField("ExternType", bindingFlags);
        var externType = externTypeField != null
            ? (ExternType)externTypeField.GetValue(node)!
            : ExternType.NativeDll;

        var defaultCallingConventionField = nodeType.GetField("DefaultCallingConvention", bindingFlags);
        var defaultCallingConvention = defaultCallingConventionField != null
            ? (CallingConventionType)defaultCallingConventionField.GetValue(node)!
            : CallingConventionType.Cdecl;

        // 为每个 extern 函数生成 LoadExtern 指令
        foreach (var funcDecl in functions)
        {
            var targetName = funcDecl.Alias ?? funcDecl.FunctionName;

            // 将 DLL 名称、函数名称和 extern 类型添加到常量池
            var dllNameIndex = _compiler.ConstantPool.AddConstant(dllName);
            var funcNameIndex = _compiler.ConstantPool.AddConstant(funcDecl.FunctionName);
            var externTypeIndex = _compiler.ConstantPool.AddConstant((int)externType);

            // 获取调用约定
            var callingConv = funcDecl.CallingConvention != CallingConventionType.Cdecl
                ? funcDecl.CallingConvention
                : defaultCallingConvention;
            var callingConvIndex = _compiler.ConstantPool.AddConstant((int)callingConv);

            // 将函数签名信息序列化为字符串（如果存在）
            string? signatureStr = null;
            if (funcDecl.FunctionSignature != null)
            {
                var sig = funcDecl.FunctionSignature.FuncLangValue;
                var paramTypes = sig.Ids?.Select(p => p.AssumptionType ?? "object").ToList() ?? new List<string>();
                var returnType = sig.Id?.AssumptionType ?? "void";
                signatureStr = $"{string.Join(",", paramTypes)}:{returnType}";
            }
            var signatureIndex = signatureStr != null
                ? _compiler.ConstantPool.AddConstant(signatureStr)
                : _compiler.ConstantPool.AddConstant("");

            // 生成 LoadExtern 指令
            // 操作数格式: [dllNameIndex, funcNameIndex, externTypeIndex, callingConvIndex, signatureIndex]
            var operands = new[] { dllNameIndex, funcNameIndex, externTypeIndex, callingConvIndex, signatureIndex };
            Emit(OpCode.LoadExtern, operands);

            // 将加载的 extern 函数存储到全局变量
            // 注意: StoreGlobal 的操作数是字符串，不是索引
            Emit(OpCode.StoreGlobal, targetName);
        }

        return null;
    }

    public Instruction? VisitFileHeaderDirective(FileHeaderDirective node)
    {
        // 文件头指令在字节码模式下不需要生成代码
        return null;
    }

    public Instruction? VisitUsingStatement(UsingStatement node)
    {
        // Using 语句在字节码模式下暂不支持
        // TODO: 实现 using 的字节码生成
        return null;
    }
}
