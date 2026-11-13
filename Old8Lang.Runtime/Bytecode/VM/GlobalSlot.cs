namespace Old8Lang.Bytecode.VM;

/// <summary>
/// 全局变量槽位。
/// </summary>
/// <remarks>
/// 旧实现把全局变量存在 <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/> 里，
/// 每次 <c>LoadGlobal</c> / <c>StoreGlobal</c> 都要按名字计算哈希并查表（顶层循环里一次迭代可能做 4 次）。
/// 现在字典只负责「名字 -> 槽位」的解析，解析结果由 <see cref="Old8Lang.Bytecode.Core.Instruction"/>
/// 缓存，热路径上退化为一次字段读取 / 写入。
///
/// <para>
/// 槽位归属于某个全局表（一个虚拟机实例及其 worker 共享同一个表），<see cref="Owner"/> 用来校验
/// 指令上缓存的槽位是否仍属于当前虚拟机的全局表——同一份字节码被多个虚拟机实例（或再次执行）
/// 复用时，旧槽位必须被识别出来并重新解析，否则会读到上一次执行的残留值。
/// </para>
/// </remarks>
internal sealed class GlobalSlot
{
    private object? _value;

    internal GlobalSlot(int owner, object? value)
    {
        Owner = owner;
        _value = value;
    }

    /// <summary>所属全局表的标识（创建槽位时分配，全局唯一）</summary>
    internal int Owner { get; }

    /// <summary>
    /// 槽位中的值。
    /// </summary>
    /// <remarks>
    /// worker 线程会并发读写全局变量（例如互斥计数场景），普通的字段读写不保证可见性；
    /// 用 <see cref="Volatile"/> 让读取具备 acquire 语义、写入具备 release 语义。
    /// </remarks>
    internal object? Value
    {
        get => Volatile.Read(ref _value);
        set => Volatile.Write(ref _value, value);
    }
}
