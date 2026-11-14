namespace Old8Lang.Bytecode.VM;

/// <summary>
/// 共享的装箱布尔值。
/// </summary>
/// <remarks>
/// 求值栈/局部变量槽位是 <see cref="object"/>，每次压入比较结果或 <c>LoadTrue</c>/<c>LoadFalse</c>
/// 都会新建一个装箱对象。布尔值只有两个，缓存起来可以让循环条件、比较指令不再产生这些短命对象。
///
/// <para>
/// 这是「VM 值仍是 object 装箱」问题的一个低成本裁剪：整数运算的装箱需要类型化值表示才能消除，
/// 见 <c>Todo.md</c> 的后续项。
/// </para>
/// </remarks>
internal static class VmBoxed
{
    internal static readonly object True = true;
    internal static readonly object False = false;

    internal static object Bool(bool value) => value ? True : False;
}
