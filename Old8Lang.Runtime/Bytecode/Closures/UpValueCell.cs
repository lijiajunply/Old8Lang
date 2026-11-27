using System.Runtime.CompilerServices;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.Bytecode.Closures;

/// <summary>
/// 被闭包按引用捕获的变量的共享单元（upvalue 盒子，类型化 <see cref="VmValue"/> 存储）。
/// </summary>
/// <remarks>
/// 字节码模式的闭包原先按值快照捕获：MakeClosure 把外层局部变量的当前值复制进闭包环境。
/// 这带来两个后果：
/// <list type="bullet">
/// <item>外层在闭包创建之后再给该变量赋值，闭包读到的仍是旧值；</item>
/// <item>闭包内对该变量赋值无法传回外层，只能报错或静默遮蔽。</item>
/// </list>
/// 引入共享单元后，外层帧的局部变量槽位与闭包环境指向同一个盒子：外层读写与闭包读写
/// 都经由盒子，双向可见，与解释器语义一致。底层采用 <see cref="VmValue"/> 存储，
/// 闭包高频读写捕获的数值/布尔变量不再产生堆装箱。
/// </remarks>
public sealed class UpValueCell
{
    private VmValue _value;

    /// <summary>盒子里当前的类型化值</summary>
    public VmValue Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _value = value;
    }

    /// <summary>盒子里当前值的 <see cref="object"/> 边界视图</summary>
    public object? ObjectValue
    {
        get => _value.ToObject();
        set => _value = VmValue.FromObject(value);
    }

    /// <summary>
    /// 用类型化初始值创建共享单元
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UpValueCell(VmValue value)
    {
        _value = value;
    }

    /// <summary>
    /// 用 <see cref="object"/> 初始值创建共享单元
    /// </summary>
    public UpValueCell(object? value = null)
    {
        _value = VmValue.FromObject(value);
    }

    /// <summary>
    /// 若 <paramref name="slot"/> 还不是共享单元，则原地包装为共享单元并写回该槽位，返回盒子。
    /// 已经是盒子时直接返回，保证同一个变量只对应一个盒子。
    /// </summary>
    /// <param name="slot">要包装的局部变量槽位；结果写回这里</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UpValueCell Box(ref VmValue slot)
    {
        if (slot.Kind == VmValueKind.UpValue)
        {
            return slot.AsUpValue;
        }

        var cell = new UpValueCell(slot);
        slot = VmValue.FromUpValue(cell);
        return cell;
    }
}
