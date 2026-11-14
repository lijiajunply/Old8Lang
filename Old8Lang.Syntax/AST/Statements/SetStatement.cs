using Old8Lang.AST.Expression;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 赋值语句类，用于处理Old8Lang中的赋值操作
/// 支持普通变量赋值、成员访问赋值和索引访问赋值
/// </summary>
public partial class SetStatement : OldStatement
{
    /// <summary>
    /// 变量标识符（用于普通变量赋值）
    /// </summary>
    public readonly LangId? Id;

    /// <summary>
    /// 左侧表达式（用于成员访问或索引访问赋值）
    /// </summary>
    internal readonly LangExpression? _leftExpression;

    /// <summary>
    /// 获取左侧表达式（用于成员访问或索引访问赋值）
    /// </summary>
    public LangExpression? LeftExpression => _leftExpression;

    /// <summary>
    /// 赋值表达式
    /// </summary>
    public readonly LangExpression Value;

    /// <summary>
    /// 构造函数：创建普通变量赋值语句
    /// </summary>
    /// <param name="id">变量标识符</param>
    /// <param name="value">赋值表达式</param>
    /// <param name="position">源代码位置信息，用于错误报告</param>
    public SetStatement(LangId id, LangExpression value, SourcePosition position = default) : base(position)
    {
        Id = id;
        _leftExpression = null;
        Value = value;
    }

    /// <summary>
    /// 构造函数：创建成员访问或索引访问赋值语句
    /// </summary>
    /// <param name="leftExpression">左侧表达式（成员访问或索引访问）</param>
    /// <param name="value">赋值表达式</param>
    /// <param name="position">源代码位置信息，用于错误报告</param>
    public SetStatement(LangExpression leftExpression, LangExpression value, SourcePosition position = default) :
        base(position)
    {
        Id = null;
        _leftExpression = leftExpression;
        Value = value;
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitSetStatement(this);

    /// <summary>
    /// 获取指定索引处的语句（实现OldStatement接口）
    /// </summary>
    /// <param name="index">语句索引</param>
    /// <returns>返回当前语句本身，因为SetStatement是单个语句</returns>
    public override OldStatement this[int index] => this;

    /// <summary>
    /// 获取语句数量（实现OldStatement接口）
    /// </summary>
    /// <returns>返回0，因为SetStatement是单个语句</returns>
    public override int Count => 0;

    /// <summary>
    /// 将赋值语句转换为字符串表示
    /// </summary>
    /// <returns>赋值语句的字符串表示</returns>
    public override string ToString()
    {
        if (_leftExpression is not null)
        {
            return $"{_leftExpression} <- {Value}";
        }

        // 如果 Id 为空或 IdName 为空，只显示右值（用于块表达式的返回值）
        if (Id is null || string.IsNullOrEmpty(Id.IdName))
        {
            return $" <- {Value}";
        }

        return $"{Id} <- {Value}";
    }
}
