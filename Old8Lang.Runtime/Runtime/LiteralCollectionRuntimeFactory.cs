using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.Intermediates;

namespace Old8Lang.Runtime;

/// <summary>将集合语法构造成现有运行时值。</summary>
internal static class LiteralCollectionRuntimeFactory
{
    internal static NestedIndexAccess CreateNestedIndex(NestedIndexExpression node) => new(node.Target, node.Index, node.Position);
    internal static NestedSliceAccess CreateNestedSlice(NestedSliceExpression node) => new(node.Target, node.Start, node.End, node.Step, node.Position);

    internal static bool TryCreateNestedIndex(LangExpression? expression, out NestedIndexAccess item)
    {
        if (expression is NestedIndexExpression syntax)
        {
            item = CreateNestedIndex(syntax);
            return true;
        }
        if (expression is NestedIndexAccess legacy)
        {
            item = legacy;
            return true;
        }
        item = null!;
        return false;
    }

    internal static LangListItem CreateIndex(IndexExpression node) => new(node.Target, node.Key, node.Position);

    internal static bool TryCreateIndex(LangExpression? expression, out LangListItem item)
    {
        if (expression is IndexExpression syntax)
        {
            item = CreateIndex(syntax);
            return true;
        }
        if (expression is LangListItem legacy)
        {
            item = legacy;
            return true;
        }
        item = null!;
        return false;
    }

    internal static SliceLangValue CreateSlice(SliceExpression node) =>
        new(node.Id, node.Start, node.End, node.Step) { Position = node.Position };

    internal static RangeLangValue CreateRange(RangeLiteralExpression node) =>
        new(node.Start, node.End, node.Position, node.IncludeStart, node.IncludeEnd);

    internal static TupleLangValue CreateTuple(TupleLiteralExpression node) =>
        new(node.Elements.ToList(), node.FieldNames?.ToList(), node.Position);

    internal static DictionaryLangValue CreateDictionary(DictionaryLiteralExpression node) =>
        new(node.Entries.Select(entry => new TupleLangValue(entry.Key, entry.Value, node.Position)).ToList(),
            position: node.Position);
}
