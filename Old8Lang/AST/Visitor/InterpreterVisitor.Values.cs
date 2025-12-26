using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// InterpreterVisitor - Value 节点的 Visit 方法实现
/// </summary>
public partial class InterpreterVisitor
{
    /// <summary>
    /// 访问 IntLangValue 节点
    /// </summary>
    public LangValueType VisitIntLangValue(IntLangValue node)
    {
        // 值类型节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 DoubleLangValue 节点
    /// </summary>
    public LangValueType VisitDoubleLangValue(DoubleLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 StringLangValue 节点
    /// </summary>
    public LangValueType VisitStringLangValue(StringLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 BoolLangValue 节点
    /// </summary>
    public LangValueType VisitBoolLangValue(BoolLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 CharLangValue 节点
    /// </summary>
    public LangValueType VisitCharLangValue(CharLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 NullLangValue 节点
    /// </summary>
    public LangValueType VisitNullLangValue(NullLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 VoidLangValue 节点
    /// </summary>
    public LangValueType VisitVoidLangValue(VoidLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 ArrayLangValue 节点
    /// </summary>
    public LangValueType VisitArrayLangValue(ArrayLangValue node)
    {
        // 完整迁移自 ArrayLangValue.Run()
        // 执行数组中的表达式并返回自身
        for (var i = 0; i < node.Values.Count; i++)
            node.RunResult[i] = node.Values[i].Accept(this);
        return node;
    }

    /// <summary>
    /// 访问 ListLangValue 节点
    /// </summary>
    public LangValueType VisitListLangValue(ListLangValue node)
    {
        // 完整迁移自 ListLangValue.Run()
        // 只有当Values为空且Value中有表达式时才需要执行，且没有被手动清空过
        if (node.Values.Count == 0 && node.Value.Count > 0 && !IsListCleared(node))
        {
            foreach (var expr in node.Value)
                node.Values.Add(expr.Accept(this));
        }

        return node;
    }

    /// <summary>
    /// 检查列表是否被清空（通过反射访问私有字段）
    /// </summary>
    private static bool IsListCleared(ListLangValue list)
    {
        var field = typeof(ListLangValue).GetField("HasBeenCleared",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null && (bool)field.GetValue(list)!;
    }

    /// <summary>
    /// 访问 DictionaryLangValue 节点
    /// </summary>
    public LangValueType VisitDictionaryLangValue(DictionaryLangValue node)
    {
        // 完整迁移自 DictionaryLangValue.Run()
        // 清空之前的值，避免重复添加
        node.Value.Clear();

        foreach (var tuple in node.Tuples)
        {
            tuple.Accept(this);
            node.Value.Add(tuple.Value);
        }

        return node;
    }

    /// <summary>
    /// 访问 TupleLangValue 节点
    /// </summary>
    public LangValueType VisitTupleLangValue(TupleLangValue node)
    {
        // 完整迁移自 TupleLangValue.Run()
        // 运行第一个元素
        var item1Result = node.V1.Accept(this);

        // 运行第二个元素，处理空名称的特殊情况
        LangValueType item2Result;
        if (node.V2 is LangId item2Id && string.IsNullOrEmpty(item2Id.IdName))
        {
            // 如果第二个元素是空名称的LangId，直接使用NullLangValue，避免NameError
            item2Result = NullLangValue.Instance;
        }
        else
        {
            // 正常运行第二个元素
            item2Result = node.V2.Accept(this);
        }

        // 使用反射设置 Value 属性（因为它是私有 setter）
        var valueProperty = typeof(TupleLangValue).GetProperty("Value");
        valueProperty?.SetValue(node, (item1Result, item2Result));

        return node;
    }

    /// <summary>
    /// 访问 RangeLangValue 节点
    /// </summary>
    public LangValueType VisitRangeLangValue(RangeLangValue node)
    {
        // 完整迁移自 RangeLangValue.Run()
        // 范围表达式，创建一个整数数组
        var results = new List<LangValueType>();

        var startValue = node.Start?.Accept(this);
        var endValue = node.End?.Accept(this);

        if (startValue is not IntLangValue startIntValue || endValue is not IntLangValue endIntValue)
            throw new TypeError(node, "IntValue",
                $"RangeValue: start 或 end 不是 IntValue，实际得到了 {startValue?.GetType().Name} 和 {endValue?.GetType().Name}");

        // 根据包含规则调整起始值
        var startNum = startIntValue.Value;
        var endNum = endIntValue.Value;

        if (!node.IncludeStart)
            startNum++;
        if (!node.IncludeEnd)
            endNum--;

        // 检查范围是否有效
        if (startNum > endNum)
        {
            for (var i = startNum; i >= endNum; i--)
            {
                results.Add(new IntLangValue(i));
            }
        }
        else
        {
            for (var i = startNum; i <= endNum; i++)
            {
                results.Add(new IntLangValue(i));
            }
        }

        return new ArrayLangValue(results);
    }

    /// <summary>
    /// 访问 SliceLangValue 节点
    /// </summary>
    public LangValueType VisitSliceLangValue(SliceLangValue node)
    {
        // 完整迁移自 SliceLangValue.Run()
        // 切片表达式
        var value = node.Id.Accept(this);
        var start1 = node.Start?.Accept(this);
        var end1 = node.End?.Accept(this);
        var step1 = node.Step?.Accept(this);

        if (value is not ILangList list)
            throw new InvalidOperationError(node, $"类型 '{value.GetType().Name}' 不支持切片操作");

        var length = list.GetLength();
        var stepValue = step1?.GetValue<int>() ?? 1;

        // 如果步长为0，抛出错误
        if (stepValue == 0)
            throw new InvalidOperationError(node, "切片步长不能为0");

        // 处理负数步长
        int startValue, endValue;
        if (stepValue > 0)
        {
            // 正向切片
            startValue = start1?.GetValue<int>() ?? 0;
            endValue = end1?.GetValue<int>() ?? length;
        }
        else
        {
            // 反向切片
            startValue = start1?.GetValue<int>() ?? length - 1;
            endValue = end1?.GetValue<int>() ?? -1;
        }

        return list.Slice(startValue, endValue, stepValue);
    }

    /// <summary>
    /// 访问 Instance 节点
    /// </summary>
    public LangValueType VisitInstance(Instance node)
    {
        // 迁移自 Instance.Run()
        // Instance 表示类实例化，逻辑复杂，包含全局函数注册、Lock、Task等特殊处理
        return node.Run(_manager);
    }

    /// <summary>
    /// 访问 TypeLangValue 节点
    /// </summary>
    public LangValueType VisitTypeLangValue(TypeLangValue node)
    {
        // 迁移自 TypeLangValue.Run()
        // TypeLangValue 表示类型值，需要对表达式求值来确定类型
        return node.Run(_manager);
    }

    /// <summary>
    /// 访问 StringTemplateValue 节点
    /// </summary>
    public LangValueType VisitStringTemplateValue(StringTemplateValue node)
    {
        // 完整迁移自 StringTemplateValue.Run()
        // 字符串模板插值 - 将所有部分拼接成字符串
        var result = node.ExpressionList.Select(item => item.Accept(this)).Aggregate(string.Empty,
            (current, exprResult) => current + exprResult.ToDisplayString());

        return StringLangValue.Create(result);
    }

    /// <summary>
    /// 访问 ListComprehension 节点
    /// </summary>
    public LangValueType VisitListComprehension(ListComprehension node)
    {
        // 迁移自 ListComprehension.Run()
        // 列表推导式逻辑
        return node.Run(_manager);
    }

    /// <summary>
    /// 访问 NestedIndexAccess 节点
    /// </summary>
    public LangValueType VisitNestedIndexAccess(NestedIndexAccess node)
    {
        // 迁移自 NestedIndexAccess.Run()
        // 嵌套索引访问逻辑
        return node.Run(_manager);
    }

    /// <summary>
    /// 访问 NestedSliceAccess 节点
    /// </summary>
    public LangValueType VisitNestedSliceAccess(NestedSliceAccess node)
    {
        // 迁移自 NestedSliceAccess.Run()
        // 嵌套切片访问逻辑
        return node.Run(_manager);
    }

    /// <summary>
    /// 访问 MethodOverloadList 节点
    /// </summary>
    public LangValueType VisitMethodOverloadList(MethodOverloadList node)
    {
        // 迁移自 MethodOverloadList.Run()
        // 方法重载列表，通常在方法重载解析时使用
        return node.Run(_manager);
    }

    /// <summary>
    /// 访问 SuperProxy 节点
    /// </summary>
    public LangValueType VisitSuperProxy(SuperProxy node)
    {
        // 迁移自 SuperProxy.Run()
        // Super 代理，用于访问父类成员
        return node.Run(_manager);
    }

    /// <summary>
    /// 访问 AnyLangValue 节点
    /// </summary>
    public LangValueType VisitAnyLangValue(AnyLangValue node)
    {
        // 迁移自 AnyLangValue.Run()
        // 值类型节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 ErrorLangValue 节点
    /// </summary>
    public LangValueType VisitErrorLangValue(ErrorLangValue node)
    {
        // 迁移自 ErrorLangValue.Run()
        // 错误值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 LangListItem 节点
    /// </summary>
    public LangValueType VisitLangListItem(LangListItem node)
    {
        // 迁移自 LangListItem.Run()
        // 列表项节点
        return node.Run(_manager);
    }

    // ==================== 异步和生成器相关节点 ====================

    /// <summary>
    /// 访问 GeneratorLangValue 节点
    /// </summary>
    public LangValueType VisitGeneratorLangValue(GeneratorLangValue node)
    {
        // 迁移自 GeneratorLangValue.Run()
        // 生成器值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 AsyncGeneratorLangValue 节点
    /// </summary>
    public LangValueType VisitAsyncGeneratorLangValue(AsyncGeneratorLangValue node)
    {
        // 迁移自 AsyncGeneratorLangValue.Run()
        // 异步生成器值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 AsyncStreamLangValue 节点
    /// </summary>
    public LangValueType VisitAsyncStreamLangValue(AsyncStreamLangValue node)
    {
        // 迁移自 AsyncStreamLangValue.Run()
        // 异步流值节点在解释器模式下直接返回自身
        return node;
    }

    // ==================== CancellationToken 相关节点 ====================

    /// <summary>
    /// 访问 CancellationTokenLangValue 节点
    /// </summary>
    public LangValueType VisitCancellationTokenLangValue(CancellationTokenLangValue node)
    {
        // 迁移自 CancellationTokenLangValue.Run()
        // CancellationToken 值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 CancellationTokenSourceLangValue 节点
    /// </summary>
    public LangValueType VisitCancellationTokenSourceLangValue(CancellationTokenSourceLangValue node)
    {
        // 迁移自 CancellationTokenSourceLangValue.Run()
        // CancellationTokenSource 值节点在解释器模式下直接返回自身
        return node;
    }

    // ==================== Task 相关节点 ====================

    /// <summary>
    /// 访问 TaskLangValue 节点
    /// </summary>
    public LangValueType VisitTaskLangValue(TaskLangValue node)
    {
        // 迁移自 TaskLangValue.Run()
        // Task 值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 TaskClassLangValue 节点
    /// </summary>
    public LangValueType VisitTaskClassLangValue(TaskClassLangValue node)
    {
        // 迁移自 TaskClassLangValue.Run()
        // Task 类值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 TaskCompletionSourceLangValue 节点
    /// </summary>
    public LangValueType VisitTaskCompletionSourceLangValue(TaskCompletionSourceLangValue node)
    {
        // 迁移自 TaskCompletionSourceLangValue.Run()
        // TaskCompletionSource 值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 TaskFactoryClassLangValue 节点
    /// </summary>
    public LangValueType VisitTaskFactoryClassLangValue(TaskFactoryClassLangValue node)
    {
        // 迁移自 TaskFactoryClassLangValue.Run()
        // TaskFactory 类值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 TaskFactoryStaticMethodWrapper 节点
    /// </summary>
    public LangValueType VisitTaskFactoryStaticMethodWrapper(TaskFactoryStaticMethodWrapper node)
    {
        // 迁移自 TaskFactoryStaticMethodWrapper.Run()
        // TaskFactory 静态方法包装器节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 TaskSchedulerClassLangValue 节点
    /// </summary>
    public LangValueType VisitTaskSchedulerClassLangValue(TaskSchedulerClassLangValue node)
    {
        // 迁移自 TaskSchedulerClassLangValue.Run()
        // TaskScheduler 类值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 TaskSchedulerLangValue 节点
    /// </summary>
    public LangValueType VisitTaskSchedulerLangValue(TaskSchedulerLangValue node)
    {
        // 迁移自 TaskSchedulerLangValue.Run()
        // TaskScheduler 值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 TaskStaticMethodWrapper 节点
    /// </summary>
    public LangValueType VisitTaskStaticMethodWrapper(TaskStaticMethodWrapper node)
    {
        // 迁移自 TaskStaticMethodWrapper.Run()
        // Task 静态方法包装器节点在解释器模式下直接返回自身
        return node;
    }

    // ==================== Thread 相关节点 ====================

    /// <summary>
    /// 访问 ThreadLangValue 节点
    /// </summary>
    public LangValueType VisitThreadLangValue(ThreadLangValue node)
    {
        // 迁移自 ThreadLangValue.Run()
        // Thread 值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 ThreadClassLangValue 节点
    /// </summary>
    public LangValueType VisitThreadClassLangValue(ThreadClassLangValue node)
    {
        // 迁移自 ThreadClassLangValue.Run()
        // Thread 类值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 ThreadStaticMethodWrapper 节点
    /// </summary>
    public LangValueType VisitThreadStaticMethodWrapper(ThreadStaticMethodWrapper node)
    {
        // 迁移自 ThreadStaticMethodWrapper.Run()
        // Thread 静态方法包装器节点在解释器模式下直接返回自身
        return node;
    }

    // ==================== 特殊节点 ====================

    /// <summary>
    /// 访问 InterpreterVisitor 节点
    /// </summary>
    public LangValueType VisitInterpreterVisitor(InterpreterVisitor node)
    {
        // 注意: InterpreterVisitor 本身作为 AST 节点比较特殊
        // 这个方法通常不应该被调用，因为 Visitor 本身不是值类型
        // 如果被调用，抛出异常 (使用字符串消息而非节点,因为 InterpreterVisitor 不是 IOldLangTree)
        throw new NotImplementedException("InterpreterVisitor 不应该被作为值类型节点访问");
    }
}
