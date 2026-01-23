using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Linq;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - Expression节点的实现
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitLangId(LangId node)
    {
        string varName = node.IdName;

        // 检查是否是局部变量
        if (_compiler.IsLocalVariable(varName))
        {
            int localIndex = _compiler.GetLocalIndex(varName);
            Emit(OpCode.LoadLocal, localIndex);
        }
        // 检查是否是类名（优先于实例字段检查）
        else if (_compiler.IsClassName(varName))
        {
            // 这是一个类名，应该作为全局变量加载（类元数据）
            Emit(OpCode.LoadGlobal, varName);
        }
        // 检查是否是当前类的字段
        else if (_compiler.IsClassField(varName))
        {
            // 这是一个字段访问：this.field
            // 加载 this（第一个局部变量）
            Emit(OpCode.LoadLocal, 0);

            // 加载字段
            Emit(OpCode.GetField, varName);
        }
        else
        {
            // 全局变量
            Emit(OpCode.LoadGlobal, varName);
        }

        return null;
    }

    public Instruction? VisitOperation(Operation node)
    {
        // 特殊处理 Dot 运算符（成员访问和方法调用）
        if (node.Opera == LangTokenType.Dot)
        {
            // 检查是否是 super 表达式
            bool isSuperAccess = node.Left is SuperExpression;

            // 生成左操作数代码（对象或super）
            if (node.Left != null)
                node.Left.Accept(this);

            // 检查右操作数是否是方法调用（Instance）
            if (node.Right is Instance instance)
            {
                // 这是方法调用：object.method(args) 或 super.method(args)
                // 左操作数（对象或super）已经在栈上

                // 生成所有参数的代码
                foreach (var arg in instance.Ids)
                {
                    arg.Accept(this);
                }

                string methodName = instance.Id.IdName;
                int argCount = instance.Ids.Count + 1; // +1 因为对象本身是第一个参数

                if (isSuperAccess)
                {
                    // super.method(args) - 调用父类方法
                    Emit(OpCode.CallSuperMethod, new object[] { argCount, methodName });
                }
                else
                {
                    // object.method(args) - 调用对象方法
                    // 使用 CallMethod 指令，它会在对象的类中查找方法
                    Emit(OpCode.CallMethod, new object[] { argCount, methodName });
                }
            }
            else if (node.Right is LangId memberId)
            {
                // 这是字段访问：object.field 或 super.field
                // 左操作数（对象或super）已经在栈上
                string fieldName = memberId.IdName;

                if (isSuperAccess)
                {
                    // super.field - 访问父类字段
                    Emit(OpCode.GetSuperField, fieldName);
                }
                else
                {
                    // object.field - 访问普通字段
                    Emit(OpCode.GetField, fieldName);
                }
            }
            else if (node.Right is ClassMemberId classMemberId)
            {
                // 这是字段访问：object.field 或 super.field（字段带访问修饰符）
                // 左操作数（对象或super）已经在栈上
                string fieldName = classMemberId.IdName;

                if (isSuperAccess)
                {
                    // super.field - 访问父类字段
                    Emit(OpCode.GetSuperField, fieldName);
                }
                else
                {
                    // object.field - 访问普通字段
                    Emit(OpCode.GetField, fieldName);
                }
            }
            else
            {
                // 其他情况：生成右操作数代码
                if (node.Right != null)
                    node.Right.Accept(this);
                Emit(OpCode.Nop);
            }

            return null;
        }

        // 原有逻辑：处理其他运算符

        // 特殊处理类型转换运算符 (as)
        if (node.Opera == LangTokenType.As)
        {
            // 生成左操作数（要转换的值）
            if (node.Left != null)
                node.Left.Accept(this);

            // 获取目标类型名称
            string typeName;
            if (node.Right is LangId rightId)
            {
                typeName = rightId.IdName;
            }
            else if (node.Right is TypeLangValue typeValue)
            {
                typeName = typeValue.ToString();
            }
            else if (node.Right is StringLangValue stringValue)
            {
                typeName = stringValue.Value;
            }
            else
            {
                throw new Exception($"类型转换运算符 'as' 的右操作数必须是类型名称，实际为: {node.Right?.GetType().Name}");
            }

            // 生成 Cast 指令
            Emit(OpCode.Cast, typeName);
            return null;
        }

        // 特殊处理类型检查运算符 (is)
        if (node.Opera == LangTokenType.Is)
        {
            // 生成左操作数（要检查的值）
            if (node.Left != null)
                node.Left.Accept(this);

            // 获取目标类型名称
            string typeName;
            if (node.Right is LangId rightId)
            {
                typeName = rightId.IdName;
            }
            else if (node.Right is TypeLangValue typeValue)
            {
                typeName = typeValue.ToString();
            }
            else if (node.Right is StringLangValue stringValue)
            {
                typeName = stringValue.Value;
            }
            else
            {
                throw new Exception($"类型检查运算符 'is' 的右操作数必须是类型名称，实际为: {node.Right?.GetType().Name}");
            }

            // 生成 IsType 指令
            Emit(OpCode.IsType, typeName);
            return null;
        }

        // 检查是否是一元运算符
        bool isUnaryOperator = node.Opera == LangTokenType.Exclamation || // !
                                (node.Opera == LangTokenType.Minus && node.Left == null); // 一元负号

        if (isUnaryOperator)
        {
            // 一元运算符：只生成右操作数
            if (node.Right != null)
                node.Right.Accept(this);

            // 生成一元运算符指令
            switch (node.Opera)
            {
                case LangTokenType.Exclamation:  // !
                    Emit(OpCode.Not);
                    break;
                case LangTokenType.Minus:  // 一元负号
                    Emit(OpCode.Neg);
                    break;
            }
        }
        else
        {
            // 二元运算符：生成左右操作数
            if (node.Left != null)
                node.Left.Accept(this);

            if (node.Right != null)
                node.Right.Accept(this);

            // 生成二元运算符指令
            switch (node.Opera)
            {
                case LangTokenType.Plus:
                    Emit(OpCode.Add);
                    break;
                case LangTokenType.Minus:
                    Emit(OpCode.Sub);
                    break;
                case LangTokenType.Star:
                    Emit(OpCode.Mul);
                    break;
                case LangTokenType.Slash:
                    Emit(OpCode.Div);
                    break;
                case LangTokenType.Percent:
                    Emit(OpCode.Mod);
                    break;
                case LangTokenType.Caret:  // ^ 幂运算
                    Emit(OpCode.Pow);
                    break;
                case LangTokenType.Equals:  // ==
                    Emit(OpCode.Equal);
                    break;
                case LangTokenType.NotEquals:  // !=
                    Emit(OpCode.NotEqual);
                    break;
                case LangTokenType.GreaterThan:  // >
                    Emit(OpCode.Greater);
                    break;
                case LangTokenType.LessThan:  // <
                    Emit(OpCode.Less);
                    break;
                case LangTokenType.GreaterThanEquals:  // >=
                    Emit(OpCode.GreaterEqual);
                    break;
                case LangTokenType.LessThanEquals:  // <=
                    Emit(OpCode.LessEqual);
                    break;
                case LangTokenType.And:  // &&
                    Emit(OpCode.And);
                    break;
                case LangTokenType.Or:  // ||
                    Emit(OpCode.Or);
                    break;
                default:
                    Emit(OpCode.Nop); // 未支持的运算符
                    break;
            }
        }

        return null;
    }

    public Instruction? VisitFunctionCallExpression(FunctionCallExpression node)
    {
        int positionalCount = node.Arguments.Count;
        int namedCount = node.NamedArguments?.Count ?? 0;

        // 检查函数表达式的类型
        bool isComplexExpression = node.FunctionExpression is not LangId;
        bool isClassName = false;
        string funcName = "";

        if (!isComplexExpression)
        {
            funcName = ((LangId)node.FunctionExpression).IdName;
            isClassName = _compiler.IsClassName(funcName);
        }

        // 如果是类实例化
        if (!isComplexExpression && isClassName)
        {
            // 类实例化: Person(arg1, arg2)
            // 1. 生成 NewObject 指令创建对象
            Emit(OpCode.NewObject, funcName);

            // 2. 查找构造函数：优先 init，其次与类名相同的方法
            var classMetadata = _compiler.GetClassMetadata(funcName);
            string? constructorName = null;

            if (classMetadata != null)
            {
                // 优先查找 init 方法
                if (classMetadata.Methods.Any(m => m.Name == "init"))
                {
                    constructorName = "init";
                }
                // 其次查找与类名相同的方法
                else if (classMetadata.Methods.Any(m => m.Name == funcName))
                {
                    constructorName = funcName;
                }
            }

            // 3. 如果找到构造函数，调用它
            if (constructorName != null)
            {
                // 复制对象引用，因为 CallMethod 会消耗它
                Emit(OpCode.Dup);

                // 生成位置参数代码
                foreach (var arg in node.Arguments)
                {
                    arg.Accept(this);
                }

                // 生成命名参数的值
                if (namedCount > 0)
                {
                    foreach (var namedArg in node.NamedArguments)
                    {
                        namedArg.Value.Accept(this);
                    }
                }

                // 调用构造函数
                // CallMethod 操作数: [argCount, methodName]
                // argCount 包括对象本身 + 实际参数
                int totalArgCount = positionalCount + namedCount + 1; // +1 for 'this'

                if (namedCount > 0)
                {
                    var namedArgNames = node.NamedArguments.Select(na => na.Name).ToArray();
                    Emit(OpCode.CallMethod, new object[] { totalArgCount, constructorName, namedArgNames });
                }
                else
                {
                    Emit(OpCode.CallMethod, new object[] { totalArgCount, constructorName });
                }

                // 构造函数返回 void，不需要弹出返回值
            }
        }
        else
        {
            // 普通函数调用或复杂表达式调用

            // 如果是复杂表达式，使用 CallIndirect
            if (isComplexExpression)
            {
                // 复杂表达式调用: lambda(arg1, arg2) 或 map(lambda, array)
                // 1. 编译函数表达式
                node.FunctionExpression.Accept(this);

                // 2. 生成参数代码
                foreach (var arg in node.Arguments)
                {
                    arg.Accept(this);
                }

                if (namedCount > 0)
                {
                    throw new Exception("字节码模式下的动态函数调用暂不支持命名参数");
                }

                // 3. 生成 CallDynamic 指令
                Emit(OpCode.CallDynamic, positionalCount);
            }
            // 1. 检查是否是局部变量 holding a function (Lambda调用)
            else if (_compiler.IsLocalVariable(funcName))
            {
                // 加载函数对象到栈底
                int localIndex = _compiler.GetLocalIndex(funcName);
                Emit(OpCode.LoadLocal, localIndex);

                // 生成参数代码
                foreach (var arg in node.Arguments)
                {
                    arg.Accept(this);
                }

                if (namedCount > 0)
                {
                    throw new Exception("字节码模式下的动态函数调用暂不支持命名参数");
                }

                // 调用 CallDynamic
                Emit(OpCode.CallDynamic, positionalCount);
            }
            // 2. 特殊处理 Spawn 函数
            else if ((funcName == "Spawn" || funcName == "spawn") && positionalCount > 0)
            {
                // 处理第一个参数（函数引用）
                var firstArg = node.Arguments[0];
                if (firstArg is LangId funcRefId)
                {
                    // 获取函数索引
                    int funcIndex = _compiler.GetFunctionIndex(funcRefId.IdName);
                    if (funcIndex < 0)
                    {
                        throw new Exception($"Spawn 函数引用的函数 '{funcRefId.IdName}' 未找到");
                    }
                    Emit(OpCode.LoadConst, funcIndex);
                }
                else
                {
                    // 如果不是简单的标识符，按正常方式处理
                    firstArg.Accept(this);
                }

                // 处理剩余的参数
                for (int i = 1; i < node.Arguments.Count; i++)
                {
                    node.Arguments[i].Accept(this);
                }
                
                // Spawn 是原生函数
                Emit(OpCode.CallNative, new object[] { positionalCount, funcName });
            }
            else
            {
                // 特殊处理 TaskRun 函数
                if (funcName == "TaskRun" && namedCount == 0)
                {
                    if (node.Arguments.Count < 1)
                    {
                        throw new Exception("TaskRun requires at least 1 argument");
                    }

                    var funcExpr = node.Arguments[0];
                    var taskArgs = node.Arguments.Skip(1).ToList();

                    // 1. Visit args
                    foreach (var arg in taskArgs)
                    {
                        arg.Accept(this);
                    }

                    // 2. Push arg count
                    Emit(OpCode.LoadConst, taskArgs.Count);

                    // 3. Push function
                    if (funcExpr is LangId id)
                    {
                        int funcIdx = _compiler.GetFunctionIndex(id.IdName);
                        if (funcIdx != -1)
                        {
                            Emit(OpCode.LoadConst, funcIdx);
                        }
                        else
                        {
                            Emit(OpCode.LoadConst, id.IdName);
                        }
                    }
                    else
                    {
                        funcExpr.Accept(this);
                    }

                    // 4. Emit NewTask
                    Emit(OpCode.NewTask);
                    return null;
                }

                // 特殊处理 spawn 函数 (创建线程)
                if (funcName == "spawn" && namedCount == 0)
                {
                    if (node.Arguments.Count < 1)
                    {
                        throw new Exception("spawn requires at least 1 argument");
                    }

                    var funcExpr = node.Arguments[0];
                    // 线程参数是 spawn 的后续参数
                    var threadArgs = node.Arguments.Skip(1).ToList();

                    // 1. 生成线程参数代码 (压入栈)
                    foreach (var arg in threadArgs)
                    {
                        arg.Accept(this);
                    }

                    // 2. 压入参数数量
                    Emit(OpCode.LoadConst, threadArgs.Count);

                    // 3. 压入函数引用
                    if (funcExpr is LangId id)
                    {
                        // 尝试在编译时查找函数索引
                        int funcIdx = _compiler.GetFunctionIndex(id.IdName);
                        if (funcIdx != -1)
                        {
                            Emit(OpCode.LoadConst, funcIdx);
                        }
                        else
                        {
                            // 编译时找不到，可能是变量或后定义的函数
                            // 传递函数名字符串，运行时查找
                            Emit(OpCode.LoadConst, id.IdName);
                        }
                    }
                    else
                    {
                        // 表达式求值 (如 lambda)
                        funcExpr.Accept(this);
                    }

                    // 4. 发射创建线程指令
                    Emit(OpCode.ThreadCreate);
                    return null;
                }

                // 3. 普通静态/原生函数调用
                // 生成位置参数代码
                foreach (var arg in node.Arguments)
                {
                    arg.Accept(this);
                }

                // 生成命名参数的值
                if (namedCount > 0)
                {
                    foreach (var namedArg in node.NamedArguments)
                    {
                        namedArg.Value.Accept(this);
                    }
                }

                // 检查是否是原生函数
                bool isNative = _compiler.IsNativeFunction(funcName);
                
                // 检查是否是异步函数
                bool isAsync = _compiler.IsAsyncFunction(funcName);

                if (namedCount > 0)
                {
                    if (isAsync)
                    {
                        throw new Exception("异步函数暂不支持命名参数调用");
                    }

                    // 有命名参数: [positionalCount, namedCount, funcName, namedArgNames[]]
                    var namedArgNames = node.NamedArguments.Select(na => na.Name).ToArray();
                    Emit(isNative ? OpCode.CallNative : OpCode.Call,
                        new object[] { positionalCount, namedCount, funcName, namedArgNames });
                }
                else
                {
                    // 无命名参数: [argCount, funcName]
                    if (isAsync)
                    {
                        Emit(OpCode.CallAsync, new object[] { positionalCount, funcName });
                    }
                    else
                    {
                        Emit(isNative ? OpCode.CallNative : OpCode.Call,
                            new object[] { positionalCount, funcName });
                    }
                }
            }
        }

        return null;
    }

    public Instruction? VisitClassMemberId(ClassMemberId node)
    {
        // ClassMemberId 是带访问修饰符的成员ID
        // 在字节码层面，它和普通的 LangId 类似
        // 加载成员的值
        string varName = node.IdName;

        // 检查是否是局部变量
        if (_compiler.IsLocalVariable(varName))
        {
            int localIndex = _compiler.GetLocalIndex(varName);
            Emit(OpCode.LoadLocal, localIndex);
        }
        else
        {
            // 检查是否在类方法中，且 this 是局部变量（实例方法）
            // 如果是，则应该通过 this.field 访问字段
            if (_compiler.IsLocalVariable("this"))
            {
                // 这是一个实例方法中的字段访问
                // 加载 this
                int thisIndex = _compiler.GetLocalIndex("this");
                Emit(OpCode.LoadLocal, thisIndex);

                // 加载字段
                Emit(OpCode.GetField, varName);
            }
            else
            {
                // 全局变量或静态成员
                Emit(OpCode.LoadGlobal, varName);
            }
        }

        return null;
    }

    public Instruction? VisitAwaitExpression(AwaitExpression node)
    {
        // 生成被 await 的表达式代码（应该返回 Task ID）
        node.Expression.Accept(this);

        // 发出 Await 指令
        Emit(OpCode.Await);

        return null;
    }

    public Instruction? VisitAsyncStreamExpression(AsyncStreamExpression node)
    {
        // 异步流表达式: async { block }
        // 创建一个匿名异步生成器函数来包装块

        // 获取块语句
        var block = GetPrimaryConstructorParameter<BlockStatement>(node, "Block");
        if (block == null)
        {
            return null;
        }

        // 编译为异步生成器函数
        var funcName = $"<async_stream_{GetCurrentPosition()}>";
        var parameters = new List<string>();
        var parameterTypes = new List<string>();
        var defaultValues = new List<object?>();

        // 编译异步生成器函数
        var function = _compiler.CompileAsyncGeneratorFunction(funcName, parameters, parameterTypes, defaultValues, block);

        // 查找函数在字节码文件中的索引
        var funcIndex = _compiler.GetFunctionIndex(funcName);

        // 调用函数（无参数）
        Emit(OpCode.Call, new object[] { 0, funcName });

        return null;
    }

    public Instruction? VisitTernaryExpression(TernaryExpression node)
    {
        // 三元运算符: condition ? trueExpr : falseExpr
        // 生成条件表达式代码
        node.Condition.Accept(this);

        // 如果条件为false，跳转到false分支
        int jumpIfFalseIndex = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1);

        // True分支
        node.TrueExpression.Accept(this);
        int jumpToEndIndex = GetCurrentPosition();
        Emit(OpCode.Jump, -1); // 跳转到结束

        // False分支
        PatchJump(jumpIfFalseIndex, GetCurrentPosition());
        node.FalseExpression.Accept(this);

        // 结束
        PatchJump(jumpToEndIndex, GetCurrentPosition());

        return null;
    }

    public Instruction? VisitMatchExpression(MatchExpression node)
    {
        // Match 表达式（模式匹配）
        // 实现完整的 match 支持：
        // 1. 值匹配
        // 2. 通配符匹配 (_)
        // 3. 变量绑定
        // 4. 类型匹配（带可选守卫条件）
        // 5. 元组解构匹配
        // 6. 范围匹配

        // 1. 计算被匹配的值并存储到局部变量
        node.MatchValue.Accept(this);
        int matchValueLocal = _compiler.AllocateLocal();
        Emit(OpCode.StoreLocal, matchValueLocal);

        // 2. 为每个 case 创建标签
        var caseLabels = new List<int>();
        var endLabel = -1; // 将在最后设置

        // 3. 生成每个 case 的匹配判断
        for (int i = 0; i < node.Cases.Count; i++)
        {
            var matchCase = node.Cases[i];

            // 检查是否是通配符或变量绑定（匹配所有）
            if (matchCase.IsWildcard || matchCase.IsVariableBinding)
            {
                // 通配符或变量绑定：直接跳转到对应的 case
                int jumpIndex = GetCurrentPosition();
                Emit(OpCode.Jump, -1); // 占位，稍后修补
                caseLabels.Add(jumpIndex);
                continue;
            }

            // 根据模式类型生成匹配代码
            switch (matchCase.Type)
            {
                case PatternType.Value:
                    // 值匹配：比较 match 值和 case 值
                    Emit(OpCode.LoadLocal, matchValueLocal);
                    matchCase.Pattern!.Accept(this);
                    Emit(OpCode.Equal);

                    // 如果匹配，跳转到对应的 case
                    int jumpIfTrueIndex = GetCurrentPosition();
                    Emit(OpCode.JumpIfTrue, -1); // 占位，稍后修补
                    caseLabels.Add(jumpIfTrueIndex);
                    break;

                case PatternType.TypeMatch:
                    // 类型匹配（带可选守卫条件）
                    GenerateTypeMatchCode(matchCase, matchValueLocal);
                    int typeMatchJumpIndex = GetCurrentPosition();
                    Emit(OpCode.JumpIfTrue, -1); // 占位，稍后修补
                    caseLabels.Add(typeMatchJumpIndex);
                    break;

                case PatternType.Range:
                    // 范围匹配
                    GenerateRangeMatchCode(matchCase, matchValueLocal);
                    int rangeMatchJumpIndex = GetCurrentPosition();
                    Emit(OpCode.JumpIfTrue, -1); // 占位，稍后修补
                    caseLabels.Add(rangeMatchJumpIndex);
                    break;

                case PatternType.Tuple:
                    // 元组解构匹配
                    GenerateTupleMatchCode(matchCase, matchValueLocal);
                    int tupleMatchJumpIndex = GetCurrentPosition();
                    Emit(OpCode.JumpIfTrue, -1); // 占位，稍后修补
                    caseLabels.Add(tupleMatchJumpIndex);
                    break;

                default:
                    throw new NotSupportedException($"虚拟机模式暂不支持模式类型: {matchCase.Type}");
            }
        }

        // 4. 所有 case 都不匹配，抛出异常
        Emit(OpCode.LoadConst, _compiler.AddConstant("Match 表达式没有匹配的分支"));
        Emit(OpCode.Throw);

        // 5. 生成各个 case 的执行代码
        var caseEndJumps = new List<int>();

        for (int i = 0; i < node.Cases.Count; i++)
        {
            var matchCase = node.Cases[i];

            // 修补跳转到此 case 的指令
            PatchJump(caseLabels[i], GetCurrentPosition());

            // 如果有变量绑定，将 match 值赋给局部变量
            if (matchCase.BindingVariable != null)
            {
                Emit(OpCode.LoadLocal, matchValueLocal);
                int bindingLocal = _compiler.AllocateLocal(matchCase.BindingVariable);
                Emit(OpCode.StoreLocal, bindingLocal);
            }

            // 执行 case 的结果表达式
            matchCase.ResultExpression.Accept(this);

            // 跳转到结束
            int jumpToEndIndex = GetCurrentPosition();
            Emit(OpCode.Jump, -1); // 占位，稍后修补
            caseEndJumps.Add(jumpToEndIndex);
        }

        // 6. 修补所有跳转到结束的指令
        int endPosition = GetCurrentPosition();
        foreach (var jumpIndex in caseEndJumps)
        {
            PatchJump(jumpIndex, endPosition);
        }

        // 7. 释放 match 值的局部变量
        _compiler.FreeLocal(matchValueLocal);

        return null;
    }

    /// <summary>
    /// 生成类型匹配代码（带可选守卫条件）
    /// </summary>
    private void GenerateTypeMatchCode(MatchCase matchCase, int matchValueLocal)
    {
        // 加载 match 值
        Emit(OpCode.LoadLocal, matchValueLocal);

        // 调用类型检查指令
        Emit(OpCode.IsType, matchCase.TypeAnnotation!);

        // 如果有守卫条件，需要额外检查
        if (matchCase.GuardCondition != null)
        {
            // 如果类型不匹配，跳过守卫条件检查
            int skipGuardJump = GetCurrentPosition();
            Emit(OpCode.JumpIfFalse, -1); // 占位

            // 类型匹配，绑定变量并评估守卫条件
            Emit(OpCode.LoadLocal, matchValueLocal);
            int guardBindingLocal = _compiler.AllocateLocal(matchCase.BindingVariable!);
            Emit(OpCode.StoreLocal, guardBindingLocal);

            // 评估守卫条件
            matchCase.GuardCondition.Accept(this);

            // 释放临时变量
            _compiler.FreeLocal(guardBindingLocal);

            // 修补跳过守卫的跳转
            PatchJump(skipGuardJump, GetCurrentPosition());
        }
    }

    /// <summary>
    /// 生成范围匹配代码
    /// </summary>
    private void GenerateRangeMatchCode(MatchCase matchCase, int matchValueLocal)
    {
        var rangePattern = matchCase.RangePattern!;

        // 加载 match 值
        Emit(OpCode.LoadLocal, matchValueLocal);

        // 计算范围起始值
        rangePattern.Start.Accept(this);

        // 计算范围结束值
        rangePattern.End.Accept(this);

        // 加载范围包含标志
        Emit(OpCode.LoadConst, _compiler.AddConstant(rangePattern.IncludeStart));
        Emit(OpCode.LoadConst, _compiler.AddConstant(rangePattern.IncludeEnd));

        // 调用原生函数进行范围检查
        // 参数顺序: value, start, end, includeStart, includeEnd
        Emit(OpCode.CallNative, new object[] { 5, "CheckRange" });
    }

    /// <summary>
    /// 生成元组解构匹配代码
    /// </summary>
    private void GenerateTupleMatchCode(MatchCase matchCase, int matchValueLocal)
    {
        var tuplePattern = matchCase.TuplePattern!;

        // 加载 match 值
        Emit(OpCode.LoadLocal, matchValueLocal);

        // 检查是否是元组类型
        Emit(OpCode.IsType, "tuple");

        // 如果不是元组，返回 false
        int notTupleJump = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1); // 占位

        // 展平元组并检查元素数量
        Emit(OpCode.LoadLocal, matchValueLocal);
        Emit(OpCode.CallNative, new object[] { 1, "FlattenTuple" });
        int flattenedLocal = _compiler.AllocateLocal();
        Emit(OpCode.StoreLocal, flattenedLocal);

        // 检查元素数量
        Emit(OpCode.LoadLocal, flattenedLocal);
        Emit(OpCode.CallNative, new object[] { 1, "GetCount" });
        Emit(OpCode.LoadConst, _compiler.AddConstant(tuplePattern.Elements.Count));
        Emit(OpCode.Equal);

        // 如果数量不匹配，返回 false
        int countMismatchJump = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1); // 占位

        // 逐个匹配元组元素
        bool allMatched = true;
        var elementMismatchJumps = new List<int>();

        for (int i = 0; i < tuplePattern.Elements.Count; i++)
        {
            var patternElement = tuplePattern.Elements[i];

            // 通配符跳过
            if (patternElement.IsWildcard)
                continue;

            // 获取元组元素
            Emit(OpCode.LoadLocal, flattenedLocal);
            Emit(OpCode.LoadConst, _compiler.AddConstant(i));
            Emit(OpCode.GetIndex);

            if (patternElement.Variable != null)
            {
                // 变量绑定：存储到局部变量
                int elementLocal = _compiler.AllocateLocal(patternElement.Variable);
                Emit(OpCode.StoreLocal, elementLocal);
            }
            else if (patternElement.Value != null)
            {
                // 值匹配：比较值
                patternElement.Value.Accept(this);
                Emit(OpCode.Equal);

                // 如果不匹配，跳转
                int mismatchJump = GetCurrentPosition();
                Emit(OpCode.JumpIfFalse, -1); // 占位
                elementMismatchJumps.Add(mismatchJump);
            }
        }

        // 所有元素都匹配，压入 true
        Emit(OpCode.LoadTrue);
        int successJump = GetCurrentPosition();
        Emit(OpCode.Jump, -1); // 占位

        // 修补不匹配的跳转（压入 false）
        PatchJump(notTupleJump, GetCurrentPosition());
        PatchJump(countMismatchJump, GetCurrentPosition());
        foreach (var jump in elementMismatchJumps)
        {
            PatchJump(jump, GetCurrentPosition());
        }
        Emit(OpCode.LoadFalse);

        // 修补成功跳转
        PatchJump(successJump, GetCurrentPosition());

        // 释放临时变量
        _compiler.FreeLocal(flattenedLocal);
    }

    public Instruction? VisitGenericInstanceExpression(GenericInstanceExpression node)
    {
        // 泛型实例化的字节码生成
        // 策略：在编译时进行泛型特化，生成具体类型的类或函数

        // 获取基础表达式名称
        if (node.BaseExpression is not LangId identifier)
        {
            throw new InvalidOperationException("字节码模式下泛型表达式必须使用简单的标识符");
        }

        var name = identifier.IdName;

        // 判断是泛型类还是泛型函数
        if (_compiler.IsGenericClass(name))
        {
            // 处理泛型类实例化
            HandleGenericClassInstantiation(node, name);
        }
        else if (_compiler.IsGenericFunction(name))
        {
            // 处理泛型函数调用
            HandleGenericFunctionCall(node, name);
        }
        else
        {
            throw new InvalidOperationException($"找不到泛型类或泛型函数定义：{name}");
        }

        return null;
    }

    public Instruction? VisitLinqExpression(LinqExpression node)
    {
        // LINQ 查询表达式的字节码生成策略:
        // 1. 获取数据源并转换为迭代器
        // 2. 遍历数据源,对每个元素应用查询子句
        // 3. 收集结果到列表中
        // 4. 返回结果列表

        // 步骤1: 计算数据源表达式
        node.FromClause.DataSource.Accept(this);

        // 获取迭代器
        Emit(OpCode.GetIterator);
        int iteratorLocal = _compiler.AllocateLocal();
        Emit(OpCode.StoreLocal, iteratorLocal);

        // 检查终止子句类型,决定创建列表还是分组字典
        bool isGroupBy = node.TerminationClause is GroupByClause;

        if (isGroupBy)
        {
            // 创建分组字典 (用于 GroupBy)
            Emit(OpCode.NewGroupDict);
        }
        else
        {
            // 创建结果列表 (用于 Select)
            Emit(OpCode.NewList, 0);
        }

        int resultListLocal = _compiler.AllocateLocal();
        Emit(OpCode.StoreLocal, resultListLocal);

        // 为范围变量分配局部变量槽
        int rangeVarLocal = _compiler.AllocateLocal(node.FromClause.RangeVariable);

        // 为 let 变量分配局部变量槽
        var letVariables = new Dictionary<string, int>();
        foreach (var clause in node.BodyClauses)
        {
            if (clause is LetClause letClause)
            {
                int letVarLocal = _compiler.AllocateLocal(letClause.Variable);
                letVariables[letClause.Variable] = letVarLocal;
            }
        }

        // 步骤2: 遍历数据源
        int loopStartPos = GetCurrentPosition();

        // 检查迭代器是否有下一个元素
        Emit(OpCode.LoadLocal, iteratorLocal);
        Emit(OpCode.IteratorMoveNext);

        // 如果没有下一个元素,跳出循环
        int loopEndJump = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1); // 占位,稍后修补

        // 获取当前元素
        Emit(OpCode.LoadLocal, iteratorLocal);
        Emit(OpCode.IteratorCurrent);

        // 将当前元素存储到范围变量
        Emit(OpCode.StoreLocal, rangeVarLocal);

        // 步骤3: 处理查询体子句 (where, let)
        var skipElementJumps = new List<int>();
        ProcessLinqBodyClauses(node.BodyClauses, letVariables, skipElementJumps);

        // 步骤4: 执行终止子句 (select)
        ProcessLinqTerminationClause(node.TerminationClause, resultListLocal);

        // 跳回循环开始
        int continueJump = GetCurrentPosition();
        Emit(OpCode.Jump, loopStartPos);

        // 修补所有跳过元素的跳转 (where 条件不满足时跳到这里)
        foreach (var jumpIndex in skipElementJumps)
        {
            PatchJump(jumpIndex, continueJump);
        }

        // 修补循环结束跳转
        PatchJump(loopEndJump, GetCurrentPosition());

        // 步骤5: 处理 OrderBy (如果有)
        var orderByClause = node.BodyClauses.OfType<OrderByClause>().FirstOrDefault();
        if (orderByClause != null)
        {
            ProcessLinqOrderBy(orderByClause, resultListLocal);
        }

        // 步骤6: 如果是 GroupBy,将分组字典转换为分组列表
        if (isGroupBy)
        {
            Emit(OpCode.LoadLocal, resultListLocal);
            Emit(OpCode.GroupDictToList);
            Emit(OpCode.StoreLocal, resultListLocal);
        }

        // 步骤7: 加载结果列表到栈
        Emit(OpCode.LoadLocal, resultListLocal);

        // 释放局部变量
        _compiler.FreeLocal(iteratorLocal);
        _compiler.FreeLocal(resultListLocal);
        _compiler.FreeLocal(rangeVarLocal);
        foreach (var letVar in letVariables.Values)
        {
            _compiler.FreeLocal(letVar);
        }

        return null;
    }

    // ===== 泛型处理辅助方法 =====

    /// <summary>
    /// 处理泛型类实例化
    /// </summary>
    private void HandleGenericClassInstantiation(GenericInstanceExpression node, string className)
    {
        // 获取泛型类模板
        var typeTemplate = _compiler.GenericClasses[className];

        // 构建特化类名：ClassName$Type1_Type2_...
        // 解析类型参数时，需要考虑当前的类型参数映射
        var typeArgNames = node.TypeArguments.Select(typeArg =>
        {
            var resolvedType = ResolveSimpleTypeName(typeArg);
            // 如果类型参数在当前映射中，替换为实际类型
            if (_compiler.CurrentTypeParameterMapping.TryGetValue(resolvedType, out var mappedType))
            {
                return mappedType;
            }
            return resolvedType;
        }).ToArray();
        var specializedClassName = $"{className}${string.Join("_", typeArgNames)}";

        // 检查是否已经生成过特化类
        if (!_compiler.IsClassName(specializedClassName))
        {
            // 生成特化类定义
            GenerateSpecializedClass(typeTemplate, typeArgNames.ToList(), specializedClassName);
        }

        // 生成创建对象的字节码
        Emit(OpCode.NewObject, specializedClassName);

        // 查找并调用构造函数
        var classMetadata = _compiler.GetClassMetadata(specializedClassName);
        string? constructorName = null;

        if (classMetadata != null)
        {
            // 优先查找 init 方法
            if (classMetadata.Methods.Any(m => m.Name == "init"))
            {
                constructorName = "init";
            }
            // 其次查找与原始类名相同的方法
            else if (classMetadata.Methods.Any(m => m.Name == className))
            {
                constructorName = className;
            }
        }

        // 如果找到构造函数，调用它
        if (constructorName != null && node.CallArguments != null)
        {
            // 复制对象引用，因为 CallMethod 会消耗它
            Emit(OpCode.Dup);

            // 生成参数代码
            foreach (var arg in node.CallArguments)
            {
                arg.Accept(this);
            }

            // 调用构造函数
            // CallMethod 操作数: [argCount, methodName]
            // argCount 包括对象本身 + 实际参数
            int totalArgCount = node.CallArguments.Count + 1; // +1 for 'this'
            Emit(OpCode.CallMethod, new object[] { totalArgCount, constructorName });

            // 构造函数返回 void，不需要弹出返回值
        }
    }

    /// <summary>
    /// 处理泛型函数调用
    /// </summary>
    private void HandleGenericFunctionCall(GenericInstanceExpression node, string funcName)
    {
        // 获取泛型函数定义
        var genericFunc = _compiler.GenericFunctions[funcName];

        // 构建特化函数名：FuncName$Type1_Type2_...
        var typeArgNames = node.TypeArguments.Select(ResolveSimpleTypeName).ToArray();
        var specializedFuncName = $"{funcName}${string.Join("_", typeArgNames)}";

        // 检查是否已经生成过特化函数
        if (_compiler.GetFunctionIndex(specializedFuncName) == -1)
        {
            // 生成特化函数定义
            GenerateSpecializedFunction(genericFunc, node.TypeArguments, specializedFuncName);
        }

        // 生成调用参数的字节码
        if (node.CallArguments != null)
        {
            foreach (var arg in node.CallArguments)
            {
                arg.Accept(this);
            }
        }

        // 生成函数调用字节码
        int argCount = node.CallArguments?.Count ?? 0;
        Emit(OpCode.Call, new object[] { argCount, specializedFuncName });
    }
}
