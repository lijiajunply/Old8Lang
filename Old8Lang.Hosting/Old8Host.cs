using Old8Lang.ModuleSystem.StandardLibrary;
using Old8Lang.Modules.Abstractions;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;
using Old8Lang.Profiler;
using Old8Lang.Bytecode;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.VM;

namespace Old8Lang.Hosting;

/// <summary>
/// 构建一个隔离的 Old8Lang 宿主运行时。
/// </summary>
public sealed class Old8HostBuilder : IStandardLibraryRegistry
{
    private readonly StandardLibraryCatalog _catalog = new();

    /// <summary>
    /// 组合根构造时显式注册默认的执行后端。
    /// </summary>
    public Old8HostBuilder()
    {
        InterpreterBackend.Register();
        CompilerBackend.Register();
    }

    /// <summary>
    /// 注册一个标准库模块。
    /// </summary>
    /// <param name="module">模块实现。</param>
    /// <returns>当前构建器。</returns>
    public Old8HostBuilder Register(IStandardLibraryModule module)
    {
        _catalog.Register(module);
        return this;
    }

    /// <inheritdoc />
    public void Register(StandardLibraryModuleDescriptor descriptor)
    {
        _catalog.Register(descriptor);
    }

    /// <summary>
    /// 生成宿主实例。
    /// </summary>
    /// <returns>包含独立模块目录的宿主。</returns>
    public Old8Host Build() => new(_catalog);
}

/// <summary>
/// Old8Lang 的宿主级运行时状态。
/// </summary>
public sealed class Old8Host
{
    internal Old8Host(StandardLibraryCatalog standardLibraries)
    {
        StandardLibraries = standardLibraries;
    }

    /// <summary>
    /// 此宿主注册的标准库目录。
    /// </summary>
    public StandardLibraryCatalog StandardLibraries { get; }

    /// <summary>
    /// 将此宿主安装为当前进程默认宿主，供 CLI 和尚未完成上下文注入的执行路径使用。
    /// </summary>
    public void InstallAsDefault()
    {
        StandardLibraryRegistry.SetDefaultCatalog(StandardLibraries);
    }

    /// <summary>
    /// 创建使用此 Host 模块目录的解释器。
    /// </summary>
    /// <param name="performanceMonitor">可选性能监控器。</param>
    /// <returns>隔离的解释器实例。</returns>
    public LangInterpreter CreateInterpreter(IPerformanceMonitor? performanceMonitor = null)
    {
        return new LangInterpreter(performanceMonitor, StandardLibraries);
    }

    /// <summary>
    /// 创建使用此 Host 模块目录的字节码编译器。
    /// </summary>
    /// <returns>字节码编译器。</returns>
    public BytecodeCompiler CreateBytecodeCompiler() => new(StandardLibraries);

    /// <summary>
    /// 创建使用此 Host 模块目录的虚拟机。
    /// </summary>
    /// <param name="bytecodeFile">待执行的字节码文件。</param>
    /// <param name="baseDirectory">模块解析基目录。</param>
    /// <returns>虚拟机实例。</returns>
    public VirtualMachine CreateVirtualMachine(BytecodeFile bytecodeFile, string? baseDirectory = null)
        => new(bytecodeFile, baseDirectory, StandardLibraries);
}
