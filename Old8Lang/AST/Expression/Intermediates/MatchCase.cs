using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// Match 表达式的 case 分支
/// 支持三种模式：
/// 1. 值匹配: case 0 -> "zero"
/// 2. 变量绑定: case x -> "value is {x}"
/// 3. 通配符: case _ -> "default"
/// </summary>
public class MatchCase
{
    /// <summary>
    /// case 的模式（值匹配）
    /// 如果是变量绑定或通配符，此值为 null
    /// </summary>
    public LangExpression? Pattern { get; }

    /// <summary>
    /// 变量绑定名称（用于 case x -> ... 模式）
    /// 如果不是变量绑定模式，此值为 null
    /// </summary>
    public string? BindingVariable { get; }

    /// <summary>
    /// 是否是通配符模式 (case _ -> ...)
    /// </summary>
    public bool IsWildcard { get; }

    /// <summary>
    /// case 匹配后执行的表达式
    /// </summary>
    public LangExpression ResultExpression { get; }

    /// <summary>
    /// 是否是变量绑定模式
    /// </summary>
    public bool IsVariableBinding => BindingVariable != null && !IsWildcard;

    /// <summary>
    /// 构造函数 - 值匹配模式
    /// </summary>
    public MatchCase(LangExpression pattern, LangExpression resultExpression)
    {
        Pattern = pattern;
        ResultExpression = resultExpression;
        IsWildcard = false;
        BindingVariable = null;
    }

    /// <summary>
    /// 构造函数 - 变量绑定或通配符模式
    /// </summary>
    public MatchCase(string bindingVariable, LangExpression resultExpression, bool isWildcard = false)
    {
        BindingVariable = bindingVariable;
        ResultExpression = resultExpression;
        IsWildcard = isWildcard;
        Pattern = null;
    }

    /// <summary>
    /// 检查给定的值是否匹配当前 case
    /// </summary>
    /// <param name="value">要匹配的值</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="boundValue">如果是变量绑定，输出绑定的值</param>
    /// <returns>是否匹配</returns>
    public bool IsMatch(LangValueType value, VariateManager manager, out LangValueType? boundValue)
    {
        boundValue = null;

        // 通配符匹配所有值
        if (IsWildcard)
        {
            return true;
        }

        // 变量绑定匹配所有值，并绑定变量
        if (IsVariableBinding)
        {
            boundValue = value;
            return true;
        }

        // 值匹配：计算 pattern 表达式并比较
        if (Pattern != null)
        {
            var patternValue = Pattern.Run(manager);

            // 检查是否相等
            return value.Equal(patternValue);
        }

        return false;
    }
}
