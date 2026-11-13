using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// 解释器 Visitor - 替代原有的 Run() 方法
/// </summary>
public partial class InterpreterVisitor(VariateManager manager) : IVisitor<LangValueType>
{
    // Statement 访问方法将在后续实现
    // Expression 访问方法将在后续实现
    // Value 访问方法将在后续实现
}