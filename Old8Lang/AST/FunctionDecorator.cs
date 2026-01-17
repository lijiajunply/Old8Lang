namespace Old8Lang.AST;

/// <summary>
/// 表示函数装饰器
/// </summary>
public class FunctionDecorator
{
    /// <summary>
    /// 装饰器名称（例如：log, cache）
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 装饰器参数列表（可选）
    /// </summary>
    public List<LangExpression>? Arguments { get; set; }

    /// <summary>
    /// 装饰器在源代码中的位置
    /// </summary>
    public SourcePosition Position { get; set; }

    public FunctionDecorator(string name, List<LangExpression>? arguments = null, SourcePosition position = default)
    {
        Name = name;
        Arguments = arguments;
        Position = position;
    }
}
