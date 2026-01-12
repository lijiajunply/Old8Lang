using Old8Lang.AST.Statement;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// TypeInferenceVisitor - Statement 节点类型推断实现
/// </summary>
public partial class TypeInferenceVisitor
{
    // 所有Statement节点返回void类型，因为语句不产生值
    public Type? VisitAsyncForInStatement(AsyncForInStatement node) => typeof(void);
    public Type? VisitAsyncFuncInit(AsyncFuncInit node) => typeof(void);
    public Type? VisitBlockStatement(BlockStatement node) => typeof(void);
    public Type? VisitBreakStatement(BreakStatement node) => typeof(void);
    public Type? VisitCaseStatement(CaseStatement node) => typeof(void);
    public Type? VisitClassInit(ClassInit node) => typeof(void);
    public Type? VisitContinueStatement(ContinueStatement node) => typeof(void);
    public Type? VisitForInStatement(ForInStatement node) => typeof(void);
    public Type? VisitForStatement(ForStatement node) => typeof(void);
    public Type? VisitFuncInit(FuncInit node) => typeof(void);
    public Type? VisitIfStatement(IfStatement node) => typeof(void);
    public Type? VisitImportStatement(ImportStatement node) => typeof(void);
    public Type? VisitNativeStatement(NativeStatement node) => typeof(void);
    public Type? VisitSetStatement(SetStatement node) => typeof(void);
    public Type? VisitSwitchStatement(SwitchStatement node) => typeof(void);
    public Type? VisitThrowStatement(ThrowStatement node) => typeof(void);
    public Type? VisitTryStatement(TryStatement node) => typeof(void);
    public Type? VisitWhileStatement(WhileStatement node) => typeof(void);
    public Type? VisitYieldStatement(YieldStatement node) => typeof(void);
    public Type? VisitDeferStatement(DeferStatement node) => typeof(void);
    public Type? VisitSelectStatement(SelectStatement node) => typeof(void);

    // ReturnStatement返回其表达式的类型
    public Type? VisitReturnStatement(ReturnStatement node)
    {
        return node.OutputType(local);
    }
}
