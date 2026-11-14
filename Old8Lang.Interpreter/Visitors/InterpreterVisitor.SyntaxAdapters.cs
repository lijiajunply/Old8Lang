using System.Runtime.CompilerServices;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Runtime;

namespace Old8Lang.AST.Visitor;

// 解释后端专属的语法节点适配：标量/集合字面量、简单调用与 lambda 表达式。
// 这些适配与具体后端绑定（字节码与 IL 适配仍在各自后端中），因此随解释后端一起迁出 Runtime。
public partial class InterpreterVisitor
{
    /// <summary>访问嵌套索引表达式。</summary>
    public LangValueType VisitNestedIndexExpression(NestedIndexExpression node) =>
        VisitNestedIndexAccess(LiteralCollectionRuntimeFactory.CreateNestedIndex(node));

    /// <summary>访问嵌套切片表达式。</summary>
    public LangValueType VisitNestedSliceExpression(NestedSliceExpression node) =>
        VisitNestedSliceAccess(LiteralCollectionRuntimeFactory.CreateNestedSlice(node));

    /// <summary>求值索引表达式。</summary>
    public LangValueType VisitIndexExpression(IndexExpression node) =>
        VisitLangListItem(LiteralCollectionRuntimeFactory.CreateIndex(node));

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
        VisitListLangValue(ListLiteralValues.GetValue(node,
            static n => new ListLangValue(n.Elements.ToList(), position: n.Position)));

    /// <summary>求值数组字面量并保持同一节点的既有对象身份。</summary>
    public LangValueType VisitArrayLiteralExpression(ArrayLiteralExpression node) =>
        VisitArrayLangValue(ArrayLiteralValues.GetValue(node,
            static n => new ArrayLangValue(n.Elements, position: n.Position)));

    /// <summary>求值标量字面量。</summary>
    public LangValueType VisitScalarLiteralExpression(ScalarLiteralExpression node) =>
        LiteralValues.GetValue(node, static n => ScalarLiteralRuntimeValueFactory.Create(n));

    /// <summary>访问简单函数调用。</summary>
    public LangValueType VisitCallExpression(CallExpression node) => node.ToInstance().Accept(this);

    /// <summary>访问 lambda 表达式。</summary>
    public LangValueType VisitLambdaExpression(LambdaExpression node) =>
        LambdaExpressionRuntimeFactory.Create(node).Accept(this);
}
