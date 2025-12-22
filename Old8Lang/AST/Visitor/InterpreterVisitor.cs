using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// 解释器 Visitor - 替代原有的 Run() 方法
/// </summary>
public partial class InterpreterVisitor : IVisitor<LangValueType>
{
    private readonly VariateManager _manager;

    public InterpreterVisitor(VariateManager manager)
    {
        _manager = manager;
    }

    // Statement 访问方法将在后续实现
    // Expression 访问方法将在后续实现
    // Value 访问方法将在后续实现
}
