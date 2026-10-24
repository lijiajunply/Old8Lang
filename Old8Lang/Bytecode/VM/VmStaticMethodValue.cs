namespace Old8Lang.Bytecode.VM;

/// <summary>
/// 虚拟机里「静态类方法」作为一等函数值时的表示。
/// </summary>
/// <remarks>
/// 解释器把 <c>Task.Delay</c> / <c>Thread.Sleep</c> 这类裸引用物化成
/// <c>TaskStaticMethodWrapper</c> 等包装对象，可以赋给变量后再调用
/// （<c>f &lt;- Task.Delay; await f(10)</c>）。虚拟机没有这套对象，编译期把裸引用
/// 改写成一个带前缀的原生调用，运行期得到本类型；调用时再按限定名走静态类分发，
/// 因此「取方法值」和「直接调用」最终走同一条实现。
/// </remarks>
public sealed class VmStaticMethodValue(string qualifiedName)
{
    /// <summary>运行期限定名，如 <c>Task.Delay</c></summary>
    public string QualifiedName { get; } = qualifiedName;

    public override string ToString() => QualifiedName;
}
