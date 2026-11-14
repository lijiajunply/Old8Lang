using Old8Lang.Bytecode.Core;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// 字节码后端的访问器契约。
/// </summary>
/// <remarks>
/// 字节码编译器只需要「遍历 AST 生成指令」与最终的指令/栈深结果；具体 Visitor 由
/// <see cref="RuntimeVisitors"/> 创建，Runtime 不直接引用 VM 后端程序集。
/// </remarks>
public interface IBytecodeVisitor : IRuntimeVisitor<Instruction?>
{
    /// <summary>本次编译产生的指令列表。</summary>
    List<Instruction> GetInstructions();

    /// <summary>本次编译估算的最大求值栈深度。</summary>
    int MaxStackSize { get; }
}
