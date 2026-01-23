using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;

namespace Old8Lang.Bytecode;

/// <summary>
/// BytecodeVisitor - Value节点的实现
/// </summary>
public partial class BytecodeVisitor
{
    // ===== 基础值类型 =====

    public Instruction? VisitIntLangValue(IntLangValue node)
    {
        int constIndex = _compiler.ConstantPool.AddConstant(node.Value);
        Emit(OpCode.LoadConst, constIndex);
        return null;
    }

    public Instruction? VisitDoubleLangValue(DoubleLangValue node)
    {
        int constIndex = _compiler.ConstantPool.AddConstant(node.Value);
        Emit(OpCode.LoadConst, constIndex);
        return null;
    }

    public Instruction? VisitStringLangValue(StringLangValue node)
    {
        int constIndex = _compiler.ConstantPool.AddConstant(node.Value);
        Emit(OpCode.LoadConst, constIndex);
        return null;
    }

    public Instruction? VisitBoolLangValue(BoolLangValue node)
    {
        Emit(node.Value ? OpCode.LoadTrue : OpCode.LoadFalse);
        return null;
    }

    public Instruction? VisitCharLangValue(CharLangValue node)
    {
        int constIndex = _compiler.ConstantPool.AddConstant(node.Value);
        Emit(OpCode.LoadConst, constIndex);
        return null;
    }

    public Instruction? VisitNullLangValue(NullLangValue node)
    {
        Emit(OpCode.LoadNull);
        return null;
    }

    public Instruction? VisitVoidLangValue(VoidLangValue node)
    {
        Emit(OpCode.LoadNull); // Void表示为null
        return null;
    }

    // ===== 容器类型 =====

    public Instruction? VisitArrayLangValue(ArrayLangValue node)
    {
        // 为每个元素生成代码
        foreach (var value in node.Values)
        {
            value.Accept(this);
        }

        // 创建数组
        Emit(OpCode.NewArray, node.Values.Count);
        return null;
    }

    public Instruction? VisitListLangValue(ListLangValue node)
    {
        // 为每个元素生成代码
        foreach (var expr in node.Value)
        {
            expr.Accept(this);
        }

        // 创建列表
        Emit(OpCode.NewList, node.Value.Count);
        return null;
    }

    public Instruction? VisitDictionaryLangValue(DictionaryLangValue node)
    {
        // 生成所有键值对的代码
        foreach (var tuple in node.Tuples)
        {
            // 访问元组，会生成键和值的代码
            tuple.Accept(this);
        }

        // 创建字典，参数是键值对的数量
        Emit(OpCode.NewDict, node.Tuples.Count);
        return null;
    }

    public Instruction? VisitTupleLangValue(TupleLangValue node)
    {
        // 生成所有元素的代码
        foreach (var element in node.Elements)
        {
            element.Accept(this);
        }

        // 创建元组，参数是元素数量
        Emit(OpCode.NewTuple, node.Elements.Count);
        return null;
    }

    // ===== 其他值类型 - 默认实现 =====

