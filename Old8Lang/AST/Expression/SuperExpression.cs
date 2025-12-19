using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

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
public class SuperExpression(SourcePosition position = default) : LangExpression(position)
{
    /// <summary>
    /// 在当前上下文中解析super表达式
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <returns>父类实例或成员的值</returns>
    /// <exception cref="InterpretError">当super不在类实例中调用时抛出</exception>
    public override LangValueType Run(VariateManager manager)
    {
        // 获取当前实例（this）
        var currentInstance = manager.GetCurrentInstance();

        if (currentInstance == null)
        {
            throw new InvalidOperationError(Position, "super只能在类实例方法中使用");
        }

        // 获取父类实例
        var superInstance = currentInstance.GetSuperInstance();

        if (superInstance == null)
        {
            throw new InvalidOperationError(Position, "当前类没有父类，无法使用super");
        }

        return superInstance;
    }

    public override string ToString()
    {
        return "super";
    }
}