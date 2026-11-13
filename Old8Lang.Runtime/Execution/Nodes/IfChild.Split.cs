using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// IfChild 的执行/编译辅助实现（原位于 AST 目录，阶段 1 迁出）。
/// </summary>
public partial class IfChild
{
    public void Run(VariateManager manager, ref bool r)
    {
        if (!r) return;
        var exprValue = expression.Accept(new InterpreterVisitor(manager));
        if (exprValue is not BoolLangValue { Value: true }) return;
        blockStatement.Accept(new InterpreterVisitor(manager));
        r = false;
    }
    public void GenerateConditionIl(ILGenerator ilGenerator, LocalManager local)
    {
        expression.Accept(new CompilerVisitor(ilGenerator, local));
    }
}
