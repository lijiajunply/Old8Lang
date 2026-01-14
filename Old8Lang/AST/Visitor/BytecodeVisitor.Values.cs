using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Bytecode;

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
        // 生成两个元素的代码
        node.V1.Accept(this);
        node.V2.Accept(this);

        // 创建元组(作为2元素数组)
        Emit(OpCode.NewTuple, 2);
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
        // 访问基础索引访问 (例如 array[index1])
        node.BaseIndex.Accept(this);

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
        // 我们需要在栈上准备这些参数,然后调用辅助方法
        // 栈布局: start, end, includeStart, includeEnd

        Emit(OpCode.LoadConst, node.IncludeStart ? 1 : 0);
        Emit(OpCode.LoadConst, node.IncludeEnd ? 1 : 0);

        // 调用原生方法创建范围数组
        // 使用CallNative调用RangeLangValue.CreateRangeArray
        var methodName = "Old8Lang.AST.Expression.Intermediates.RangeLangValue::CreateRangeArray";
        Emit(OpCode.CallNative, new object[] { 4, methodName });

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
        // 策略: 创建一个对象数组,将所有表达式结果放入数组,然后调用string.Concat

        var expressionList = node.ExpressionList;

        if (expressionList.Count == 0)
        {
            // 空模板,返回空字符串
            Emit(OpCode.LoadConst, _compiler.ConstantPool.AddConstant(""));
            return null;
        }

        // 创建对象数组: NewArray指令
        Emit(OpCode.NewArray, expressionList.Count);

        // 遍历所有表达式,将结果存入数组
        for (int i = 0; i < expressionList.Count; i++)
        {
            var expr = expressionList[i];

            // 复制数组引用
            Emit(OpCode.Dup);

            // 加载索引
            Emit(OpCode.LoadConst, _compiler.ConstantPool.AddConstant(i));

            // 访问表达式,将结果压入栈
            expr.Accept(this);

            // 将值存入数组: SetIndex指令
            Emit(OpCode.SetIndex);
        }

        // 调用string.Concat(object[])方法
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
}
