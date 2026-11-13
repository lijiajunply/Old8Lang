using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

public partial class ForStatement(
    SetStatement setStatement,
    LangExpression expression,
    OldStatement statement,
    BlockStatement blockStatement,
    SourcePosition position = default)
    : OldStatement(position)
{
    public SetStatement Init => setStatement;
    public LangExpression Condition => expression;
    public OldStatement Operation => statement;
    public BlockStatement Block => blockStatement;

    /// <summary>
    /// 检测是否为简单的计数循环（可以优化）
    /// </summary>
    internal bool IsSimpleCountingLoop()
    {
        // 简单启发式：检查是否是二元比较表达式
        // 这是一个简化版本，实际实现可以更复杂
        return expression is Expression.Operation;
    }



    public override OldStatement this[int index] => blockStatement[index];

    public override int Count => blockStatement.Count;

    public override string ToString()
    {
        var sb = new StringBuilder($"for {setStatement}, {expression}, {statement}");
        sb.AppendLine();
        sb.Append($"{{{blockStatement}\n}}");
        return sb.ToString();
    }
}
