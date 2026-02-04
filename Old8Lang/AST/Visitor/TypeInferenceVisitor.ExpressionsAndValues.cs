using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Linq;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// TypeInferenceVisitor - Expression 和 Value 节点的类型推断实现
/// </summary>
public partial class TypeInferenceVisitor
{
    // ===== Expression 节点实现 =====

    public Type VisitAsyncStreamExpression(AsyncStreamExpression node)
    {
        return node.OutputType(local);
    }

    public Type VisitAwaitExpression(AwaitExpression node)
    {
        return node.OutputType(local);
    }

    public Type VisitFunctionCallExpression(FunctionCallExpression node)
    {
        return node.OutputType(local);
    }

    public Type VisitLangId(LangId node)
    {
        return node.OutputType(local);
    }

    public Type? VisitOperation(Operation node)
    {
        return node.OutputType(local);
    }

    public Type VisitSuperExpression(SuperExpression node)
    {
        return node.OutputType(local);
    }

    public Type VisitThisExpression(ThisExpression node)
    {
        return node.OutputType(local);
    }

    public Type? VisitTernaryExpression(TernaryExpression node)
    {
        return node.OutputType(local);
    }

    // ===== Value 节点实现 =====
    // 所有Value节点通过OutputType方法推断类型

    public Type? VisitAnyLangValue(AnyLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitArrayLangValue(ArrayLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitAsyncGeneratorLangValue(AsyncGeneratorLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitAsyncStreamLangValue(AsyncStreamLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitBoolLangValue(BoolLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitCancellationTokenLangValue(CancellationTokenLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitCancellationTokenSourceLangValue(CancellationTokenSourceLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitCharLangValue(CharLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitClassMemberId(ClassMemberId node)
    {
        return node.OutputType(local);
    }

    public Type VisitDictionaryLangValue(DictionaryLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitDoubleLangValue(DoubleLangValue node)
    {
        return node.OutputType(local);
    }

    public Type? VisitErrorLangValue(ErrorLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitFuncLangValue(FuncLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitGeneratorLangValue(GeneratorLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitInstance(Instance node)
    {
        return node.OutputType(local);
    }

    public Type VisitIntLangValue(IntLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitLangListItem(LangListItem node)
    {
        return node.OutputType(local);
    }

    public Type VisitListComprehension(ListComprehension node)
    {
        return node.OutputType(local);
    }

    public Type VisitListLangValue(ListLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitMethodOverloadList(MethodOverloadList node)
    {
        // MethodOverloadList 是方法重载列表，返回 Delegate 类型
        // 避免循环调用 OutputType -> Accept -> VisitMethodOverloadList -> OutputType
        return typeof(Delegate);
    }

    public Type VisitNestedIndexAccess(NestedIndexAccess node)
    {
        return node.OutputType(local);
    }

    public Type VisitNestedSliceAccess(NestedSliceAccess node)
    {
        return node.OutputType(local);
    }

    public Type VisitNullLangValue(NullLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitRangeLangValue(RangeLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitSliceLangValue(SliceLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitStringLangValue(StringLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitStringTemplateValue(StringTemplateValue node)
    {
        return node.OutputType(local);
    }

    public Type? VisitSuperProxy(SuperProxy node)
    {
        return node.OutputType(local);
    }

    public Type VisitTaskClassLangValue(TaskClassLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitTaskCompletionSourceLangValue(TaskCompletionSourceLangValue node)
    {
        return node.OutputType(local);
    }

    public Type? VisitTaskFactoryClassLangValue(TaskFactoryClassLangValue node)
    {
        return node.OutputType(local);
    }

    public Type? VisitTaskFactoryStaticMethodWrapper(TaskFactoryStaticMethodWrapper node)
    {
        return node.OutputType(local);
    }

    public Type VisitTaskLangValue(TaskLangValue node)
    {
        return node.OutputType(local);
    }

    public Type? VisitTaskSchedulerClassLangValue(TaskSchedulerClassLangValue node)
    {
        return node.OutputType(local);
    }

    public Type? VisitTaskSchedulerLangValue(TaskSchedulerLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitTaskStaticMethodWrapper(TaskStaticMethodWrapper node)
    {
        return node.OutputType(local);
    }

    public Type? VisitThreadClassLangValue(ThreadClassLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitThreadLangValue(ThreadLangValue node)
    {
        return node.OutputType(local);
    }

    public Type? VisitThreadStaticMethodWrapper(ThreadStaticMethodWrapper node)
    {
        return node.OutputType(local);
    }

    public Type VisitTupleLangValue(TupleLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitTypeLangValue(TypeLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitVoidLangValue(VoidLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitMatchExpression(MatchExpression node)
    {
        return node.OutputType(local);
    }

    public Type VisitEnumLangValue(EnumLangValue node)
    {
        // 枚举值的类型是 int（枚举的底层类型）
        return typeof(int);
    }

    public Type VisitGenericInstanceExpression(GenericInstanceExpression node)
    {
        return node.OutputType(local);
    }

    public Type VisitLinqExpression(LinqExpression node)
    {
        return node.OutputType(local);
    }

    public Type? VisitAssertClassLangValue(AssertClassLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitAssertStaticMethodWrapper(AssertStaticMethodWrapper node)
    {
        return node.OutputType(local);
    }

    public Type? VisitLockedVariableLangValue(LockedVariableLangValue node)
    {
        return node.OutputType(local);
    }

    public Type? VisitMockLibClassLangValue(MockLibClassLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitMockLibStaticMethodWrapper(MockLibStaticMethodWrapper node)
    {
        return node.OutputType(local);
    }

    public Type VisitMockObjectLangValue(MockObjectLangValue node)
    {
        return node.OutputType(local);
    }

    public Type? VisitTestRunnerClassLangValue(TestRunnerClassLangValue node)
    {
        return node.OutputType(local);
    }

    public Type VisitTestRunnerStaticMethodWrapper(TestRunnerStaticMethodWrapper node)
    {
        return node.OutputType(local);
    }
}
