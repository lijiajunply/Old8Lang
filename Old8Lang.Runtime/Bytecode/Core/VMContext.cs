using Old8Lang.Bytecode.VM;

namespace Old8Lang.Bytecode.Core;

/// <summary>
/// 虚拟机上下文 - 用于在全局函数中访问当前虚拟机实例
/// </summary>
public static class VMContext
{
    [ThreadStatic]
    private static VirtualMachine? _currentVM;

    /// <summary>
    /// 获取或设置当前线程的虚拟机实例
    /// </summary>
    public static VirtualMachine? CurrentVM
    {
        get => _currentVM;
        set => _currentVM = value;
    }
}
