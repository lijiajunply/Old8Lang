using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Runtime;

namespace Old8Lang.AST.Visitor;

// IL 后端专属的语法节点适配：标量/集合字面量、简单调用与 lambda 表达式。
public partial class CompilerVisitor
{
    /// <summary>访问嵌套索引表达式。</summary>
    public object? VisitNestedIndexExpression(NestedIndexExpression node) =>
        VisitNestedIndexAccess(LiteralCollectionRuntimeFactory.CreateNestedIndex(node));

    /// <summary>访问嵌套切片表达式。</summary>
    public object? VisitNestedSliceExpression(NestedSliceExpression node) =>
        VisitNestedSliceAccess(LiteralCollectionRuntimeFactory.CreateNestedSlice(node));

    /// <summary>生成索引表达式的 IL。</summary>
    public object? VisitIndexExpression(IndexExpression node) =>
        VisitLangListItem(LiteralCollectionRuntimeFactory.CreateIndex(node));

    /// <summary>生成切片表达式的 IL。</summary>
    public object? VisitSliceExpression(SliceExpression node) =>
        VisitSliceLangValue(LiteralCollectionRuntimeFactory.CreateSlice(node));

    /// <summary>生成范围字面量的 IL。</summary>
    public object? VisitRangeLiteralExpression(RangeLiteralExpression node) =>
        VisitRangeLangValue(LiteralCollectionRuntimeFactory.CreateRange(node));

    /// <summary>生成元组字面量的 IL。</summary>
    public object? VisitTupleLiteralExpression(TupleLiteralExpression node) =>
        VisitTupleLangValue(LiteralCollectionRuntimeFactory.CreateTuple(node));

    /// <summary>生成字典字面量的 IL。</summary>
    public object? VisitDictionaryLiteralExpression(DictionaryLiteralExpression node) =>
        VisitDictionaryLangValue(LiteralCollectionRuntimeFactory.CreateDictionary(node));

    /// <summary>生成列表字面量的 IL。</summary>
    public object? VisitListLiteralExpression(ListLiteralExpression node) =>
        VisitListLangValue(new ListLangValue(node.Elements.ToList(), position: node.Position));

    /// <summary>生成数组字面量的 IL。</summary>
    public object? VisitArrayLiteralExpression(ArrayLiteralExpression node) =>
        VisitArrayLangValue(new ArrayLangValue(node.Elements, position: node.Position));

    /// <summary>生成标量字面量的 IL。</summary>
    public object? VisitScalarLiteralExpression(ScalarLiteralExpression node) =>
        ScalarLiteralRuntimeValueFactory.Create(node).Accept(this);

    /// <summary>访问简单函数调用。</summary>
    public object? VisitCallExpression(CallExpression node) => node.ToInstance().Accept(this);

    /// <summary>访问 lambda 表达式。</summary>
    public object? VisitLambdaExpression(LambdaExpression node) =>
        LambdaExpressionRuntimeFactory.Create(node).Accept(this);
}