    public Instruction? VisitAnyLangValue(AnyLangValue node) => null;
    public Instruction? VisitAsyncGeneratorLangValue(AsyncGeneratorLangValue node) => null;
    public Instruction? VisitAsyncStreamLangValue(AsyncStreamLangValue node) => null;
    public Instruction? VisitCancellationTokenLangValue(CancellationTokenLangValue node) => null;
    public Instruction? VisitCancellationTokenSourceLangValue(CancellationTokenSourceLangValue node) => null;
    public Instruction? VisitErrorLangValue(ErrorLangValue node) => null;
    public Instruction? VisitGeneratorLangValue(GeneratorLangValue node) => null;
    public Instruction? VisitInstance(Instance node)
    {
        // Instance 是函数调用表达式 a(b, c)
        string funcName = node.Id.IdName;
        int positionalCount = node.Ids.Count;
        int namedCount = node.NamedArgs?.Count ?? 0;

        // 检查是否是类实例化
        bool isClassName = _compiler.IsClassName(funcName);

        if (isClassName)
        {
            // 类实例化: Person(arg1, arg2)
            // 1. 生成 NewObject 指令创建对象
            Emit(OpCode.NewObject, funcName);

            // 2. 查找构造函数：优先 init，其次与类名相同的方法
            // 沿着继承链查找构造函数
            var classMetadata = _compiler.GetClassMetadata(funcName);
            string? constructorName = null;
            ClassMetadata? currentClass = classMetadata;

            while (currentClass != null && constructorName == null)
            {
                // 优先查找 init 方法
                if (currentClass.Methods.Any(m => m.Name == "init"))
                {
                    constructorName = "init";
                    break;
                }
                // 其次查找与类名相同的方法（只在当前类中查找，不在父类中查找）
                else if (currentClass.Name == funcName && currentClass.Methods.Any(m => m.Name == funcName))
                {
                    constructorName = funcName;
                    break;
                }

                // 在父类中继续查找
                if (!string.IsNullOrEmpty(currentClass.BaseClassName))
                {
                    currentClass = _compiler.GetClassMetadata(currentClass.BaseClassName);
                }
                else
                {
                    break;
                }
            }

            // 3. 如果找到构造函数，调用它
            if (constructorName != null)
            {
                // 复制对象引用，因为 CallMethod 会消耗它
                Emit(OpCode.Dup);

                // 生成位置参数代码
                foreach (var arg in node.Ids)
                {
                    arg.Accept(this);
                }

                // 生成命名参数的值
                if (namedCount > 0)
                {
                    foreach (var namedArg in node.NamedArgs)
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
                    var namedArgNames = node.NamedArgs.Select(na => na.Name).ToArray();
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
            // 普通函数调用

            // 检查函数名是否是局部变量或全局变量（Lambda 函数）
            bool isLocalVariable = _compiler.IsLocalVariable(funcName);
            bool isGlobalVariable = _compiler.IsGlobalVariable(funcName);
            bool isLambdaVariable = isLocalVariable || isGlobalVariable;
            bool isFunctionDefined = _compiler.GetFunctionIndex(funcName) >= 0;

            // 如果是 Lambda 变量，使用 CallDynamic 指令
            if (isLambdaVariable && !isFunctionDefined)
            {
                // Lambda 变量调用: f(arg1, arg2)
                // 1. 加载函数对象到栈
                if (isLocalVariable)
                {
                    int localIndex = _compiler.GetLocalIndex(funcName);
                    Emit(OpCode.LoadLocal, localIndex);
                }
                else
                {
                    Emit(OpCode.LoadGlobal, funcName);
                }

                // 2. 生成参数代码（位置参数 + 命名参数）
                foreach (var arg in node.Ids)
                {
                    arg.Accept(this);
                }

                // 生成命名参数的值
                if (namedCount > 0)
                {
                    foreach (var namedArg in node.NamedArgs)
                    {
                        namedArg.Value.Accept(this);
                    }
                }

                // 3. 使用 CallDynamic 指令调用
                int totalArgCount = positionalCount + namedCount;
                Emit(OpCode.CallDynamic, totalArgCount);
            }
            else
            {
                // 普通函数调用
                // 生成参数代码（位置参数 + 命名参数）

                // 先生成位置参数
                foreach (var arg in node.Ids)
                {
                    arg.Accept(this);
                }

                // 生成命名参数的值
                if (namedCount > 0)
                {
                    foreach (var namedArg in node.NamedArgs)
                    {
                        namedArg.Value.Accept(this);
                    }
                }

                // 检查是否是原生函数
                if (_compiler.IsNativeFunction(funcName))
                {
                    if (namedCount > 0)
                    {
                        // 有命名参数: [positionalCount, namedCount, funcName, namedArgNames[]]
                        var namedArgNames = node.NamedArgs.Select(na => na.Name).ToArray();
                        Emit(OpCode.CallNative, new object[] { positionalCount, namedCount, funcName, namedArgNames });
                    }
                    else
                    {
                        // 无命名参数: [argCount, funcName]
                        Emit(OpCode.CallNative, new object[] { positionalCount, funcName });
                    }
                }
                // 检查是否是生成器函数（包括异步生成器）
                // 注意：生成器函数调用不执行函数体，而是创建生成器对象
                else if (_compiler.IsGeneratorFunction(funcName))
                {
                    if (namedCount > 0)
                    {
                        // 有命名参数: [positionalCount, namedCount, funcName, namedArgNames[]]
                        var namedArgNames = node.NamedArgs.Select(na => na.Name).ToArray();
                        Emit(OpCode.Call, new object[] { positionalCount, namedCount, funcName, namedArgNames });
                    }
                    else
                    {
                        // 无命名参数: [argCount, funcName]
                        Emit(OpCode.Call, new object[] { positionalCount, funcName });
                    }
                }
                // 检查是否是异步函数（非生成器）
                else if (_compiler.IsAsyncFunction(funcName))
                {
                    if (namedCount > 0)
                    {
                        // 有命名参数: [positionalCount, namedCount, funcName, namedArgNames[]]
                        var namedArgNames = node.NamedArgs.Select(na => na.Name).ToArray();
                        Emit(OpCode.CallAsync, new object[] { positionalCount, namedCount, funcName, namedArgNames });
                    }
                    else
                    {
                        // 无命名参数: [argCount, funcName]
                        Emit(OpCode.CallAsync, new object[] { positionalCount, funcName });
                    }
                }
                else
                {
                    if (namedCount > 0)
                    {
                        // 有命名参数: [positionalCount, namedCount, funcName, namedArgNames[]]
                        var namedArgNames = node.NamedArgs.Select(na => na.Name).ToArray();
                        Emit(OpCode.Call, new object[] { positionalCount, namedCount, funcName, namedArgNames });
                    }
                    else
                    {
                        // 无命名参数: [argCount, funcName]
                        Emit(OpCode.Call, new object[] { positionalCount, funcName });
                    }
                }
            }
        }

        return null;
    }
    public Instruction? VisitLangListItem(LangListItem node)
    {
        // 访问列表/数组/字典变量
        node.ListId.Accept(this);

        // 访问索引/键
        node.Key.Accept(this);

        // 发出GetIndex指令来执行索引访问
        Emit(OpCode.GetIndex);

        return null;
    }
    public Instruction? VisitListComprehension(ListComprehension node) => null;
    public Instruction? VisitMethodOverloadList(MethodOverloadList node) => null;
    public Instruction? VisitNestedIndexAccess(NestedIndexAccess node)
    {
        // 访问基础表达式 (例如 array[index1] 或更深的嵌套)
        node.BaseExpression.Accept(this);

        // 访问嵌套索引 (例如 index2)
        node.NestedIndex.Accept(this);

        // 发出GetIndex指令来执行嵌套索引访问
        Emit(OpCode.GetIndex);

        return null;
    }
    public Instruction? VisitNestedSliceAccess(NestedSliceAccess node)
    {
        // 访问基础表达式
        node.BaseExpression.Accept(this);

        // 访问切片起始索引
        node.SliceStart.Accept(this);

        // 访问切片结束索引(如果有)
        if (node.SliceEnd != null)
        {
            node.SliceEnd.Accept(this);
        }
        else
        {
            // 如果没有结束索引,使用int.MaxValue表示到末尾
            int constIndex = _compiler.ConstantPool.AddConstant(int.MaxValue);
            Emit(OpCode.LoadConst, constIndex);
        }

        // 访问切片步长(如果有)
        if (node.SliceStep != null)
        {
            node.SliceStep.Accept(this);
        }
        else
        {
            // 如果没有步长,默认为1
            int constIndex = _compiler.ConstantPool.AddConstant(1);
            Emit(OpCode.LoadConst, constIndex);
        }

        // 发出Slice指令
        Emit(OpCode.Slice);

        return null;
    }
    public Instruction? VisitRangeLangValue(RangeLangValue node)
    {
        // 访问start表达式
        if (node.Start != null)
        {
            node.Start.Accept(this);
        }
        else
        {
            Emit(OpCode.LoadConst, 0); // 默认起始值为0
        }

        // 访问end表达式
        if (node.End != null)
        {
            node.End.Accept(this);
        }
        else
        {
            Emit(OpCode.LoadConst, 0); // 默认结束值为0
        }

        // 加载includeStart和includeEnd标志
        // 栈布局(从下到上): start, end, includeStart, includeEnd
        Emit(OpCode.LoadConst, node.IncludeStart ? 1 : 0);
        Emit(OpCode.LoadConst, node.IncludeEnd ? 1 : 0);

        // 使用 NewRange 指令创建范围数组
        // NewRange 将从栈中弹出4个参数
        Emit(OpCode.NewRange);

        return null;
    }
    public Instruction? VisitSliceLangValue(SliceLangValue node)
    {
        // 访问集合变量
        node.Id.Accept(this);

        // 访问起始索引(如果有)
        if (node.Start != null)
        {
            node.Start.Accept(this);
        }
        else
        {
            // 默认起始索引为0
            int constIndex = _compiler.ConstantPool.AddConstant(0);
            Emit(OpCode.LoadConst, constIndex);
        }

        // 访问结束索引(如果有)
        if (node.End != null)
        {
            node.End.Accept(this);
        }
        else
        {
            // 默认结束索引为int.MaxValue(表示到末尾)
            int constIndex = _compiler.ConstantPool.AddConstant(int.MaxValue);
            Emit(OpCode.LoadConst, constIndex);
        }

        // 访问步长(如果有)
        if (node.Step != null)
        {
            node.Step.Accept(this);
        }
        else
        {
            // 默认步长为1
            int constIndex = _compiler.ConstantPool.AddConstant(1);
            Emit(OpCode.LoadConst, constIndex);
        }

        // 发出Slice指令
        Emit(OpCode.Slice);

        return null;
    }
    public Instruction? VisitStringTemplateValue(StringTemplateValue node)
    {
        // 字符串模板: $"Hello {name}, you are {age} years old"
        // 策略: 将所有表达式结果压入栈，创建数组，然后调用string.Concat

        var expressionList = node.ExpressionList;

        if (expressionList.Count == 0)
        {
            // 空模板,返回空字符串
            Emit(OpCode.LoadConst, _compiler.ConstantPool.AddConstant(""));
            return null;
        }

        // 1. 遍历所有表达式,将结果压入栈
        foreach (var expr in expressionList)
        {
            expr.Accept(this);
        }

        // 2. 创建对象数组: NewArray指令 (会从栈中弹出 expressionList.Count 个元素)
        Emit(OpCode.NewArray, expressionList.Count);

        // 3. 调用string.Concat(object[])方法
        var concatMethodName = "System.String::Concat";
        Emit(OpCode.CallNative, new object[] { 1, concatMethodName });

        return null;
    }
    public Instruction? VisitSuperExpression(SuperExpression node)
    {
        // 加载 super 引用（实际上是加载当前实例 this）
        // LoadSuper 指令会将当前实例压栈，并标记为 super 上下文
        Emit(OpCode.LoadSuper);
        return null;
    }

    public Instruction? VisitSuperProxy(SuperProxy node)
    {
        // SuperProxy 在字节码模式中不应该直接访问
        // 它应该通过 super.method() 或 super.field 的形式使用
        throw new NotSupportedException("SuperProxy 不应该在字节码模式中直接访问");
    }
    public Instruction? VisitTaskClassLangValue(TaskClassLangValue node) => null;
    public Instruction? VisitTaskCompletionSourceLangValue(TaskCompletionSourceLangValue node) => null;
    public Instruction? VisitTaskFactoryClassLangValue(TaskFactoryClassLangValue node) => null;
    public Instruction? VisitTaskFactoryStaticMethodWrapper(TaskFactoryStaticMethodWrapper node) => null;
    public Instruction? VisitTaskLangValue(TaskLangValue node) => null;
    public Instruction? VisitTaskSchedulerClassLangValue(TaskSchedulerClassLangValue node) => null;
    public Instruction? VisitTaskSchedulerLangValue(TaskSchedulerLangValue node) => null;
    public Instruction? VisitTaskStaticMethodWrapper(TaskStaticMethodWrapper node) => null;
    public Instruction? VisitThreadClassLangValue(ThreadClassLangValue node) => null;
    public Instruction? VisitThreadLangValue(ThreadLangValue node) => null;
    public Instruction? VisitThreadStaticMethodWrapper(ThreadStaticMethodWrapper node) => null;
    public Instruction? VisitTypeLangValue(TypeLangValue node) => null;
    public Instruction? VisitAssertClassLangValue(AssertClassLangValue node) => null;
    public Instruction? VisitTestRunnerClassLangValue(TestRunnerClassLangValue node) => null;
    public Instruction? VisitMockLibClassLangValue(MockLibClassLangValue node) => null;
    public Instruction? VisitEnumLangValue(EnumLangValue node)
    {
        // 枚举值在字节码模式下加载其整数值
        Emit(OpCode.LoadConst, node.Value);
        return null;
    }
    public Instruction? VisitAssertStaticMethodWrapper(AssertStaticMethodWrapper node) => null;
    public Instruction? VisitMockObjectLangValue(MockObjectLangValue node) => null;
    public Instruction? VisitMockLibStaticMethodWrapper(MockLibStaticMethodWrapper node) => null;
    public Instruction? VisitTestRunnerStaticMethodWrapper(TestRunnerStaticMethodWrapper node) => null;
    public Instruction? VisitLockedVariableLangValue(LockedVariableLangValue node) => null;
    public Instruction? VisitInterpreterVisitor(InterpreterVisitor node) => null;

    /// <summary>
    /// 访问 FuncLangValue 节点
    /// </summary>
    public Instruction? VisitFuncLangValue(FuncLangValue node)
    {
        // 1. 生成唯一的 Lambda 名称
        string lambdaName = $"<lambda_{Guid.NewGuid():N}>";

        // 2. 提取参数信息
        var paramNames = node.Ids?.Select(id => id.IdName).ToList() ?? [];
        var paramTypes = node.Ids?.Select(id => id.AssumptionType ?? "").ToList() ?? [];

        var defaultValues = new List<object?>();
        int paramsIndex = -1;

        if (node.Ids != null)
        {
            for (int i = 0; i < node.Ids.Count; i++)
            {
                var param = node.Ids[i];
                if (param.IsParams) paramsIndex = i;

                if (param.DefaultValue != null)
                {
                    defaultValues.Add(EvaluateConstantExpression(param.DefaultValue));
                }
                else
                {
                    defaultValues.Add(null);
                }
            }
        }

        // 3. 分析捕获的变量
        var analyzer = new ClosureCaptureAnalyzer();
        var capturedVars = analyzer.AnalyzeCaptures(node.BlockStatement, paramNames);

        // DEBUG: 打印分析结果
        var debugOutput = new System.Text.StringBuilder();
        debugOutput.AppendLine($"[DEBUG] Lambda {lambdaName}:");
        debugOutput.AppendLine($"[DEBUG]   分析到的捕获变量: {string.Join(", ", capturedVars)}");
        debugOutput.AppendLine($"[DEBUG]   IsLocalVariable: {string.Join(", ", capturedVars.Where(v => _compiler.IsLocalVariable(v)))}");
        debugOutput.AppendLine($"[DEBUG]   IsGlobalVariable: {string.Join(", ", capturedVars.Where(v => _compiler.IsGlobalVariable(v)))}");
        debugOutput.AppendLine($"[DEBUG]   IsCapturedVariable: {string.Join(", ", capturedVars.Where(v => _compiler.IsCapturedVariable(v)))}");

        // 4. 过滤出实际存在的变量
        // 注意：对于嵌套 Lambda，内层 Lambda 可能需要捕获外层 Lambda 的捕获变量
        // 这些变量在编译时既不是局部变量也不是全局变量，但仍然需要捕获
        var actualCapturedVars = new List<string>();
        foreach (var varName in capturedVars)
        {
            // 检查变量是否存在：局部变量、全局变量、或当前函数的捕获变量
            // 如果都不是，也保留它（可能是外层函数的捕获变量，用于嵌套闭包）
            if (_compiler.IsLocalVariable(varName))
            {
                actualCapturedVars.Add(varName);
            }
            else if (_compiler.IsGlobalVariable(varName))
            {
                actualCapturedVars.Add(varName);
            }
            else if (_compiler.IsCapturedVariable(varName))
            {
                actualCapturedVars.Add(varName);
            }
            else
            {
                // 对于嵌套闭包，变量可能来自外层函数但还未被标记为捕获变量
                // 保留这些变量，让运行时从闭包环境中查找
                actualCapturedVars.Add(varName);
            }
        }

        debugOutput.AppendLine($"[DEBUG]   实际捕获的变量: {string.Join(", ", actualCapturedVars)}");
        System.IO.File.AppendAllText("/tmp/lambda_debug.txt", debugOutput.ToString());

        // 5. 提取返回类型
        string returnType = node.Id?.AssumptionType ?? "";

        // 6. 编译 Lambda 函数体，传递捕获的变量列表和返回类型
        _compiler.CompileFunction(lambdaName, paramNames, paramTypes, defaultValues, node.BlockStatement, paramsIndex, actualCapturedVars, returnType);

        // 7. 获取编译后的函数索引
        int funcIndex = _compiler.GetFunctionIndex(lambdaName);

        // 7. 如果有捕获的变量，生成 MakeClosure 指令；否则生成 MakeFunction 指令
        if (actualCapturedVars.Count > 0)
        {
            // 为每个捕获的变量加载其值到栈
            foreach (var varName in actualCapturedVars)
            {
                if (_compiler.IsLocalVariable(varName))
                {
                    int localIndex = _compiler.GetLocalIndex(varName);
                    Emit(OpCode.LoadLocal, localIndex);
                }
                else if (_compiler.IsCapturedVariable(varName))
                {
                    // 如果变量是当前函数的捕获变量，使用 LoadGlobal 从闭包环境加载
                    // （虚拟机会自动从闭包环境中查找）
                    Emit(OpCode.LoadGlobal, varName);
                }
                else if (_compiler.IsGlobalVariable(varName))
                {
                    Emit(OpCode.LoadGlobal, varName);
                }
                else
                {
                    // 对于嵌套闭包，变量可能来自外层函数的参数或外层闭包的捕获变量
                    // 使用 LoadGlobal 从闭包环境中加载（虚拟机会自动查找）
                    Emit(OpCode.LoadGlobal, varName);
                }
            }

            // 生成 MakeClosure 指令
            // 操作数: [funcIndex, capturedVarCount, varNames...]
            var operand = new object[] { funcIndex, actualCapturedVars.Count, actualCapturedVars.ToArray() };
            Emit(OpCode.MakeClosure, operand);
        }
        else
        {
            // 没有捕获变量，生成普通的 MakeFunction 指令
            Emit(OpCode.MakeFunction, funcIndex);
        }

        return null;
    }
}
