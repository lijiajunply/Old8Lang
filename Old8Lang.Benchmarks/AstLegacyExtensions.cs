using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.Benchmarks;

/// <summary>
/// 测试辅助扩展：把旧节点执行 API（Run/GenerateIl/LoadIlValue/OutputType）适配到 Visitor。
/// 仅用于测试工程，运行时与库代码不再提供这些入口。
/// </summary>
public static class AstLegacyExtensions
{
    public static LangValueType Run(this LangExpression expression, VariateManager manager) =>
        expression.Accept(new InterpreterVisitor(manager));

    public static LangValueType Run(this OldStatement statement, VariateManager manager) =>
        statement.Accept(new InterpreterVisitor(manager));

    public static void GenerateIl(this OldStatement statement, ILGenerator ilGenerator, LocalManager local) =>
        statement.Accept(new CompilerVisitor(ilGenerator, local));

    public static void LoadIlValue(this LangExpression expression, ILGenerator ilGenerator, LocalManager local) =>
        expression.Accept(new CompilerVisitor(ilGenerator, local));

    public static Type? OutputType(this LangExpression expression, LocalManager local) =>
        expression.Accept(new TypeInferenceVisitor(local));
}
