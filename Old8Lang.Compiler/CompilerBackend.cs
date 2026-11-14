using Old8Lang.AST.Visitor;

namespace Old8Lang.Compiler;

/// <summary>
/// IL 后端的组合注册入口。
/// </summary>
/// <remarks>
/// Runtime 共享层通过 <see cref="RuntimeVisitors"/> 持有 IL 访问器工厂；组合根必须显式
/// 调用 <see cref="Register"/>，未注册时 Runtime 会给出缺少后端程序集的明确错误。
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

