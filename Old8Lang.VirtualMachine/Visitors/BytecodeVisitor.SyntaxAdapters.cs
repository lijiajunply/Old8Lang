using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Runtime;

namespace Old8Lang.AST.Visitor;

// 字节码/VM 后端专属的语法节点适配：标量/集合字面量、简单调用与 lambda 表达式。
public partial class BytecodeVisitor
{
    /// <summary>访问嵌套索引表达式。</summary>
    public Instruction? VisitNestedIndexExpression(NestedIndexExpression node) =>
        VisitNestedIndexAccess(LiteralCollectionRuntimeFactory.CreateNestedIndex(node));

    /// <summary>访问嵌套切片表达式。</summary>
    public Instruction? VisitNestedSliceExpression(NestedSliceExpression node) =>
        VisitNestedSliceAccess(LiteralCollectionRuntimeFactory.CreateNestedSlice(node));

    /// <summary>生成索引表达式的字节码。</summary>
    public Instruction? VisitIndexExpression(IndexExpression node) =>
        VisitLangListItem(LiteralCollectionRuntimeFactory.CreateIndex(node));

    /// <summary>生成切片表达式的字节码。</summary>
    public Instruction? VisitSliceExpression(SliceExpression node) =>
        VisitSliceLangValue(LiteralCollectionRuntimeFactory.CreateSlice(node));

    /// <summary>生成范围字面量的字节码。</summary>
    public Instruction? VisitRangeLiteralExpression(RangeLiteralExpression node) =>
        VisitRangeLangValue(LiteralCollectionRuntimeFactory.CreateRange(node));

    /// <summary>生成元组字面量的字节码。</summary>
    public Instruction? VisitTupleLiteralExpression(TupleLiteralExpression node) =>
        VisitTupleLangValue(LiteralCollectionRuntimeFactory.CreateTuple(node));

    /// <summary>生成字典字面量的字节码。</summary>
    public Instruction? VisitDictionaryLiteralExpression(DictionaryLiteralExpression node) =>
        VisitDictionaryLangValue(LiteralCollectionRuntimeFactory.CreateDictionary(node));

    /// <summary>生成列表字面量的字节码。</summary>
    public Instruction? VisitListLiteralExpression(ListLiteralExpression node) =>
        VisitListLangValue(new ListLangValue(node.Elements.ToList(), position: node.Position));

    /// <summary>生成数组字面量的字节码。</summary>
    public Instruction? VisitArrayLiteralExpression(ArrayLiteralExpression node) =>
        VisitArrayLangValue(new ArrayLangValue(node.Elements, position: node.Position));

    /// <summary>生成标量字面量的字节码。</summary>
    public Instruction? VisitScalarLiteralExpression(ScalarLiteralExpression node) =>
        ScalarLiteralRuntimeValueFactory.Create(node).Accept(this);

    /// <summary>访问简单函数调用。</summary>
    public Instruction? VisitCallExpression(CallExpression node) => node.ToInstance().Accept(this);

    /// <summary>访问 lambda 表达式。</summary>
    public Instruction? VisitLambdaExpression(LambdaExpression node) =>
        LambdaExpressionRuntimeFactory.Create(node).Accept(this);
}
