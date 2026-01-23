using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 特殊语句
/// </summary>
public partial class BytecodeVisitor
{
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
            Emit(OpCode.CallNative, new object[] { 1, "Sleep" });

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
                if (instruction is { OpCode: OpCode.Jump, Operand: -1 })
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
            memberData[index++] = kvp.Key; // 成员名
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

        var dllNameField = nodeType.GetField("_dllName", bindingFlags);
        var dllName = dllNameField?.GetValue(node) as string
                      ?? throw new InvalidOperationException("无法获取 DLL 名称");

        var functionsField = nodeType.GetField("_functions", bindingFlags);
        var functions = functionsField?.GetValue(node) as List<ExternFunctionDeclaration>
                        ?? throw new InvalidOperationException("无法获取函数列表");

        var externTypeField = nodeType.GetField("_externType", bindingFlags);
        var externType = externTypeField != null
            ? (ExternType)externTypeField.GetValue(node)!
            : ExternType.NativeDll;

        var defaultCallingConventionField = nodeType.GetField("_defaultCallingConvention", bindingFlags);
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
                var paramTypes = sig.Ids?.Select(p => p.AssumptionType).ToList() ?? [];
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
        var resourceLocalIndex =
            _compiler.AllocateLocal(node.VariableName ?? "<using_resource>"); // 如果有变量名，使用用户指定的变量名；如果没有变量名，使用临时变量

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
            CatchStart = -1, // 没有 catch 块
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
    /// <summary>
    /// 递归处理嵌套类
    /// </summary>
    private void ProcessNestedClasses(TypeTemplate typeTemplate)
    {
        // 处理实例成员中的嵌套类
        foreach (var (_, memberExpr) in typeTemplate.Variates)
        {
            if (memberExpr is TypeTemplate nestedTypeTemplate)
            {
                // 创建带有完整路径的嵌套类名称
                var fullNestedClassName = typeTemplate.ClassName + "." + nestedTypeTemplate.ClassName;

                // 创建新的 TypeTemplate，使用完整的类名
                var updatedNestedTemplate = new TypeTemplate(
                    fullNestedClassName,
                    nestedTypeTemplate.Variates,
                    nestedTypeTemplate.StaticVariates,
                    nestedTypeTemplate.ParentClassName,
                    nestedTypeTemplate.IsMixin,
                    nestedTypeTemplate.MixinNames,
                    nestedTypeTemplate.ImplementsNames,
                    nestedTypeTemplate.IsInterface,
                    nestedTypeTemplate.IsAbstract,
                    nestedTypeTemplate.GenericParameters,
                    nestedTypeTemplate.ParentGenericTypeParameters,
                    nestedTypeTemplate.Position
                );

                // 递归编译嵌套类
                var nestedClassInit = new ClassInit(updatedNestedTemplate, updatedNestedTemplate.Position);
                nestedClassInit.Accept(this);
            }
        }

        // 处理静态成员中的嵌套类
        foreach (var (_, memberExpr) in typeTemplate.StaticVariates)
        {
            if (memberExpr is TypeTemplate nestedTypeTemplate)
            {
                // 创建带有完整路径的嵌套类名称
                var fullNestedClassName = typeTemplate.ClassName + "." + nestedTypeTemplate.ClassName;

                // 创建新的 TypeTemplate，使用完整的类名
                var updatedNestedTemplate = new TypeTemplate(
                    fullNestedClassName,
                    nestedTypeTemplate.Variates,
                    nestedTypeTemplate.StaticVariates,
                    nestedTypeTemplate.ParentClassName,
                    nestedTypeTemplate.IsMixin,
                    nestedTypeTemplate.MixinNames,
                    nestedTypeTemplate.ImplementsNames,
                    nestedTypeTemplate.IsInterface,
                    nestedTypeTemplate.IsAbstract,
                    nestedTypeTemplate.GenericParameters,
                    nestedTypeTemplate.ParentGenericTypeParameters,
                    nestedTypeTemplate.Position
                );

                // 递归编译嵌套类
                var nestedClassInit = new ClassInit(updatedNestedTemplate, updatedNestedTemplate.Position);
                nestedClassInit.Accept(this);
            }
        }
    }

    private void CompileInterfaceDefinition(TypeTemplate typeTemplate)
    {
        var interfaceName = typeTemplate.ClassName;
        var methods = new List<string>();

        // 提取接口方法签名
        foreach (var (memberId, memberExpr) in typeTemplate.Variates)
        {
            if (memberExpr is FuncLangValue)
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

        if (decorator.Arguments is { Count: > 0 })
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