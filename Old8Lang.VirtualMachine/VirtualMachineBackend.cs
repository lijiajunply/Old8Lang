using Old8Lang.AST.Visitor;

namespace Old8Lang.BytecodeBackend;

/// <summary>
/// 字节码/VM 后端的组合注册入口。
/// </summary>
/// <remarks>
/// Runtime 共享层通过 <see cref="RuntimeVisitors"/> 持有字节码访问器工厂；组合根必须显式
/// 调用 <see cref="Register"/>，未注册时 Runtime 会给出缺少后端程序集的明确错误。
/// </remarks>
public static class VirtualMachineBackend
{
    /// <summary>
    /// 把字节码生成 Visitor 注册为 Runtime 的后端工厂。重复调用是幂等的。
    /// </summary>
    public static void Register()
    {
        RuntimeVisitors.RegisterBytecode(static compiler => new BytecodeVisitor((Old8Lang.Bytecode.BytecodeCompiler)compiler));
    }
}

