using System.Runtime.CompilerServices;

namespace Old8Lang.AST.Expression;

/// <summary>编译过程的运算类型缓存，随节点回收。</summary>
internal static class OperationTypeCache
{
    private sealed class Entry { public Type? Type; }
    private static readonly ConditionalWeakTable<Operation, Entry> Entries = new();
    internal static Type? Get(Operation node) => Entries.TryGetValue(node, out var entry) ? entry.Type : null;
    internal static void Set(Operation node, Type? type) => Entries.GetValue(node, static _ => new Entry()).Type = type;
}
