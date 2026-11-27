using System.Runtime.CompilerServices;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.Bytecode.VM;

/// <summary>
/// 全局表并发状态标记（同一虚拟机实例及其所有 worker 共享一份）。
/// </summary>
/// <remarks>
/// <see cref="VmValue"/> 为 24 字节值类型，跨线程裸读写会产生结构体撕裂。
/// 单线程执行期间 <see cref="IsConcurrent"/> 为 false，全局槽位走零锁零屏障直读直写；
/// 一旦派生 worker 虚拟机（<c>spawn</c> / <c>CallAsync</c> / <c>Task.Run</c>），
/// 在启动工作线程前将此标记置 true，所有槽位自动切换为无锁 SeqLock 读写，保证无撕裂、零 SyncBlock 分配且多核只读零争用。
/// </remarks>
internal sealed class GlobalTableState
{
    internal volatile bool IsConcurrent;
}

/// <summary>
/// 全局变量槽位（类型化 <see cref="VmValue"/> 存储）。
/// </summary>
/// <remarks>
/// 旧实现把全局变量存在 <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/> 里，
/// 每次 <c>LoadGlobal</c> / <c>StoreGlobal</c> 都要按名字计算哈希并查表（顶层循环里一次迭代可能做 4 次）。
/// 现在字典只负责「名字 -> 槽位」的解析，解析结果由 <see cref="Old8Lang.Bytecode.Core.Instruction"/>
/// 缓存，热路径上退化为一次类型化字段读取 / 写入（零装箱）。
///
/// <para>
/// 槽位归属于某个全局表（一个虚拟机实例及其 worker 共享同一个表），<see cref="Owner"/> 用来校验
/// 指令上缓存的槽位是否仍属于当前虚拟机的全局表——同一份字节码被多个虚拟机实例（或再次执行）
/// 复用时，旧槽位必须被识别出来并重新解析，否则会读到上一次执行的残留值。
/// </para>
/// </remarks>
internal sealed class GlobalSlot
{
    private readonly GlobalTableState _state;
    private VmValue _value;
    // SeqLock 版本号：偶数为稳定状态，奇数为写入进行中（仅在并发模式下使用）
    private int _version;

    internal GlobalSlot(int owner, GlobalTableState state, VmValue value)
    {
        Owner = owner;
        _state = state;
        _value = value;
    }

    /// <summary>所属全局表的标识（创建槽位时分配，全局唯一）</summary>
    internal int Owner { get; }

    /// <summary>读取槽位中的类型化值（单线程零锁，并发模式无锁 SeqLock 防撕裂）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal VmValue GetValue()
    {
        if (!_state.IsConcurrent)
        {
            return _value;
        }

        return GetValueConcurrent();
    }

    /// <summary>写入槽位中的类型化值（单线程零锁，并发模式 SeqLock 防撕裂）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetValue(VmValue value)
    {
        if (!_state.IsConcurrent)
        {
            _value = value;
            return;
        }

        SetValueConcurrent(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private VmValue GetValueConcurrent()
    {
        var spinWait = new SpinWait();
        while (true)
        {
            int v1 = Volatile.Read(ref _version);
            if ((v1 & 1) != 0)
            {
                spinWait.SpinOnce();
                continue;
            }

            // VmValue.ObjectRef 为对齐的指针宽度托管引用，按 ECMA-335 读写单字段原子，
            // 即使并发写入导致结构体字段不匹配，snapshot 也绝对 GC 安全，随后通过 v1 == v2 校验丢弃重试。
            var snapshot = _value;
            Interlocked.MemoryBarrier();
            int v2 = Volatile.Read(ref _version);
            if (v1 == v2)
            {
                return snapshot;
            }

            spinWait.SpinOnce();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SetValueConcurrent(VmValue value)
    {
        var spinWait = new SpinWait();
        while (true)
        {
            int v = Volatile.Read(ref _version);
            if ((v & 1) != 0)
            {
                spinWait.SpinOnce();
                continue;
            }

            if (Interlocked.CompareExchange(ref _version, unchecked(v + 1), v) == v)
            {
                _value = value;
                Volatile.Write(ref _version, unchecked(v + 2));
                return;
            }

            spinWait.SpinOnce();
        }
    }

    /// <summary>
    /// 面向冷路径与外部边界的 <see cref="object"/> 视图。
    /// </summary>
    internal object? Value
    {
        get => GetValue().ToObject();
        set => SetValue(VmValue.FromObject(value));
    }
}
