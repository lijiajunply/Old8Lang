using Old8Lang.AST.Visitor;

namespace Old8Lang.Interpreter;

/// <summary>
/// 解释后端的组合注册入口。
/// </summary>
/// <remarks>
/// Runtime 共享层只持有 <see cref="RuntimeVisitors"/> 中的抽象工厂；解释后端程序集负责把
/// 具体 Visitor 注册进去。组合根必须显式调用 <see cref="Register"/>；未注册时 Runtime 会给出缺少后端程序集的明确错误。
/// </remarks>
public static class InterpreterBackend
{
    /// <summary>
    /// 把解释执行 Visitor 注册为 Runtime 的后端工厂。重复调用是幂等的。
    /// </summary>
    public static void Register()
    {
        RuntimeVisitors.RegisterInterpreter(static manager => new InterpreterVisitor(manager));
        RuntimeVisitors.RegisterInterpreterHost(static standardLibraries => new LangInterpreter(standardLibraries));
    }
}

