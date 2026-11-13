using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

public partial class IfChild(LangExpression expression, BlockStatement blockStatement, SourcePosition position = default)
    : OldStatement(position)
{
    public LangExpression Condition => expression;
    public BlockStatement Block => blockStatement;


    public override string ToString() => $"{expression}\n {{ {blockStatement} }}";


    public override OldStatement this[int index] => blockStatement[index];

    public override int Count => blockStatement.Count;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("IfChild 是内部辅助类，不支持 Visitor 模式访问");
    }
}