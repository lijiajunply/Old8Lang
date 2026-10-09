using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;
using Old8Lang.Error;
using Old8Lang.Bytecode.Closures;
using ClassMetadata = Old8Lang.Bytecode.Metadata.ClassMetadata;

namespace Old8Lang.AST.Visitor;

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
    // 说明：这些节点在字节码模式下没有对应实现。此处必须显式报错而不是静默返回 null——
    // 静默返回会让表达式不产出任何字节码，栈随之失衡，最终在无关指令处抛出
    // "Stack empty" 之类的错误，报出的原因和位置都与真实问题无关。

    public Instruction? VisitAnyLangValue(AnyLangValue node) =>
        throw new VmUnsupportedError(node, "类实例值参与表达式求值");

    public Instruction? VisitAsyncGeneratorLangValue(AsyncGeneratorLangValue node) =>
        throw new VmUnsupportedError(node, "异步生成器值");

    public Instruction? VisitAsyncStreamLangValue(AsyncStreamLangValue node) =>
        throw new VmUnsupportedError(node, "异步流值");

    public Instruction? VisitCancellationTokenLangValue(CancellationTokenLangValue node) =>
        throw new VmUnsupportedError(node, "CancellationToken");

    public Instruction? VisitCancellationTokenSourceLangValue(CancellationTokenSourceLangValue node) =>
        throw new VmUnsupportedError(node, "CancellationTokenSource");

    public Instruction? VisitErrorLangValue(ErrorLangValue node) =>
        throw new VmUnsupportedError(node, "错误值（ErrorValue）");

    public Instruction? VisitGeneratorLangValue(GeneratorLangValue node) =>
        throw new VmUnsupportedError(node, "生成器值");
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


                // 实例已经留在栈上（NewObject 创建 + Dup 保留引用），
                // 构造函数自身的返回值（无返回值时是一个 VoidLangValue 占位）必须丢弃
                Emit(OpCode.Pop);
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
                int funcIndex = _compiler.GetFunctionIndex(funcName);
                bool hasFunctionIndex = funcIndex >= 0;
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
                        var namedArgNames = node.NamedArgs.Select(na => na.Name).ToArray();
                        if (hasFunctionIndex)
                        {
                            Emit(OpCode.Call, new object[] { positionalCount, namedCount, funcName, namedArgNames, funcIndex });
                        }
                        else
                        {
                            Emit(OpCode.Call, new object[] { positionalCount, namedCount, funcName, namedArgNames });
                        }
                    }
                    else
                    {
                        if (hasFunctionIndex)
                        {
                            Emit(OpCode.Call, new object[] { positionalCount, funcName, funcIndex });
                        }
                        else
                        {
                            Emit(OpCode.Call, new object[] { positionalCount, funcName });
                        }
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
                        if (hasFunctionIndex)
                        {
                            Emit(OpCode.CallAsync, new object[] { positionalCount, funcName, funcIndex });
                        }
                        else
                        {
                            Emit(OpCode.CallAsync, new object[] { positionalCount, funcName });
                        }
                    }
                }
                else
                {
                    if (namedCount > 0)
                    {
                        var namedArgNames = node.NamedArgs.Select(na => na.Name).ToArray();
                        if (hasFunctionIndex)
                        {
                            Emit(OpCode.Call, new object[] { positionalCount, namedCount, funcName, namedArgNames, funcIndex });
                        }
                        else
                        {
                            Emit(OpCode.Call, new object[] { positionalCount, namedCount, funcName, namedArgNames });
                        }
                    }
                    else
                    {
                        if (hasFunctionIndex)
                        {
                            Emit(OpCode.Call, new object[] { positionalCount, funcName, funcIndex });
                        }
                        else
                        {
                            Emit(OpCode.Call, new object[] { positionalCount, funcName });
                        }
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
    /// <summary>
    /// 生成列表推导式的字节码
    /// </summary>
    /// <remarks>
    /// 节点结构：最外层节点持有元素表达式、首个 for 子句及其 if 条件，
    /// 后续 for 子句以链式结构挂在 NestedLoops 上（每层最多一个）。
    /// 生成的字节码等价于：
    /// <code>
    /// result = []
    /// for v1 in iterable1:            # 逐层嵌套
    ///     if cond1:
    ///         for v2 in iterable2:
    ///             if cond2:
    ///                 result.Add(expr)
    /// </code>
    /// 解释器会在最内层重新检查外层条件，对无副作用的条件表达式而言两种做法结果一致，
    /// 这里按层级就地检查，避免重复求值。
    /// </remarks>
    public Instruction? VisitListComprehension(ListComprehension node)
    {
        // 结果列表局部变量
        int resultLocalIndex = _compiler.AllocateLocal($"<comprehension_result_{GetCurrentPosition()}>");

        Emit(OpCode.NewList, 0);
        Emit(OpCode.StoreLocal, resultLocalIndex);

        EmitComprehensionLevel(node, resultLocalIndex, 0);

        // 推导式整体的值就是收集到的列表
        Emit(OpCode.LoadLocal, resultLocalIndex);

        _compiler.FreeLocal(resultLocalIndex);

        return null;
    }

    /// <summary>
    /// 生成列表推导式中一层 for 子句的字节码
    /// </summary>
    /// <param name="level">当前层节点</param>
    /// <param name="resultLocalIndex">收集结果的列表所在局部变量下标</param>
    /// <param name="depth">嵌套深度，仅用于生成唯一的临时变量名</param>
    private void EmitComprehensionLevel(ListComprehension level, int resultLocalIndex, int depth)
    {
        int iteratorLocalIndex = _compiler.AllocateLocal($"<comprehension_iterator_{GetCurrentPosition()}_{depth}>");

        // 求值可迭代对象并取出迭代器
        level.Iterable.Accept(this);
        Emit(OpCode.GetIterator);
        Emit(OpCode.StoreLocal, iteratorLocalIndex);

        int loopStart = GetCurrentPosition();

        // 迭代器在循环开始时压栈，跳出循环时弹出，与 for-in 的栈约定保持一致
        Emit(OpCode.LoadLocal, iteratorLocalIndex);
        Emit(OpCode.IteratorMoveNext);
        int jumpIfNoMore = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1);

        // IteratorCurrent / IteratorMoveNext 都只做 Peek，取完当前元素后需要手动弹出迭代器
        Emit(OpCode.IteratorCurrent);

        var varName = level.Variable.IdName;
        if (_compiler.IsLocalVariable(varName))
        {
            Emit(OpCode.StoreLocal, _compiler.GetLocalIndex(varName));
        }
        else
        {
            Emit(OpCode.StoreLocal, _compiler.DeclareLocalVariable(varName));
        }

        Emit(OpCode.Pop);

        // 条件筛选
        int jumpIfConditionFalse = -1;
        if (level.Condition is not null)
        {
            level.Condition.Accept(this);
            jumpIfConditionFalse = GetCurrentPosition();
            Emit(OpCode.JumpIfFalse, -1);
        }

        var nestedLoops = level.NestedLoops;
        if (nestedLoops is { Count: > 0 })
        {
            // 还有后续 for 子句，继续向下嵌套
            foreach (var nested in nestedLoops)
            {
                EmitComprehensionLevel(nested, resultLocalIndex, depth + 1);
            }
        }
        else
        {
            // 最内层：求值元素表达式并追加到结果列表
            Emit(OpCode.LoadLocal, resultLocalIndex);
            level.Expression.Accept(this);
            Emit(OpCode.CallNative, new object[] { 2, "__list_append" });
        }

        // 本轮结束：条件不满足时直接跳到循环末尾
        int continueTarget = GetCurrentPosition();
        if (jumpIfConditionFalse >= 0)
        {
            PatchJump(jumpIfConditionFalse, continueTarget);
        }

        Emit(OpCode.Jump, loopStart);

        int loopEnd = GetCurrentPosition();
        PatchJump(jumpIfNoMore, loopEnd);

        // 跳出循环时栈上还留着迭代器
        Emit(OpCode.Pop);

        _compiler.FreeLocal(iteratorLocalIndex);
    }
    /// <summary>
    /// 方法重载列表是编译期的元数据集合，本身不是可求值的表达式，因此不产出字节码。
    /// 这里返回 null 是设计如此（与 CompilerVisitor 的处理一致），并非未实现的桩。
    /// </summary>
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
        // 注意：LoadConst 的操作数是**常量池下标**，不是字面值。
        // 这里曾经直接写 Emit(LoadConst, 0/1)，等于假定常量池槽 0 放着 0、槽 1 放着 1。
        // 只要此前有别的常量占用了这两个槽（典型：extern 块先放入库名与函数名，
        // 槽 1 变成函数名字符串），区间表达式就会取到错误的操作数 ——
        // 例如 `extern "..." { func abs(...) }` 之后的 `for i in [1~3]` 会把
        // 布尔标志读成字符串 "abs"，报 “String 'abs' was not recognized as a valid Boolean”。
        // 因此一律先入池再按下标加载。

        // 访问start表达式
        if (node.Start != null)
        {
            node.Start.Accept(this);
        }
        else
        {
            Emit(OpCode.LoadConst, _compiler.ConstantPool.AddConstant(0)); // 默认起始值为0
        }

        // 访问end表达式
        if (node.End != null)
        {
            node.End.Accept(this);
        }
        else
        {
            Emit(OpCode.LoadConst, _compiler.ConstantPool.AddConstant(0)); // 默认结束值为0
        }

        // 加载includeStart和includeEnd标志
        // 栈布局(从下到上): start, end, includeStart, includeEnd
        Emit(node.IncludeStart ? OpCode.LoadTrue : OpCode.LoadFalse);
        Emit(node.IncludeEnd ? OpCode.LoadTrue : OpCode.LoadFalse);

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

    public Instruction? VisitThisExpression(ThisExpression node)
    {
        // 加载 this 引用
        // LoadThis 指令会将当前实例压栈
        Emit(OpCode.LoadThis);
        return null;
    }

    public Instruction VisitSuperProxy(SuperProxy node)
    {
        // SuperProxy 在字节码模式中不应该直接访问
        // 它应该通过 super.method() 或 super.field 的形式使用
        throw new NotSupportedException("SuperProxy 不应该在字节码模式中直接访问");
    }
    // 以下静态类在解释器模式下由 LangInterpreter 注册为全局对象，字节码模式尚未提供对应实现，
    // 详见 Docs/MODE_SUPPORT.md 的支持矩阵。这里显式报错以指明真实原因。

    public Instruction? VisitTaskClassLangValue(TaskClassLangValue node) =>
        throw new VmUnsupportedError(node, "Task 静态 API（Task.Delay / Task.WhenAll / Task.WhenAny）");

    public Instruction? VisitTaskCompletionSourceLangValue(TaskCompletionSourceLangValue node) =>
        throw new VmUnsupportedError(node, "TaskCompletionSource");

    public Instruction? VisitTaskFactoryClassLangValue(TaskFactoryClassLangValue node) =>
        throw new VmUnsupportedError(node, "TaskFactory");

    public Instruction? VisitTaskFactoryStaticMethodWrapper(TaskFactoryStaticMethodWrapper node) =>
        throw new VmUnsupportedError(node, "TaskFactory 静态方法");

    public Instruction? VisitTaskLangValue(TaskLangValue node) =>
        throw new VmUnsupportedError(node, "Task 实例");

    public Instruction? VisitTaskSchedulerClassLangValue(TaskSchedulerClassLangValue node) =>
        throw new VmUnsupportedError(node, "TaskScheduler");

    public Instruction? VisitTaskSchedulerLangValue(TaskSchedulerLangValue node) =>
        throw new VmUnsupportedError(node, "TaskScheduler 实例");

    public Instruction? VisitTaskStaticMethodWrapper(TaskStaticMethodWrapper node) =>
        throw new VmUnsupportedError(node, "Task 静态方法");

    public Instruction? VisitThreadClassLangValue(ThreadClassLangValue node) =>
        throw new VmUnsupportedError(node, "Thread 静态 API（Thread.Sleep / Thread.CurrentThread）");

    public Instruction? VisitThreadLangValue(ThreadLangValue node) =>
        throw new VmUnsupportedError(node, "Thread 实例");

    public Instruction? VisitThreadStaticMethodWrapper(ThreadStaticMethodWrapper node) =>
        throw new VmUnsupportedError(node, "Thread 静态方法");

    public Instruction? VisitTypeLangValue(TypeLangValue node) =>
        throw new VmUnsupportedError(node, "Type 类型对象");

    public Instruction? VisitAssertClassLangValue(AssertClassLangValue node) =>
        throw new VmUnsupportedError(node, "Assert 断言 API（Assert.Equal 等）");

    public Instruction? VisitTestRunnerClassLangValue(TestRunnerClassLangValue node) =>
        throw new VmUnsupportedError(node, "TestRunner API");

    public Instruction? VisitMockLibClassLangValue(MockLibClassLangValue node) =>
        throw new VmUnsupportedError(node, "Mock API");
    public Instruction? VisitEnumLangValue(EnumLangValue node)
    {
        // 枚举值在字节码模式下加载其整数值
        Emit(OpCode.LoadConst, node.Value);
        return null;
    }
    public Instruction? VisitAssertStaticMethodWrapper(AssertStaticMethodWrapper node) =>
        throw new VmUnsupportedError(node, "Assert 静态方法");

    public Instruction? VisitMockObjectLangValue(MockObjectLangValue node) =>
        throw new VmUnsupportedError(node, "Mock 对象");

    public Instruction? VisitMockLibStaticMethodWrapper(MockLibStaticMethodWrapper node) =>
        throw new VmUnsupportedError(node, "Mock 静态方法");

    public Instruction? VisitTestRunnerStaticMethodWrapper(TestRunnerStaticMethodWrapper node) =>
        throw new VmUnsupportedError(node, "TestRunner 静态方法");

    public Instruction? VisitLockedVariableLangValue(LockedVariableLangValue node) =>
        throw new VmUnsupportedError(node, "锁变量（lock）");

    /// <summary>
    /// InterpreterVisitor 是解释器的访问者对象，不属于可求值的 AST 节点，
    /// 出现在表达式位置说明代码结构异常（与 CompilerVisitor 的处理保持一致）
    /// </summary>
    public Instruction? VisitInterpreterVisitor(InterpreterVisitor node) =>
        throw new NotSupportedException("虚拟机模式下不应访问 InterpreterVisitor 对象");

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

        // 3. 分析捕获的变量，并拦截无法支持的写回（全局变量计入捕获，沿用既有行为）
        var actualCapturedVars = AnalyzeClosureCaptures(node.BlockStatement, paramNames);

        // 4. 提取返回类型
        string returnType = node.Id?.AssumptionType ?? "";

        // 5. 编译 Lambda 函数体，传递捕获的变量列表和返回类型
        _compiler.CompileFunction(lambdaName, paramNames, paramTypes, defaultValues, node.BlockStatement, paramsIndex, actualCapturedVars, returnType);

        // 6. 获取编译后的函数索引：有捕获则生成 MakeClosure，否则生成 MakeFunction
        int funcIndex = _compiler.GetFunctionIndex(lambdaName);
        EmitClosureOrFunction(funcIndex, actualCapturedVars);

        return null;
    }

    /// <summary>
    /// 分析函数体（lambda 或嵌套具名函数）对外层变量的捕获。
    /// </summary>
    /// <param name="body">函数体</param>
    /// <param name="paramNames">形参名，形参不算捕获</param>
    /// <remarks>
    /// 捕获项只取「外层的局部变量或已被外层闭包捕获的变量」。全局变量不进捕获列表：
    /// 运行期按名直接查全局表，既能读到最新值，也不会把每个引用全局的函数都变成闭包。
    /// </remarks>
    private List<string> AnalyzeClosureCaptures(
        BlockStatement body, List<string> paramNames)
    {
        var analyzer = new ClosureCaptureAnalyzer();
        var capturedVars = analyzer.AnalyzeCaptures(body, paramNames);

        var candidates = new List<string>(capturedVars);

        // 只读引用只是捕获的一部分。闭包**只写**某个外层局部变量（不读它）时，
        // AnalyzeCaptures 不会把该名字算作自由变量；若漏掉它，闭包内会另建一个同名局部
        // 变量，静默遮蔽外层那个，写回悄悄地不生效。因此「闭包内赋值 + 恰好是外层变量」
        // 的名字也必须捕获。
        foreach (var assignedName in analyzer.AssignedVariables)
        {
            if (!candidates.Contains(assignedName))
            {
                candidates.Add(assignedName);
            }
        }

        // 本闭包体内的嵌套闭包（例如 lambda 里的 func）需要的名字也要算进来：
        // 那些名字只出现在嵌套具名函数的函数体里，AnalyzeCaptures 不会把它算作本层的
        // 自由变量，但只有本层先捕获它，嵌套闭包才能沿父环境链取到。
        // 本层自己声明的局部变量由本层直接提供，不算捕获。
        var ownLocals = new HashSet<string>(analyzer.AssignedVariables, StringComparer.Ordinal);
        foreach (var neededName in analyzer.NamesNeededByNestedClosures)
        {
            if (!ownLocals.Contains(neededName) && !candidates.Contains(neededName))
            {
                candidates.Add(neededName);
            }
        }

        var actualCapturedVars = new List<string>();
        foreach (var varName in candidates)
        {
            if (_compiler.IsLocalVariable(varName) ||
                _compiler.IsCapturedVariable(varName))
            {
                actualCapturedVars.Add(varName);
            }
        }

        return actualCapturedVars;
    }

    /// <summary>
    /// 发出「构造函数值」的指令，lambda 与嵌套具名函数共用。
    /// </summary>
    /// <remarks>
    /// 有捕获变量时发 MakeClosure：外层局部变量会就地装箱成共享单元，闭包与外层此后
    /// 共用同一个盒子（双向可见）；否则发 MakeFunction（裸函数元数据，调用时
    /// frame.ClosureEnvironment 为 null）。
    /// 操作数编码: [funcIndex, capturedVarCount, kind0, operand0, name0, kind1, operand1, name1, ...]
    ///   kind 0 = 外层局部变量（operand 是该局部变量的下标）
    ///   kind 1 = 外层闭包已捕获的变量（运行期沿父环境链查找）
    ///   kind 2 = 全局变量（运行期查全局表）
    /// </remarks>
    private void EmitClosureOrFunction(int funcIndex, List<string> capturedVars)
    {
        if (capturedVars.Count == 0)
        {
            Emit(OpCode.MakeFunction, funcIndex);
            return;
        }

        var operand = new object[2 + capturedVars.Count * 3];
        operand[0] = funcIndex;
        operand[1] = capturedVars.Count;

        for (var i = 0; i < capturedVars.Count; i++)
        {
            var varName = capturedVars[i];
            var baseIndex = 2 + i * 3;
            if (_compiler.IsLocalVariable(varName))
            {
                operand[baseIndex] = 0;
                operand[baseIndex + 1] = _compiler.GetLocalIndex(varName);
                operand[baseIndex + 2] = varName;
            }
            else if (_compiler.IsCapturedVariable(varName))
            {
                operand[baseIndex] = 1;
                operand[baseIndex + 1] = -1;
                operand[baseIndex + 2] = varName;
            }
            else
            {
                operand[baseIndex] = 2;
                operand[baseIndex + 1] = -1;
                operand[baseIndex + 2] = varName;
            }
        }

        Emit(OpCode.MakeClosure, operand);
    }
}
