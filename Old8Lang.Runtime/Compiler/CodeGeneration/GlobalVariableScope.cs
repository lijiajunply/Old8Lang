namespace Old8Lang.Compiler;

/// <summary>
/// IL 模式下顶层变量的全局槽位信息。
/// </summary>
/// <remarks>
/// 不是所有顶层变量都需要全局槽位：只有那些会被函数体（具名函数、lambda、类方法）访问的
/// 变量才需要，因为它们的值是跨 <see cref="System.Reflection.Emit.DynamicMethod"/> 访问的。
/// 其余顶层变量继续使用普通的局部变量，以保留 IL 模式的性能。
/// </remarks>
public sealed class GlobalVariableScope
{
    /// <summary>顶层变量名 → 静态类型。</summary>
    public Dictionary<string, Type> Types { get; } = new(StringComparer.Ordinal);

    /// <summary>需要提升为全局槽位的变量名。</summary>
    public HashSet<string> SlotNames { get; } = new(StringComparer.Ordinal);

    public bool IsGlobal(string name) => Types.ContainsKey(name);

    public bool IsSlot(string name) => SlotNames.Contains(name);

    public Type GetType(string name) => Types.TryGetValue(name, out var type) ? type : typeof(object);

    /// <summary>把一个顶层变量提升为全局槽位，返回是否是本次新提升。</summary>
    public bool Promote(string name) => SlotNames.Add(name);

    /// <summary>仅供运行时初始化使用的槽位类型表。</summary>
    public Dictionary<string, Type> GetSlotTypes()
    {
        var result = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var name in SlotNames)
        {
            result[name] = GetType(name);
        }

        return result;
    }
}
