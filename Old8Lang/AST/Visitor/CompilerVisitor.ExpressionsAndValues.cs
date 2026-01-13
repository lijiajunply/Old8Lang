using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Linq;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// CompilerVisitor - Expression 和 Value 节点的简化实现
/// </summary>
public partial class CompilerVisitor
{
    // ===== Expression 节点实现 =====

    public object? VisitAsyncStreamExpression(AsyncStreamExpression node)
    {
        // 简化实现：加载表达式的IL值
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitAwaitExpression(AwaitExpression node)
    {
        // 简化实现：委托给节点的LoadIlValue方法
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitFunctionCallExpression(FunctionCallExpression node)
    {
        // 简化实现：委托给节点的LoadIlValue方法
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitOperation(Operation node)
    {
        // 简化实现：委托给节点的LoadIlValue方法
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitSuperExpression(SuperExpression node)
    {
        // 简化实现
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitTernaryExpression(TernaryExpression node)
    {
        // 简化实现：委托给节点的LoadIlValue方法
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    // ===== Value 节点实现 =====
    // Value节点在编译器模式下通常直接加载常量或调用LoadIlValue

    public object? VisitAnyLangValue(AnyLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitArrayLangValue(ArrayLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitAsyncGeneratorLangValue(AsyncGeneratorLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitAsyncStreamLangValue(AsyncStreamLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitCancellationTokenLangValue(CancellationTokenLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitCancellationTokenSourceLangValue(CancellationTokenSourceLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitDictionaryLangValue(DictionaryLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitErrorLangValue(ErrorLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitGeneratorLangValue(GeneratorLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitInstance(Instance node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitLangListItem(LangListItem node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitListComprehension(ListComprehension node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitListLangValue(ListLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitMethodOverloadList(MethodOverloadList node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitNestedIndexAccess(NestedIndexAccess node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitNestedSliceAccess(NestedSliceAccess node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitRangeLangValue(RangeLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitSliceLangValue(SliceLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitStringTemplateValue(StringTemplateValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitSuperProxy(SuperProxy node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitTaskClassLangValue(TaskClassLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitTaskCompletionSourceLangValue(TaskCompletionSourceLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitTaskFactoryClassLangValue(TaskFactoryClassLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitTaskFactoryStaticMethodWrapper(TaskFactoryStaticMethodWrapper node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitTaskLangValue(TaskLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitTaskSchedulerClassLangValue(TaskSchedulerClassLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitTaskSchedulerLangValue(TaskSchedulerLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitTaskStaticMethodWrapper(TaskStaticMethodWrapper node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitThreadClassLangValue(ThreadClassLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitThreadLangValue(ThreadLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitThreadStaticMethodWrapper(ThreadStaticMethodWrapper node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitTupleLangValue(TupleLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitTypeLangValue(TypeLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitGenericInstanceExpression(GenericInstanceExpression node)
    {
        // 泛型实例化在编译器模式下暂不支持
        // TODO: 实现泛型的编译器支持
        return null;
    }

    public object? VisitLinqExpression(LinqExpression node)
    {
        // 调用 LinqExpression 的 LoadIlValue 方法生成 IL 代码
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitAssertClassLangValue(AssertClassLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitAssertStaticMethodWrapper(AssertStaticMethodWrapper node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitMockLibClassLangValue(MockLibClassLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitMockObjectLangValue(MockObjectLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitMockLibStaticMethodWrapper(MockLibStaticMethodWrapper node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitTestRunnerClassLangValue(TestRunnerClassLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitTestRunnerStaticMethodWrapper(TestRunnerStaticMethodWrapper node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitLockedVariableLangValue(LockedVariableLangValue node)
    {
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    public object? VisitInterpreterVisitor(InterpreterVisitor node)
    {
        // InterpreterVisitor 不应该在编译器模式下访问
        throw new NotSupportedException("InterpreterVisitor 不应该在编译器模式下访问");
    }
}
