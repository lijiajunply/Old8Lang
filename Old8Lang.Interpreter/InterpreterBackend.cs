using System.Runtime.CompilerServices;
using Old8Lang.AST.Visitor;

namespace Old8Lang.Interpreter;

/// <summary>
/// 解释后端的组合注册入口。
/// </summary>
/// <remarks>
/// Runtime 共享层只持有 <see cref="RuntimeVisitors"/> 中的抽象工厂；解释后端程序集负责把
/// 具体 Visitor 注册进去。组合根应显式调用 <see cref="Register"/>，模块初始化器提供在
/// 直接使用本程序集类型时的兜底注册。
/// </remarks>
public static class InterpreterBackend
{
    /// <summary>
    /// 把解释执行 Visitor 注册为 Runtime 的后端工厂。重复调用是幂等的。
    /// </summary>
    public static void Register()
    {
        RuntimeVisitors.RegisterInterpreter(static manager => new InterpreterVisitor(manager));
    }
}

/// <summary>
/// 程序集加载时的兜底注册，保证直接使用解释器类型的宿主无需额外初始化。
/// </summary>
internal static class InterpreterBackendModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize() => InterpreterBackend.Register();
}
