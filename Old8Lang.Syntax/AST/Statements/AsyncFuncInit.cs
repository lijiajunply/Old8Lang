using Old8Lang.AST.Expression;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 异步函数声明语句：承载纯语法模型，运行时值由 <c>AsyncFuncInitRuntimeExtensions</c> 按需创建。
/// </summary>
public partial class AsyncFuncInit(FunctionSyntax syntax, SourcePosition position = default)
    : OldStatement(position), IBlockDeclaration
{
    /// <summary>函数语法模型（函数名、参数、函数体、装饰器与文档注释）。</summary>
    public readonly FunctionSyntax Syntax = syntax;

    /// <summary>函数名与返回类型注解。</summary>
    public LangId? Id => Syntax.Id;

    /// <summary>参数列表。</summary>
    public List<LangId> Parameters => Syntax.Parameters;

    /// <summary>函数体。</summary>
    public BlockStatement Body => Syntax.Body;

    /// <summary>判断是否为 Lambda 表达式。</summary>
    public bool IsLambda => Syntax.Id is null;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitAsyncFuncInit(this);

    /// <summary>AST 子节点访问（无子节点）。</summary>
    public override OldStatement? this[int index] => null;

    /// <summary>AST 子节点数量。</summary>
    public override int Count => 0;
}
