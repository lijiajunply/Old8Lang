using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>
/// Super表达式类，用于表示对父类成员的调用
/// </summary>
/// <param name="position">源代码位置信息，用于错误报告</param>
/// <remarks>
/// 该类用于处理super关键字，支持：
/// - super.init(params) - 调用父类构造函数
/// - super.method(params) - 调用父类方法
/// - super.property - 访问父类属性
/// </remarks>
public partial class SuperExpression(SourcePosition position = default) : LangExpression(position)
{

    public override string ToString()
    {
        return "super";
    }
}
