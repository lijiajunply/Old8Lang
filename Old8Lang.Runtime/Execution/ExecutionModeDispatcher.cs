using System.Reflection.Emit;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.Execution;

/// <summary>
/// 语言执行后端的统一入口。
/// </summary>
/// <remarks>
/// AST 中仍保留的 <c>Run</c> / <c>GenerateIl</c> 方法仅用于逐节点迁移期间的兼容。
/// 所有宿主级调用必须经由本类进入对应 Visitor，避免新的调用点继续绑定到旧 AST 执行 API。
/// </remarks>
public static class ExecutionModeDispatcher
{
    /// <summary>
    /// 使用解释执行 Visitor 执行程序根节点。
    /// </summary>
    public static void Execute(BlockStatement statement, VariateManager manager)
    {
        ArgumentNullException.ThrowIfNull(statement);
        ArgumentNullException.ThrowIfNull(manager);

        _ = statement.Accept(RuntimeVisitors.CreateInterpreter(manager));
    }

    /// <summary>
    /// 使用 IL Visitor 为程序根节点生成 IL。
    /// </summary>
    public static void GenerateIl(BlockStatement statement, ILGenerator ilGenerator, LocalManager local)
    {
        ArgumentNullException.ThrowIfNull(statement);
        ArgumentNullException.ThrowIfNull(ilGenerator);
        ArgumentNullException.ThrowIfNull(local);

        _ = statement.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
    }
}
