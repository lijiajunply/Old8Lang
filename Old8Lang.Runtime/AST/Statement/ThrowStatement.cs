using Old8Lang.Error;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Helpers;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

public partial class ThrowStatement(LangExpression expression, SourcePosition position = default) : OldStatement(position)
{
    internal LangExpression Expression => expression;

    public override OldStatement? this[int index] => null;

    public override int Count => 0;

    public override string ToString() => $"throw {expression}";
}