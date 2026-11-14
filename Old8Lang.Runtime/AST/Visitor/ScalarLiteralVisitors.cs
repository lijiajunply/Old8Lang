using System.Runtime.CompilerServices;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Runtime;
using Old8Lang.Syntax;

namespace Old8Lang.AST.Visitor;

public partial class CompilerVisitor
{
    /// <summary>访问嵌套索引表达式。</summary>
    public object? VisitNestedIndexExpression(NestedIndexExpression node) => VisitNestedIndexAccess(LiteralCollectionRuntimeFactory.CreateNestedIndex(node));
    /// <summary>访问嵌套切片表达式。</summary>
    public object? VisitNestedSliceExpression(NestedSliceExpression node) => VisitNestedSliceAccess(LiteralCollectionRuntimeFactory.CreateNestedSlice(node));

    /// <summary>生成索引表达式的 IL。</summary>
    public object? VisitIndexExpression(IndexExpression node) => VisitLangListItem(LiteralCollectionRuntimeFactory.CreateIndex(node));

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
}

public partial class BytecodeVisitor
{
    /// <summary>访问嵌套索引表达式。</summary>
    public Instruction? VisitNestedIndexExpression(NestedIndexExpression node) => VisitNestedIndexAccess(LiteralCollectionRuntimeFactory.CreateNestedIndex(node));
    /// <summary>访问嵌套切片表达式。</summary>
    public Instruction? VisitNestedSliceExpression(NestedSliceExpression node) => VisitNestedSliceAccess(LiteralCollectionRuntimeFactory.CreateNestedSlice(node));

    /// <summary>生成索引表达式的字节码。</summary>
    public Instruction? VisitIndexExpression(IndexExpression node) => VisitLangListItem(LiteralCollectionRuntimeFactory.CreateIndex(node));

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
}

public partial class TypeInferenceVisitor
{
    /// <summary>访问嵌套索引表达式。</summary>
    public Type? VisitNestedIndexExpression(NestedIndexExpression node) => VisitNestedIndexAccess(LiteralCollectionRuntimeFactory.CreateNestedIndex(node));
    /// <summary>访问嵌套切片表达式。</summary>
    public Type? VisitNestedSliceExpression(NestedSliceExpression node) => VisitNestedSliceAccess(LiteralCollectionRuntimeFactory.CreateNestedSlice(node));

    /// <summary>推断索引表达式的静态类型。</summary>
    public Type? VisitIndexExpression(IndexExpression node) => VisitLangListItem(LiteralCollectionRuntimeFactory.CreateIndex(node));

    /// <summary>推断切片表达式的静态类型。</summary>
    public Type? VisitSliceExpression(SliceExpression node) =>
        VisitSliceLangValue(LiteralCollectionRuntimeFactory.CreateSlice(node));

    /// <summary>推断范围字面量的静态类型。</summary>
    public Type? VisitRangeLiteralExpression(RangeLiteralExpression node) => typeof(int[]);

    /// <summary>推断元组字面量的静态类型。</summary>
    public Type? VisitTupleLiteralExpression(TupleLiteralExpression node) =>
        VisitTupleLangValue(LiteralCollectionRuntimeFactory.CreateTuple(node));

    /// <summary>推断字典字面量的静态类型。</summary>
    public Type? VisitDictionaryLiteralExpression(DictionaryLiteralExpression node) => typeof(Dictionary<object, object>);

    /// <summary>推断列表字面量的静态类型。</summary>
    public Type? VisitListLiteralExpression(ListLiteralExpression node) => typeof(List<object>);

    /// <summary>推断数组字面量的静态类型。</summary>
    public Type? VisitArrayLiteralExpression(ArrayLiteralExpression node) => typeof(object[]);

    /// <summary>推断标量字面量的类型。</summary>
    public Type? VisitScalarLiteralExpression(ScalarLiteralExpression node) => node.Kind switch
    {
        LiteralKind.Int => typeof(int),
        LiteralKind.Double => typeof(double),
        LiteralKind.String => typeof(string),
        LiteralKind.Bool => typeof(bool),
        LiteralKind.Char => typeof(char),
        LiteralKind.Null => typeof(object),
        LiteralKind.Void => typeof(void),
        _ => throw new ArgumentOutOfRangeException(nameof(node))
    };
}
