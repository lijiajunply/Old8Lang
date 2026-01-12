using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
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
        // 生成参数代码（位置参数 + 命名参数）

        // 先生成位置参数
        foreach (var arg in node.Ids)
        {
            arg.Accept(this);
        }

        // TODO: 处理命名参数
        // 命名参数暂时不支持，需要在VM中添加命名参数支持

        string funcName = node.Id.IdName;
        int argCount = node.Ids.Count;

        // 检查是否是原生函数
        if (_compiler.IsNativeFunction(funcName))
        {
            Emit(OpCode.CallNative, new object[] { argCount, funcName });
        }
        else
        {
            Emit(OpCode.Call, new object[] { argCount, funcName });
        }

        return null;
    }
    public Instruction? VisitLangListItem(LangListItem node) => null;
    public Instruction? VisitListComprehension(ListComprehension node) => null;
    public Instruction? VisitMethodOverloadList(MethodOverloadList node) => null;
    public Instruction? VisitNestedIndexAccess(NestedIndexAccess node) => null;
    public Instruction? VisitNestedSliceAccess(NestedSliceAccess node) => null;
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
    public Instruction? VisitSliceLangValue(SliceLangValue node) => null;
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
    public Instruction? VisitSuperProxy(SuperProxy node) => null;
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
}
