using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

public partial class SwitchStatement(
    LangExpression switchExpression,
    List<CaseStatement> switchCaseList,
    BlockStatement? defaultBlockStatement = null,
    SourcePosition position = default)
    : OldStatement(position)
{
    internal LangExpression SwitchExpression => switchExpression;
    internal List<CaseStatement> SwitchCaseList => switchCaseList;
    internal BlockStatement? DefaultBlockStatement => defaultBlockStatement;


    public override OldStatement this[int index] => switchCaseList[index];

    public override int Count => switchCaseList.Count;
}

public partial class CaseStatement(
    LangExpression expression,
    BlockStatement blockStatement,
    SourcePosition position = default)
    : OldStatement(position)
{

    public LangExpression Expression { get; } = expression;
    public BlockStatement BlockStatement { get; } = blockStatement;

    public override OldStatement this[int index] => BlockStatement[index];

    public override int Count => BlockStatement.Count;
}