namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 模式匹配类型枚举
/// </summary>
public enum PatternType
{
    /// <summary>
    /// 值匹配: case 0 -> "zero"
    /// </summary>
    Value,

    /// <summary>
    /// 变量绑定: case x -> "value is " + x
    /// </summary>
    VariableBinding,

    /// <summary>
    /// 通配符: case _ -> "default"
    /// </summary>
    Wildcard,

    /// <summary>
    /// 元组解构: case (x, 0) -> "on X-axis"
    /// </summary>
    Tuple,

    /// <summary>
    /// 类型匹配: case x:int -> "int value"
    /// </summary>
    TypeMatch,

    /// <summary>
    /// 范围匹配: case [0~12] -> "child"
    /// </summary>
    Range
}
