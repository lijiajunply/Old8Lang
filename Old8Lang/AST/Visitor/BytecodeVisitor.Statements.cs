using Old8Lang.AST;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
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

            string typeToCheck = node.Id.AssumptionType;

            // 如果没有显式类型注解，尝试查找变量的已知类型
            if (string.IsNullOrEmpty(typeToCheck) && _compiler.IsLocalVariable(varName))
            {
                typeToCheck = _compiler.GetLocalType(varName);
            }

            // 检查是否有类型注解
            if (!string.IsNullOrEmpty(typeToCheck))
            {
                // 复制栈顶值用于检查
                Emit(OpCode.Dup);
                
                // 执行类型检查
                Emit(OpCode.IsType, typeToCheck);
                
                // 如果检查通过，跳转到存储
                int jumpIfTrue = GetCurrentPosition();
                Emit(OpCode.JumpIfTrue, -1);
                
                // 检查失败，抛出异常
                // 加载错误消息
                var errorMsg = $"变量 '{varName}' 类型不匹配: 期望 {typeToCheck}";
                var msgIndex = _compiler.ConstantPool.AddConstant(errorMsg);
                Emit(OpCode.LoadConst, msgIndex);
                Emit(OpCode.Throw);
                
                // 修补跳转
                PatchJump(jumpIfTrue, GetCurrentPosition());
            }

            // 检查是否是局部变量
            if (_compiler.IsLocalVariable(varName))
            {
                int localIndex = _compiler.GetLocalIndex(varName);

                // 如果有显式类型注解，更新变量类型
                if (!string.IsNullOrEmpty(node.Id.AssumptionType))
                {
                    _compiler.DeclareLocalVariable(varName, node.Id.AssumptionType);
                }

                Emit(OpCode.StoreLocal, localIndex);
            }
            else if (_compiler.IsGlobalVariable(varName))
            {
                // 全局变量更新
                Emit(OpCode.StoreGlobal, varName);
            }
            else
            {
                // 新变量：根据作用域决定是全局变量还是局部变量
                // 参考解释器模式：主函数顶层的变量应该是全局变量
                if (_compiler.IsInMainFunctionTopLevel())
                {
                    // 在主函数顶层：声明为全局变量
                    _compiler.DeclareGlobalVariable(varName);
                    Emit(OpCode.StoreGlobal, varName);
                }
                else
                {
                    // 在其他作用域：声明为局部变量
                    int localIndex = _compiler.DeclareLocalVariable(varName, node.Id.AssumptionType);
                    Emit(OpCode.StoreLocal, localIndex);
                }
            }
        }
        else
        {
            // 索引/成员访问赋值: array[i] <- value 或 obj.field <- value
            var leftExpr = node.LeftExpression;

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
            else if (leftExpr is TupleLangValue tupleLHS)
            {
                // 元组解构赋值: (a, b) <- (1, 2)
                // 1. 生成 RHS 代码 (栈顶: Tuple)
                node.Value.Accept(this);
                
                // 2. 将 RHS 存储到临时变量，避免重复计算
                int tupleLocalIndex = _compiler.AllocateLocal("<temp_tuple_destruct>");
                Emit(OpCode.StoreLocal, tupleLocalIndex);
                
                // 3. 展平并赋值
                // 注意：新版 TupleLangValue 已经是扁平存储的，直接使用 Elements
                
                var elements = tupleLHS.Elements;
                
                for (int i = 0; i < elements.Count; i++)
                {
                    var element = elements[i];
                    if (element is LangId id)
                    {
                        // 提取第 i 个元素
                        Emit(OpCode.LoadLocal, tupleLocalIndex);
                        Emit(OpCode.LoadConst, i);
                        Emit(OpCode.GetIndex);
                        
                        // 赋值给变量
                        string elementName = id.IdName;
                        if (_compiler.IsLocalVariable(elementName))
                        {
                            Emit(OpCode.StoreLocal, _compiler.GetLocalIndex(elementName));
                        }
                        else if (_compiler.IsGlobalVariable(elementName))
                        {
                            Emit(OpCode.StoreGlobal, elementName);
                        }
                        else
                        {
                            int newLocal = _compiler.DeclareLocalVariable(elementName);
                            Emit(OpCode.StoreLocal, newLocal);
                        }
                    }
                    else
                    {
                        // 不支持嵌套解构赋值 (a, (b, c)) <- ... 目前仅支持单层
                         throw new NotSupportedException($"字节码模式下元组解构仅支持变量名，不支持: {element.GetType().Name}");
                    }
                }
                
                // 清理临时变量
                _compiler.FreeLocal(tupleLocalIndex);
            }
            else if (leftExpr != null)
            {
                // 字节码模式目前只支持以下赋值类型：
                // 1. 简单变量赋值 (x <- value)
                // 2. 索引赋值 (arr[i] <- value)
                // 3. 成员访问赋值 (obj.field <- value)
                // 4. 元组解构赋值 ((a, b) <- value)
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
        var elifBlocks = GetPrimaryConstructorParameter<List<IfChild?>>(node, "elifBlock") ?? [];
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
        var funcValue = node.FuncValue;
        var funcName = funcValue.Id?.IdName ?? "<lambda>";

        // 检查是否是泛型函数
        if (funcValue.GenericParameters != null && funcValue.GenericParameters.Count > 0)
        {
            // 泛型函数：注册到泛型函数缓存，不立即编译
            _compiler.RegisterGenericFunction(funcName, funcValue);
            return null;
        }

        // 检查函数是否已经被编译过（避免重复编译）
        if (_compiler.GetFunctionIndex(funcName) >= 0)
        {
            // 函数已经在预处理阶段被编译过，跳过
            return null;
        }

        // 非泛型函数：正常编译
        var paramNames = funcValue.Ids?.Select(id => id.IdName).ToList() ?? [];

        // 提取默认参数值和params参数索引
        var defaultValues = new List<object?>();
        int paramsIndex = -1;
        if (funcValue.Ids != null)
        {
            for (int i = 0; i < funcValue.Ids.Count; i++)
            {
                var param = funcValue.Ids[i];

                // 检查是否是params参数
                if (param.IsParams)
                {
                    paramsIndex = i;
                }

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

        // 编译函数
        var functionMetadata = _compiler.CompileFunction(funcName, paramNames, defaultValues, funcValue.BlockStatement, paramsIndex);

        // 检查是否有装饰器
        if (funcValue.Decorators != null && funcValue.Decorators.Count > 0)
        {
            // 应用装饰器
            ApplyDecorators(funcName, funcValue.Decorators);
        }
        else
        {
            // 无装饰器：直接将函数加载到栈并存储
            int funcIndex = _compiler.GetFunctionIndex(funcName);
            Emit(OpCode.MakeFunction, funcIndex);
            Emit(OpCode.StoreGlobal, funcName);
        }

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
        // 使用唯一的名称避免嵌套循环中的冲突
        int iteratorLocalIndex = _compiler.AllocateLocal($"<iterator_{GetCurrentPosition()}>");
        Emit(OpCode.StoreLocal, iteratorLocalIndex);

        // 循环开始标签
        int loopStart = GetCurrentPosition();
        loopLabels.ContinueTarget = loopStart;

        // 加载迭代器到栈
        Emit(OpCode.LoadLocal, iteratorLocalIndex);

        // 调用 MoveNext（栈：迭代器 → 迭代器, hasNext）
        // 注意：IteratorMoveNext 使用 Peek，所以迭代器仍在栈上
        Emit(OpCode.IteratorMoveNext);

        // 如果 MoveNext 返回 false，跳出循环
        // JumpIfFalse 会弹出 hasNext，栈上还剩迭代器
        int jumpIfFalse = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1);

        // 此时栈上还有迭代器对象（因为 IteratorMoveNext 使用 Peek）
        // 不需要再次加载迭代器

        // 获取当前元素（栈：迭代器 → 迭代器, current）
        // 注意：IteratorCurrent 也使用 Peek，所以迭代器仍在栈上
        Emit(OpCode.IteratorCurrent);

        // 将当前元素存储到循环变量（弹出 current）
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

        // 此时栈上还有迭代器对象，需要弹出
        Emit(OpCode.Pop);

        // 执行循环体
        body.Accept(this);

        // 跳回循环开始
        Emit(OpCode.Jump, loopStart);

        // 修补跳出循环的跳转
        int loopEnd = GetCurrentPosition();
        PatchJump(jumpIfFalse, loopEnd);

        // 跳出循环时（通过 JumpIfFalse），栈上还有迭代器对象，需要弹出
        Emit(OpCode.Pop);

        // 修补所有break跳转
        // break 跳转到这里时，栈上没有迭代器对象（已经在循环体中被弹出了）
        int breakTarget = GetCurrentPosition();
        foreach (var breakJump in loopLabels.BreakJumps)
        {
            PatchJump(breakJump, breakTarget);
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
            caseStmt.Expression.Accept(this);

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
        var typeTemplate = node.AnyValue;
        string className = typeTemplate.ClassName;

        // 检查类是否已经被编译过（在PreprocessClassDefinitions阶段）
        // 如果已经编译过，直接返回，避免重复编译
        if (_compiler.GetClassMetadata(className) != null)
        {
            return null;
        }

        // 检查是否是泛型类
        if (typeTemplate.GenericParameters != null && typeTemplate.GenericParameters.Count > 0)
        {
            // 泛型类：注册到泛型类缓存，不立即编译
            _compiler.RegisterGenericClass(className, typeTemplate);
            return null;
        }

        // 处理接口定义
        if (typeTemplate.IsInterface)
        {
            CompileInterfaceDefinition(typeTemplate);
            return null;
        }

        // 处理 Mixin 定义
        if (typeTemplate.IsMixin)
        {
            CompileMixinDefinition(typeTemplate);
            return null;
        }

        // 非泛型类：正常编译
        var fields = new List<string>();
        var methods = new List<(string methodName, FuncLangValue funcValue, bool isStatic, AccessModifier accessModifier)>();

        // 遍历实例成员，提取字段和方法
        foreach (var (memberId, memberExpr) in typeTemplate.Variates)
        {
            if (memberExpr is FuncLangValue funcValue)
            {
                // 这是一个实例方法
                var accessModifier = GetAccessModifier(memberId.Modifiers);
                methods.Add((memberId.IdName, funcValue, false, accessModifier));
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
                var accessModifier = GetAccessModifier(memberId.Modifiers);
                methods.Add((memberId.IdName, funcValue, true, accessModifier));
            }
        }

        // 在编译器中注册类定义（包括方法、接口和Mixin）
        _compiler.DeclareClass(className, fields, methods, typeTemplate.ParentClassName,
            typeTemplate.ImplementsNames, typeTemplate.MixinNames);

        // 类定义本身不生成运行时指令
        return null;
    }

    public Instruction? VisitImportStatement(ImportStatement node)
    {
        // 导入语句 - 生成模块加载和符号导入指令

        // 1. 生成LoadModule指令加载模块
        string moduleName = node.GetImportString();
        Emit(OpCode.LoadModule, moduleName);

        // 2. 根据导入类型生成相应的导入指令
        if (node.GetFromClause())
        {
            // import { item1, item2 } from "module"
            var importSpecifiers = node.GetImportSpecifiers();
            if (importSpecifiers != null && importSpecifiers.Count > 0)
            {
                foreach (var specifier in importSpecifiers)
                {
                    if (specifier.Alias != specifier.Name)
                    {
                        // import { item as alias } from "module"
                        Emit(OpCode.ImportSymbolAs, new object[] { moduleName, specifier.Name, specifier.Alias });
                    }
                    else
                    {
                        // import { item } from "module"
                        Emit(OpCode.ImportSymbol, new object[] { moduleName, specifier.Name });
                    }
                }
            }
            else if (importSpecifiers != null && importSpecifiers.Count == 0)
            {
                // import * from "module"
                Emit(OpCode.ImportAll, moduleName);
            }
        }
        else
        {
            // import "module" 或 import "module" as alias
            if (node.GetModuleAlias() != null)
            {
                // 模块别名：将模块对象存储到全局变量
                // 这里我们不生成指令，因为LoadModule已经加载了模块
                // 模块别名的处理在虚拟机中完成
            }
            else
            {
                // 简单导入：导入所有导出符号
                Emit(OpCode.ImportAll, moduleName);
            }
        }

        return null;
    }

    public Instruction? VisitNativeStatement(NativeStatement node)
    {
        // ImportNative 指令：导入原生资源
        // 操作数格式: [dllNameIndex, classNameIndex, mode, p1, p2]
        // mode: 0=Single, 1=All, 2=Class

        int dllNameIndex = _compiler.ConstantPool.AddConstant(node.DllName);
        int classNameIndex = _compiler.ConstantPool.AddConstant(node.ClassName);

        if (node.ImportAll)
        {
            // Mode 1: All Methods
            Emit(OpCode.ImportNative, new[] { dllNameIndex, classNameIndex, 1, 0, 0 });
        }
        else if (node.MethodList is { Count: > 0 })
        {
            // Method List -> Multiple Single Method Imports
            foreach (var methodName in node.MethodList)
            {
                int methodNameIndex = _compiler.ConstantPool.AddConstant(methodName);
                int aliasIndex = _compiler.ConstantPool.AddConstant(""); // No alias for list import
                Emit(OpCode.ImportNative, new[] { dllNameIndex, classNameIndex, 0, methodNameIndex, aliasIndex });
            }
        }
        else if (!string.IsNullOrEmpty(node.MethodName))
        {
            // Mode 0: Single Method
            int methodNameIndex = _compiler.ConstantPool.AddConstant(node.MethodName);
            int aliasIndex = _compiler.ConstantPool.AddConstant(node.NativeName ?? "");
            Emit(OpCode.ImportNative, new[] { dllNameIndex, classNameIndex, 0, methodNameIndex, aliasIndex });
        }
        else
        {
            // Mode 2: Class Import
            string alias = node.Name ?? node.ClassAlias ?? "";
            int aliasIndex = _compiler.ConstantPool.AddConstant(alias);
            Emit(OpCode.ImportNative, new[] { dllNameIndex, classNameIndex, 2, aliasIndex, 0 });
        }

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

        // 生成跳转到 finally 或结束的指令 (跳过 catch 块)
        int jumpToFinallyIndex = GetCurrentPosition();
        Emit(OpCode.Jump, -1);

        // 处理 catch 块 (实现手动分发逻辑)
        int catchStart = -1;
        int catchEnd = -1;
        string? exceptionVariable = "<exception_dispatch>";
        int exceptionVariableIndex = _compiler.AllocateLocal(exceptionVariable);

        if (node.CatchBlocks.Count > 0)
        {
            catchStart = _instructions.Count;

            // 1. 保存异常对象到临时变量
            Emit(OpCode.StoreLocal, exceptionVariableIndex);

            // 2. 遍历所有 catch 块，生成检查链
            List<int> jumpsToNextBlock = [];
            List<int> jumpsToFinally = [];

            for (int i = 0; i < node.CatchBlocks.Count; i++)
            {
                var (catchExceptionType, catchExceptionVar, filter, catchBlock) = node.CatchBlocks[i];
                
                // 修补上一个块失败后的跳转（跳到当前块的开始）
                foreach (var jump in jumpsToNextBlock)
                {
                    PatchJump(jump, GetCurrentPosition());
                }
                jumpsToNextBlock.Clear();

                // --- 类型检查 ---
                if (!string.IsNullOrEmpty(catchExceptionType) && catchExceptionType != "Exception")
                {
                    // 检查类型
                    Emit(OpCode.LoadLocal, exceptionVariableIndex);
                    Emit(OpCode.IsType, catchExceptionType);
                    
                    // 如果类型不匹配，跳到下一个 catch 块
                    jumpsToNextBlock.Add(GetCurrentPosition());
                    Emit(OpCode.JumpIfFalse, -1);
                }
                
                // --- 过滤器检查 ---
                if (filter != null)
                {
                    // 绑定变量 (供过滤器使用)
                    if (catchExceptionVar != null && !string.IsNullOrEmpty(catchExceptionVar.IdName))
                    {
                        int varIndex = _compiler.AllocateLocal(catchExceptionVar.IdName);
                        Emit(OpCode.LoadLocal, exceptionVariableIndex);
                        Emit(OpCode.StoreLocal, varIndex);
                    }
                    
                    // 执行过滤器
                    filter.Accept(this);
                    
                    // 如果过滤器为 false，跳到下一个 catch 块
                    jumpsToNextBlock.Add(GetCurrentPosition());
                    Emit(OpCode.JumpIfFalse, -1);
                }

                // --- 执行 Catch 块 ---
                // 绑定变量 (供 catch 块使用)
                // 简单起见，重新绑定 (覆盖)。
                
                if (catchExceptionVar != null && !string.IsNullOrEmpty(catchExceptionVar.IdName))
                {
                    // 实际上，我们应该在 catch 块开始时声明变量
                    int varIndex = _compiler.DeclareLocalVariable(catchExceptionVar.IdName);
                    Emit(OpCode.LoadLocal, exceptionVariableIndex);
                    Emit(OpCode.StoreLocal, varIndex);
                }

                catchBlock.Accept(this);

                // 执行完 catch 块后，跳到 finally
                jumpsToFinally.Add(GetCurrentPosition());
                Emit(OpCode.Jump, -1);
            }

            // --- 所有 Catch 块都不匹配 ---
            // 重新抛出异常
            // 如果最后一个 catch 是 catch-all，则不会到达这里
            foreach (var jump in jumpsToNextBlock)
            {
                PatchJump(jump, GetCurrentPosition());
            }
            
            Emit(OpCode.LoadLocal, exceptionVariableIndex);
            Emit(OpCode.Throw);

            catchEnd = _instructions.Count;
            
            // 修补所有跳到 finally 的指令
            // 我们将在 finally 块生成后修补
            
            // 为了让 jumpToFinallyList 在 finally 块后可用，我们需要存储它？
            // 不，我们在 finally 块生成后可以手动添加它们到 elseLabel (如果 finallyBlock 为空)
            // 或者我们可以立即修补到 finallyStart (如果 finallyBlock 存在)
            
            // 但是 finallyStart 还不知道。
            // 我们可以将 jumpToFinallyIndex 也加入到 jumpsToFinally 中
            jumpsToFinally.Add(jumpToFinallyIndex);
            
            // 稍后修补 jumpsToFinally
            
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
            
            int endPos = GetCurrentPosition();
            int target = finallyStart != -1 ? finallyStart : endPos;
            
            foreach (var jump in jumpsToFinally)
            {
                PatchJump(jump, target);
            }

            // 创建异常表条目 (单个入口，匹配所有异常)
            var exceptionEntry = new ExceptionTableEntry
            {
                TryStart = tryStart,
                TryEnd = tryEnd,
                CatchStart = catchStart,
                CatchEnd = catchEnd,
                FinallyStart = finallyStart,
                FinallyEnd = finallyEnd,
                ExceptionType = null, // 匹配所有异常
                ExceptionVariable = null, // 手动处理变量
                ExceptionVariableIndex = -1
            };

            // 将异常表条目添加到当前函数的异常表
            _compiler.AddExceptionTableEntry(exceptionEntry);
        }
        else
        {
            // 没有 catch 块，只有 finally 块
            // 修补跳过 catch 的指令 (直接跳到 finally)
            // 此时 catchStart = -1
            
            int finallyStart = -1;
            int finallyEnd = -1;

            if (node.FinallyBlock != null)
            {
                finallyStart = _instructions.Count;
                node.FinallyBlock.Accept(this);
                finallyEnd = _instructions.Count;
            }
            
            int endPos = GetCurrentPosition();
            int target = finallyStart != -1 ? finallyStart : endPos;
            PatchJump(jumpToFinallyIndex, target);
            
            if (node.FinallyBlock != null)
            {
                var exceptionEntry = new ExceptionTableEntry
                {
                    TryStart = tryStart,
                    TryEnd = tryEnd,
                    CatchStart = -1,
                    CatchEnd = -1,
                    FinallyStart = finallyStart,
                    FinallyEnd = finallyEnd,
                    ExceptionType = null,
                    ExceptionVariable = null,
                    ExceptionVariableIndex = -1
                };
                _compiler.AddExceptionTableEntry(exceptionEntry);
            }
        }

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

        // 检查函数是否已经被编译过（避免重复编译）
        if (_compiler.GetFunctionIndex(funcName) >= 0)
        {
            // 函数已经在预处理阶段被编译过，跳过
            return null;
        }

        var paramNames = funcValue.Ids?.Select(id => id.IdName).ToList() ?? [];

        // 提取默认参数值和params参数索引
        var defaultValues = new List<object?>();
        int paramsIndex = -1;
        if (funcValue.Ids != null)
        {
            for (int i = 0; i < funcValue.Ids.Count; i++)
            {
                var param = funcValue.Ids[i];

                // 检查是否是params参数
                if (param.IsParams)
                {
                    paramsIndex = i;
                }

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
            _compiler.CompileAsyncGeneratorFunction(funcName, paramNames, defaultValues, funcValue.BlockStatement, paramsIndex);
        }
        else
        {
            // 普通异步函数
            _compiler.CompileAsyncFunction(funcName, paramNames, defaultValues, funcValue.BlockStatement, paramsIndex);
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
        // 计算枚举成员的实际值
        var enumValues = new Dictionary<string, int>();
        int currentValue = 0;

        foreach (var (memberName, memberValueExpr) in node.Members)
        {
            if (memberValueExpr is not null)
            {
                // 有显式赋值，必须是整数常量
                if (memberValueExpr is IntLangValue intValue)
                {
                    currentValue = intValue.Value;
                }
                else
                {
                    throw new SyntaxError(node.Position, $"枚举成员 '{memberName}' 的值必须是整数常量");
                }
            }

            // 检查成员名是否重复
            if (!enumValues.TryAdd(memberName, currentValue))
            {
                throw new DuplicateNameError(node, memberName, "枚举成员");
            }

            currentValue++; // 下一个未赋值的成员值自动递增
        }

        // 将枚举名称添加到常量池
        var enumNameIndex = _compiler.ConstantPool.AddConstant(node.EnumName);

        // 将成员信息添加到常量池（成员名和值的数组）
        var memberData = new object[enumValues.Count * 2];
        int index = 0;
        foreach (var kvp in enumValues)
        {
            memberData[index++] = kvp.Key;   // 成员名
            memberData[index++] = kvp.Value; // 成员值
        }
        var memberDataIndex = _compiler.ConstantPool.AddConstant(memberData);

        // 发出 DefineEnum 指令
        Emit(OpCode.DefineEnum, new object[] { enumNameIndex, enumValues.Count, memberDataIndex });

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
                var sig = funcDecl.FunctionSignature.FuncValue;
                var paramTypes = sig.Ids?.Select(p => p.AssumptionType ?? "object").ToList() ?? [];
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
        // Using 语句：自动资源管理
        // 实现策略：使用 try-finally 结构，在 finally 块中调用 DisposeResource

        // 1. 执行资源表达式，获取资源ID
        node.ResourceExpression.Accept(this);

        // 2. 将资源存储到局部变量
        int resourceLocalIndex = -1;
        if (node.VariableName != null)
        {
            // 如果有变量名，使用用户指定的变量名
            resourceLocalIndex = _compiler.AllocateLocal(node.VariableName);
        }
        else
        {
            // 如果没有变量名，使用临时变量
            resourceLocalIndex = _compiler.AllocateLocal("<using_resource>");
        }

        // 存储资源到局部变量
        Emit(OpCode.StoreLocal, resourceLocalIndex);

        // 3. 记录 try 块的起始位置
        int tryStart = _instructions.Count;

        // 4. 执行 using 块
        node.BlockStatement.Accept(this);

        // 5. 记录 try 块的结束位置
        int tryEnd = _instructions.Count;

        // 6. 生成 finally 块
        int finallyStart = _instructions.Count;

        // 加载资源ID
        Emit(OpCode.LoadLocal, resourceLocalIndex);

        // 调用 DisposeResource 指令释放资源
        Emit(OpCode.DisposeResource);

        // 7. 记录 finally 块的结束位置
        int finallyEnd = _instructions.Count;

        // 8. 创建异常表条目
        var exceptionEntry = new ExceptionTableEntry
        {
            TryStart = tryStart,
            TryEnd = tryEnd,
            CatchStart = -1,  // 没有 catch 块
            CatchEnd = -1,
            FinallyStart = finallyStart,
            FinallyEnd = finallyEnd,
            ExceptionType = null,
            ExceptionVariable = null,
            ExceptionVariableIndex = -1
        };

        // 9. 将异常表条目添加到当前函数的异常表
        _compiler.AddExceptionTableEntry(exceptionEntry);

        return null;
    }

    /// <summary>
    /// 编译接口定义
    /// </summary>
    private void CompileInterfaceDefinition(TypeTemplate typeTemplate)
    {
        string interfaceName = typeTemplate.ClassName;
        var methods = new List<string>();

        // 提取接口方法签名
        foreach (var (memberId, memberExpr) in typeTemplate.Variates)
        {
            if (memberExpr is FuncLangValue funcValue)
            {
                methods.Add(memberId.IdName);
            }
        }

        // 在编译器中注册接口定义
        _compiler.DeclareInterface(interfaceName, methods, typeTemplate.ImplementsNames);
    }

    /// <summary>
    /// 编译 Mixin 定义
    /// </summary>
    private void CompileMixinDefinition(TypeTemplate typeTemplate)
    {
        string mixinName = typeTemplate.ClassName;
        var methods = new List<(string methodName, FuncLangValue funcValue)>();

        // 提取 Mixin 方法
        foreach (var (memberId, memberExpr) in typeTemplate.Variates)
        {
            if (memberExpr is FuncLangValue funcValue)
            {
                methods.Add((memberId.IdName, funcValue));
            }
        }

        // 在编译器中注册 Mixin 定义
        _compiler.DeclareMixin(mixinName, methods);
    }

    /// <summary>
    /// 应用装饰器到函数
    /// </summary>
    private void ApplyDecorators(string funcName, List<FunctionDecorator> decorators)
    {
        // 1. 加载原始函数
        int funcIndex = _compiler.GetFunctionIndex(funcName);
        Emit(OpCode.MakeFunction, funcIndex);

        // 2. 从下到上应用装饰器
        for (int i = decorators.Count - 1; i >= 0; i--)
        {
            var decorator = decorators[i];
            ApplySingleDecorator(decorator);
        }

        // 3. 存储最终函数
        Emit(OpCode.StoreGlobal, funcName);
    }

    /// <summary>
    /// 应用单个装饰器
    /// </summary>
    private void ApplySingleDecorator(FunctionDecorator decorator)
    {
        // 栈顶是目标函数

        if (decorator.Arguments != null && decorator.Arguments.Count > 0)
        {
            // 带参数的装饰器：decorator(args...)(targetFunc)

            // 1. 加载装饰器函数
            Emit(OpCode.LoadGlobal, decorator.Name);

            // 2. 计算装饰器参数
            foreach (var arg in decorator.Arguments)
            {
                arg.Accept(this);
            }

            // 3. 调用装饰器函数获取包装器
            Emit(OpCode.CallDynamic, decorator.Arguments.Count);

            // 4. 交换栈顶两个元素（包装器和目标函数）
            // 栈：[targetFunc, wrapper] -> [wrapper, targetFunc]
            Emit(OpCode.Swap);

            // 5. 调用包装器
            Emit(OpCode.CallDynamic, 1);
        }
        else
        {
            // 无参数的装饰器：decorator(targetFunc)

            // 1. 加载装饰器函数
            Emit(OpCode.LoadGlobal, decorator.Name);

            // 2. 交换栈顶两个元素
            // 栈：[targetFunc, decorator] -> [decorator, targetFunc]
            Emit(OpCode.Swap);

            // 3. 调用装饰器
            Emit(OpCode.CallDynamic, 1);
        }

        // 栈顶现在是装饰后的函数
    }

    /// <summary>
    /// 将 AccessModifierType 转换为 AccessModifier
    /// </summary>
    private AccessModifier GetAccessModifier(HashSet<AccessModifierType> modifiers)
    {
        if (modifiers.Contains(AccessModifierType.Private))
        {
            return AccessModifier.Private;
        }
        if (modifiers.Contains(AccessModifierType.Protected))
        {
            return AccessModifier.Protected;
        }
        return AccessModifier.Public;
    }
}
