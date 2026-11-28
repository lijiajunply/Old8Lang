using System.Runtime.CompilerServices;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.AST.Expression;

/// <summary>编译过程的运算类型缓存，按 LocalManager 作用域隔离并随其回收。</summary>
internal static class OperationTypeCache
{
    private static readonly ConditionalWeakTable<LocalManager, Dictionary<Operation, Type?>> Scopes = new();

    internal static Type? Get(LocalManager local, Operation node) =>
        Scopes.TryGetValue(local, out var map) && map.TryGetValue(node, out var type) ? type : null;

    internal static void Set(LocalManager local, Operation node, Type? type) =>
        Scopes.GetValue(local, static _ => new Dictionary<Operation, Type?>())[node] = type;
}
