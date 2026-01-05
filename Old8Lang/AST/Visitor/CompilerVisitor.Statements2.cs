using Old8Lang.AST.Statement;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// CompilerVisitor - 额外的 Statement 节点实现
/// </summary>
public partial class CompilerVisitor
{
    /// <summary>
    /// 访问 AsyncForInStatement 节点（编译器）
    /// </summary>
    public object? VisitAsyncForInStatement(AsyncForInStatement node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 AsyncFuncInit 节点（编译器）
    /// </summary>
    public object? VisitAsyncFuncInit(AsyncFuncInit node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 SwitchStatement 节点（编译器）
    /// </summary>
    public object? VisitSwitchStatement(SwitchStatement node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 TryStatement 节点（编译器）
    /// </summary>
    public object? VisitTryStatement(TryStatement node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 YieldStatement 节点（编译器）
    /// </summary>
    public object? VisitYieldStatement(YieldStatement node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 CaseStatement 节点（编译器）
    /// </summary>
    public object? VisitCaseStatement(CaseStatement node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 ClassInit 节点（编译器）
    /// </summary>
    public object? VisitClassInit(ClassInit node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 FuncInit 节点（编译器）
    /// </summary>
    public object? VisitFuncInit(FuncInit node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 ImportStatement 节点（编译器）
    /// </summary>
    public object? VisitImportStatement(ImportStatement node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 NativeStatement 节点（编译器）
    /// </summary>
    public object? VisitNativeStatement(NativeStatement node)
    {
        // 委托给节点的原方法
        node.GenerateIl(ilGenerator, local);
        return null;
    }
}
