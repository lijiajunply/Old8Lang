using System.Runtime.CompilerServices;
using Old8Lang.AST.Visitor;

namespace Old8Lang.BytecodeBackend;

/// <summary>
/// 字节码/VM 后端的组合注册入口。
/// </summary>
/// <remarks>
/// Runtime 共享层通过 <see cref="RuntimeVisitors"/> 持有字节码访问器工厂；组合根应显式调用
/// <see cref="Register"/>，模块初始化器在直接使用本程序集类型时提供兜底注册。
/// </remarks>
public static class VirtualMachineBackend
{
    /// <summary>
    /// 把字节码生成 Visitor 注册为 Runtime 的后端工厂。重复调用是幂等的。
    /// </summary>
    public static void Register()
    {
        RuntimeVisitors.RegisterBytecode(static compiler => new BytecodeVisitor(compiler));
    }
}

/// <summary>
/// 程序集加载时的兜底注册。
/// </summary>
internal static class VirtualMachineBackendModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize() => VirtualMachineBackend.Register();
}
