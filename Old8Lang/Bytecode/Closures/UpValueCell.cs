namespace Old8Lang.Bytecode.Closures;

/// <summary>
/// 被闭包按引用捕获的变量的共享单元（upvalue 盒子）。
/// </summary>
/// <remarks>
/// 字节码模式的闭包原先按值快照捕获：MakeClosure 把外层局部变量的当前值复制进闭包环境。
/// 这带来两个后果：
/// <list type="bullet">
/// <item>外层在闭包创建之后再给该变量赋值，闭包读到的仍是旧值；</item>
/// <item>闭包内对该变量赋值无法传回外层，只能报错或静默遮蔽。</item>
/// </list>
/// 引入共享单元后，外层帧的局部变量槽位与闭包环境指向同一个盒子：外层读写与闭包读写
/// 都经由盒子，双向可见，与解释器语义一致。未被任何闭包捕获的变量不会产生盒子，
/// 因此不额外增加开销。
/// </remarks>
public sealed class UpValueCell
{
    /// <summary>盒子里当前的值</summary>
    public object? Value { get; set; }

    /// <summary>
    /// 用初始值创建共享单元
    /// </summary>
    public UpValueCell(object? value = null)
    {
        Value = value;
    }

    /// <summary>
    /// 若 <paramref name="slot"/> 还不是共享单元，则原地装箱并写回该槽位，返回盒子。
    /// 已经是盒子时直接返回，保证同一个变量只对应一个盒子。
    /// </summary>
    /// <param name="slot">要装箱的槽位；装箱结果写回这里</param>
    public static UpValueCell Box(ref object? slot)
    {
        if (slot is UpValueCell existing)
        {
            return existing;
        }

        var cell = new UpValueCell(slot);
        slot = cell;
        return cell;
    }
}
