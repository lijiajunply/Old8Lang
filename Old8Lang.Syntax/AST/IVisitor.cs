using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;

namespace Old8Lang.AST.Visitor;

/// <summary>可独立于执行后端使用的语法节点访问契约。</summary>
/// <typeparam name="TResult">访问结果类型。</typeparam>
public interface IVisitor<out TResult>
{
    /// <summary>访问标量字面量表达式。</summary>
    TResult VisitScalarLiteralExpression(ScalarLiteralExpression node);

    /// <summary>访问 await 表达式。</summary>
    TResult VisitAwaitExpression(AwaitExpression node);

    /// <summary>访问 throw 语句。</summary>
    TResult VisitThrowStatement(ThrowStatement node);

    /// <summary>访问函数调用表达式。</summary>
    TResult VisitFunctionCallExpression(FunctionCallExpression node);

    /// <summary>访问标识符节点。</summary>
    TResult VisitLangId(LangId node);

    /// <summary>访问 ThisExpression 节点。</summary>
    TResult VisitThisExpression(ThisExpression node);

    /// <summary>访问 SuperExpression 节点。</summary>
    TResult VisitSuperExpression(SuperExpression node);

    /// <summary>访问 TernaryExpression 节点。</summary>
    TResult VisitTernaryExpression(TernaryExpression node);
}
