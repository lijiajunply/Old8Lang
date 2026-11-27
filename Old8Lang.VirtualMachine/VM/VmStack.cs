using System.Runtime.CompilerServices;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.Bytecode.VM;

/// <summary>
/// 虚拟机类型化求值栈，替代 <see cref="Stack{T}"/>（<c>Stack&lt;object?&gt;</c>）。
/// </summary>
/// <remarks>
/// 内部使用紧凑的 <see cref="VmValue"/> 数组，基础类型（int/long/double/bool/char/null）
/// 压栈与弹栈均零堆分配；仅在弹出持有托管对象引用的元素时清空栈槽，避免 GC 根滞留与多余写屏障。
/// 空栈弹出或查看时沿用与 BCL 一致的 <see cref="InvalidOperationException"/> 消息 <c>"Stack empty."</c>，
/// 兼容 <see cref="VirtualMachine"/> 既有的栈下溢诊断。
/// </remarks>
internal sealed class VmStack
{
    private const int InitialGrowthCapacity = 8;
    private VmValue[] _items;
    private int _count;

    internal VmStack(int initialCapacity = 0)
    {
        _items = initialCapacity > 0 ? new VmValue[initialCapacity] : Array.Empty<VmValue>();
        _count = 0;
    }

    internal int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Push(VmValue value)
    {
        var count = _count;
        var items = _items;
        if ((uint)count < (uint)items.Length)
        {
            items[count] = value;
            _count = count + 1;
            return;
        }

        PushWithResize(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void PushWithResize(VmValue value)
    {
        var newLength = _items.Length == 0 ? InitialGrowthCapacity : _items.Length * 2;
        var newItems = new VmValue[newLength];
        if (_count > 0)
        {
            Array.Copy(_items, newItems, _count);
        }

        _items = newItems;
        newItems[_count++] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PushInt(int value) => Push(VmValue.FromInt(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PushLong(long value) => Push(VmValue.FromLong(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PushDouble(double value) => Push(VmValue.FromDouble(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PushBool(bool value) => Push(VmValue.FromBool(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PushChar(char value) => Push(VmValue.FromChar(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PushNull() => Push(VmValue.Null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PushRef(object? value) => Push(VmValue.FromReference(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PushObject(object? value) => Push(VmValue.FromObject(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal VmValue Pop()
    {
        var idx = _count - 1;
        if ((uint)idx >= (uint)_items.Length)
        {
            ThrowStackEmpty();
        }

        var items = _items;
        var value = items[idx];
        _count = idx;
        if (value.HasReference)
        {
            items[idx] = default;
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PopDiscard()
    {
        var idx = _count - 1;
        if ((uint)idx >= (uint)_items.Length)
        {
            ThrowStackEmpty();
        }

        var items = _items;
        _count = idx;
        if (items[idx].HasReference)
        {
            items[idx] = default;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object? PopObject() => Pop().ToObject();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int PopInt32() => Pop().ToInt32();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal VmValue Peek()
    {
        var idx = _count - 1;
        if ((uint)idx >= (uint)_items.Length)
        {
            ThrowStackEmpty();
        }

        return _items[idx];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object? PeekObject() => Peek().ToObject();

    /// <summary>
    /// 查看距离栈顶 <paramref name="depthFromTop"/> 位置的元素（0 为栈顶）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal VmValue PeekAt(int depthFromTop)
    {
        var idx = _count - 1 - depthFromTop;
        if ((uint)idx >= (uint)_items.Length)
        {
            ThrowStackEmpty();
        }

        return _items[idx];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Dup()
    {
        Push(Peek());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Swap()
    {
        if (_count < 2)
        {
            ThrowStackEmpty();
        }

        var top = _count - 1;
        (_items[top], _items[top - 1]) = (_items[top - 1], _items[top]);
    }

    internal void Clear()
    {
        if (_count > 0)
        {
            Array.Clear(_items, 0, _count);
            _count = 0;
        }
    }

    /// <summary>按从栈底到栈顶顺序拷贝当前全部元素（用于生成器暂停/恢复）</summary>
    internal VmValue[] SnapshotBottomToTop()
    {
        if (_count == 0)
        {
            return [];
        }

        var copy = new VmValue[_count];
        Array.Copy(_items, 0, copy, 0, _count);
        return copy;
    }

    /// <summary>清空栈并按从栈底到栈顶顺序恢复元素</summary>
    internal void RestoreFromBottom(ReadOnlySpan<VmValue> items)
    {
        Clear();
        if (items.Length == 0)
        {
            return;
        }

        if (_items.Length < items.Length)
        {
            var capacity = _items.Length == 0 ? InitialGrowthCapacity : _items.Length;
            while (capacity < items.Length)
            {
                capacity *= 2;
            }

            _items = new VmValue[capacity];
        }

        items.CopyTo(_items);
        _count = items.Length;
    }

    /// <summary>按从栈顶到栈底顺序转换为 <see cref="object"/> 数组（与 <see cref="Stack{T}.ToArray"/> 顺序一致）</summary>
    internal object?[] ToObjectArrayTopToBottom()
    {
        if (_count == 0)
        {
            return [];
        }

        var result = new object?[_count];
        for (var i = 0; i < _count; i++)
        {
            result[i] = _items[_count - 1 - i].ToObject();
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowStackEmpty()
    {
        throw new InvalidOperationException("Stack empty.");
    }
}
