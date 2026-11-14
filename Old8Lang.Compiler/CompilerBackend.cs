using System.Runtime.CompilerServices;
using Old8Lang.AST.Visitor;

namespace Old8Lang.Compiler;

/// <summary>
/// IL 后端的组合注册入口。
/// </summary>
/// <remarks>
/// Runtime 共享层通过 <see cref="RuntimeVisitors"/> 持有 IL 访问器工厂；组合根应显式调用
/// <see cref="Register"/>，模块初始化器在直接使用本程序集类型时提供兜底注册。
/// </remarks>
public static class CompilerBackend
{
    /// <summary>
    /// 把 IL 生成 Visitor 注册为 Runtime 的后端工厂。重复调用是幂等的。
    /// </summary>
    public static void Register()
    {
        RuntimeVisitors.RegisterCompiler(static (ilGenerator, local) => new CompilerVisitor(ilGenerator, local));
    }
}

/// <summary>
/// 程序集加载时的兜底注册。
/// </summary>
internal static class CompilerBackendModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize() => CompilerBackend.Register();
}
