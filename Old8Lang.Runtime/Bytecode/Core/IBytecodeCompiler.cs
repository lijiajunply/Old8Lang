namespace Old8Lang.Bytecode.Core;

/// <summary>
/// 字节码编译器在共享运行时中的最小契约。
/// </summary>
/// <remarks>
/// Runtime 只通过该接口把 <c>BytecodeVisitor</c> 交给具体编译器；具体
/// <c>BytecodeCompiler</c> 位于 <c>Old8Lang.VirtualMachine</c>。
/// </remarks>
public interface IBytecodeCompiler
{
}
