using System.Runtime.CompilerServices;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Runtime;
using Old8Lang.Syntax;

namespace Old8Lang.AST.Visitor;

public partial class InterpreterVisitor
{
    /// <summary>访问嵌套索引表达式。</summary>
    public LangValueType VisitNestedIndexExpression(NestedIndexExpression node) => VisitNestedIndexAccess(LiteralCollectionRuntimeFactory.CreateNestedIndex(node));
    /// <summary>访问嵌套切片表达式。</summary>
    public LangValueType VisitNestedSliceExpression(NestedSliceExpression node) => VisitNestedSliceAccess(LiteralCollectionRuntimeFactory.CreateNestedSlice(node));

    /// <summary>求值索引表达式。</summary>
    public LangValueType VisitIndexExpression(IndexExpression node) => VisitLangListItem(LiteralCollectionRuntimeFactory.CreateIndex(node));

    /// <summary>求值切片表达式。</summary>
    public LangValueType VisitSliceExpression(SliceExpression node) =>
        VisitSliceLangValue(LiteralCollectionRuntimeFactory.CreateSlice(node));

    /// <summary>求值范围字面量。</summary>
    public LangValueType VisitRangeLiteralExpression(RangeLiteralExpression node) =>
        VisitRangeLangValue(LiteralCollectionRuntimeFactory.CreateRange(node));

    // 旧 AST 在解析时创建值，同一节点重复求值返回同一实例；弱引用缓存保留这一语义。
    private static readonly ConditionalWeakTable<ScalarLiteralExpression, LangValueType> LiteralValues = new();
    private static readonly ConditionalWeakTable<ArrayLiteralExpression, ArrayLangValue> ArrayLiteralValues = new();
    private static readonly ConditionalWeakTable<ListLiteralExpression, ListLangValue> ListLiteralValues = new();
    private static readonly ConditionalWeakTable<DictionaryLiteralExpression, DictionaryLangValue> DictionaryLiteralValues = new();
    private static readonly ConditionalWeakTable<TupleLiteralExpression, TupleLangValue> TupleLiteralValues = new();

    /// <summary>求值元组并保持同一节点的既有对象身份。</summary>
    public LangValueType VisitTupleLiteralExpression(TupleLiteralExpression node) =>
        VisitTupleLangValue(TupleLiteralValues.GetValue(node, LiteralCollectionRuntimeFactory.CreateTuple));

    /// <summary>按原顺序求值字典条目并保持同一节点的既有对象身份。</summary>
    public LangValueType VisitDictionaryLiteralExpression(DictionaryLiteralExpression node) =>
        VisitDictionaryLangValue(DictionaryLiteralValues.GetValue(node, LiteralCollectionRuntimeFactory.CreateDictionary));

    /// <summary>求值列表字面量并保持同一节点的既有对象身份。</summary>
    public LangValueType VisitListLiteralExpression(ListLiteralExpression node) =>
        VisitListLangValue(ListLiteralValues.GetValue(node, static n => new ListLangValue(n.Elements.ToList(), position: n.Position)));

    /// <summary>求值数组字面量并保持同一节点的既有对象身份。</summary>
    public LangValueType VisitArrayLiteralExpression(ArrayLiteralExpression node) =>
        VisitArrayLangValue(ArrayLiteralValues.GetValue(node, static n => new ArrayLangValue(n.Elements, position: n.Position)));

    /// <summary>求值标量字面量。</summary>
    public LangValueType VisitScalarLiteralExpression(ScalarLiteralExpression node) =>
        LiteralValues.GetValue(node, static n => ScalarLiteralRuntimeValueFactory.Create(n));
}

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
