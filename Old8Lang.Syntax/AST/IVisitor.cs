using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;

namespace Old8Lang.AST.Visitor;

/// <summary>可独立于执行后端使用的语法节点访问契约。</summary>
/// <typeparam name="TResult">访问结果类型。</typeparam>
public interface IVisitor<out TResult>
{
    /// <summary>访问标量字面量表达式。</summary>
    TResult VisitScalarLiteralExpression(ScalarLiteralExpression node);

    /// <summary>访问嵌套索引表达式。</summary>
    TResult VisitNestedIndexExpression(NestedIndexExpression node);

    /// <summary>访问嵌套切片表达式。</summary>
    TResult VisitNestedSliceExpression(NestedSliceExpression node);

    /// <summary>访问索引表达式。</summary>
    TResult VisitIndexExpression(IndexExpression node);

    /// <summary>访问切片表达式。</summary>
    TResult VisitSliceExpression(SliceExpression node);

    /// <summary>访问范围字面量表达式。</summary>
    TResult VisitRangeLiteralExpression(RangeLiteralExpression node);

    /// <summary>访问元组字面量表达式。</summary>
    TResult VisitTupleLiteralExpression(TupleLiteralExpression node);

    /// <summary>访问字典字面量表达式。</summary>
    TResult VisitDictionaryLiteralExpression(DictionaryLiteralExpression node);

    /// <summary>访问列表字面量表达式。</summary>
    TResult VisitListLiteralExpression(ListLiteralExpression node);

    /// <summary>访问数组字面量表达式。</summary>
    TResult VisitArrayLiteralExpression(ArrayLiteralExpression node);

    /// <summary>访问 await 表达式。</summary>
    TResult VisitAwaitExpression(AwaitExpression node);

    /// <summary>访问 throw 语句。</summary>
    TResult VisitThrowStatement(ThrowStatement node);

    /// <summary>访问函数调用表达式。</summary>
    TResult VisitFunctionCallExpression(FunctionCallExpression node);

    /// <summary>访问运算表达式。</summary>
    TResult VisitOperation(Operation node);

    /// <summary>访问标识符节点。</summary>
    TResult VisitLangId(LangId node);

    /// <summary>访问 ThisExpression 节点。</summary>
    TResult VisitThisExpression(ThisExpression node);

    /// <summary>访问 SuperExpression 节点。</summary>
    TResult VisitSuperExpression(SuperExpression node);

    /// <summary>访问 TernaryExpression 节点。</summary>
    TResult VisitTernaryExpression(TernaryExpression node);
}
