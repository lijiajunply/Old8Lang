using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 列表推导式
/// </summary>
public partial class ListComprehension : LangValueType
{
    
    /// <summary>
    /// 表达式部分，用于生成列表元素
    /// </summary>
    public LangExpression Expression { get; }

    /// <summary>
    /// 遍历变量
    /// </summary>
    public LangId Variable { get; }

    /// <summary>
    /// 可迭代对象
    /// </summary>
    public LangExpression Iterable { get; }

    /// <summary>
    /// 条件筛选（可选）
    /// </summary>
    public LangExpression? Condition { get; }

    /// <summary>
    /// 嵌套的for循环（可选）
    /// </summary>
    public List<ListComprehension>? NestedLoops { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="expression">表达式部分</param>
    /// <param name="variable">遍历变量</param>
    /// <param name="iterable">可迭代对象</param>
    /// <param name="condition">条件筛选（可选）</param>
    /// <param name="nestedLoops">嵌套的for循环（可选）</param>
    /// <param name="position">位置信息</param>
    public ListComprehension(
        LangExpression expression,
        LangId variable,
        LangExpression iterable,
        LangExpression? condition = null,
        List<ListComprehension>? nestedLoops = null,
        SourcePosition position = default)
        : base(position)
    {
        Expression = expression;
        Variable = variable;
        Iterable = iterable;
        Condition = condition;
        NestedLoops = nestedLoops;
    }



    public override string ToString()
    {
        var s = NestedLoops?.Count > 0 ? " " + string.Join(" ", NestedLoops.Select(loop => loop.ToString())) : "";
        return
            $"[{Expression} for {Variable} in {Iterable} {(Condition is not null ? "if " + Condition : "")} {s}]";
    }
}