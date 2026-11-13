using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// while语句
/// </summary>
public partial class WhileStatement(LangExpression expression, OldStatement blockStatement, SourcePosition position = default)
    : OldStatement(position)
{
    public LangExpression Condition => expression;
    public OldStatement Block => blockStatement;



    public override OldStatement this[int index] => blockStatement[index] ?? throw new IndexOutOfRangeException();

    public override int Count => blockStatement.Count;

    public override string ToString() => $"while {expression}\n{blockStatement}";
}