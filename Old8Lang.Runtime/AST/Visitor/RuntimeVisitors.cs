using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;
using Old8Lang.ModuleSystem.StandardLibrary;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// 执行后端访问器的组合入口。
/// </summary>
/// <remarks>
/// Runtime 共享层不再直接构造具体后端的 Visitor：解释、IL 与字节码后端分别位于
/// <c>Old8Lang.Interpreter</c>、<c>Old8Lang.Compiler</c> 与 <c>Old8Lang.VirtualMachine</c>，
/// 由组合根在启动时显式注册工厂。这样 Runtime 只依赖本契约，不反向引用任何后端程序集。
/// </remarks>
public static class RuntimeVisitors
{
    private static Func<VariateManager, IRuntimeVisitor<LangValueType>>? _interpreterFactory;

    /// <summary>
    /// 注册解释后端访问器工厂。重复注册以最后一次为准，便于宿主隔离测试重置。
    /// </summary>
    public static void RegisterInterpreter(Func<VariateManager, IRuntimeVisitor<LangValueType>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _interpreterFactory = factory;
    }

    /// <summary>
    /// 创建一个解释执行访问器。未注册时说明组合根缺少解释后端程序集。
    /// </summary>
    public static IRuntimeVisitor<LangValueType> CreateInterpreter(VariateManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        var factory = _interpreterFactory ?? throw new InvalidOperationException(
            "解释后端尚未注册：请引用 Old8Lang.Interpreter 并在组合根调用 InterpreterBackend.Register()。");

        return factory(manager);
    }

    /// <summary>
    /// 是否存在已注册的解释后端。
    /// </summary>
    public static bool HasInterpreter => _interpreterFactory is not null;

    private static Func<ILGenerator, LocalManager, IRuntimeVisitor<object?>>? _compilerFactory;

    /// <summary>
    /// 注册 IL 后端访问器工厂。
    /// </summary>
    public static void RegisterCompiler(Func<ILGenerator, LocalManager, IRuntimeVisitor<object?>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _compilerFactory = factory;
    }

    /// <summary>
    /// 创建一个 IL 生成访问器。未注册时说明组合根缺少 IL 后端程序集。
    /// </summary>
    public static IRuntimeVisitor<object?> CreateCompiler(ILGenerator ilGenerator, LocalManager local)
    {
        ArgumentNullException.ThrowIfNull(ilGenerator);
        ArgumentNullException.ThrowIfNull(local);

        var factory = _compilerFactory ?? throw new InvalidOperationException(
            "IL 后端尚未注册：请引用 Old8Lang.Compiler 并在组合根调用 CompilerBackend.Register()。");

        return factory(ilGenerator, local);
    }

    /// <summary>
    /// 是否存在已注册的 IL 后端。
    /// </summary>
    public static bool HasCompiler => _compilerFactory is not null;

    private static Func<Old8Lang.Bytecode.BytecodeCompiler, IBytecodeVisitor>? _bytecodeFactory;

    /// <summary>
    /// 注册字节码/VM 后端访问器工厂。
    /// </summary>
    public static void RegisterBytecode(Func<Old8Lang.Bytecode.BytecodeCompiler, IBytecodeVisitor> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _bytecodeFactory = factory;
    }

    /// <summary>
    /// 创建一个字节码生成访问器。未注册时说明组合根缺少 VM 后端程序集。
    /// </summary>
    public static IBytecodeVisitor CreateBytecode(Old8Lang.Bytecode.BytecodeCompiler compiler)
    {
        ArgumentNullException.ThrowIfNull(compiler);

        var factory = _bytecodeFactory ?? throw new InvalidOperationException(
            "字节码后端尚未注册：请引用 Old8Lang.VirtualMachine 并在组合根调用 VirtualMachineBackend.Register()。");

        return factory(compiler);
    }

    /// <summary>
    /// 是否存在已注册的字节码/VM 后端。
    /// </summary>
    public static bool HasBytecode => _bytecodeFactory is not null;

    private static Func<StandardLibraryCatalog, ILangInterpreter>? _interpreterHostFactory;

    /// <summary>
    /// 注册解释器宿主工厂（用于模块加载等需要新建解释器的共享路径）。
    /// </summary>
    public static void RegisterInterpreterHost(Func<StandardLibraryCatalog, ILangInterpreter> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _interpreterHostFactory = factory;
    }

    /// <summary>
    /// 创建一个使用指定模块目录的解释器宿主。未注册时说明组合根缺少解释后端程序集。
    /// </summary>
    public static ILangInterpreter CreateInterpreterHost(StandardLibraryCatalog standardLibraries)
    {
        ArgumentNullException.ThrowIfNull(standardLibraries);

        var factory = _interpreterHostFactory ?? throw new InvalidOperationException(
            "解释后端尚未注册：请引用 Old8Lang.Interpreter 并在组合根调用 InterpreterBackend.Register()。");

        return factory(standardLibraries);
    }

    /// <summary>
    /// 是否存在已注册的解释器宿主工厂。
    /// </summary>
    public static bool HasInterpreterHost => _interpreterHostFactory is not null;
}
