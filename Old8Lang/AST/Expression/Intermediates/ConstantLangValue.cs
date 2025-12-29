using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 常量值包装类，用于将常量存储到 ImportInfo 中
/// 常量在模块导出时可以被导入
/// </summary>
/// <param name="name">常量名称</param>
/// <param name="value">常量值</param>
/// <param name="position">源代码位置</param>
public class ConstantLangValue(
    string name,
    LangValueType value,
    SourcePosition position = default
) : ImportInfo(position)
{
    /// <summary>
    /// 常量名称
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// 常量值
    /// </summary>
    public LangValueType Value { get; } = value;

    public override string ToString()
    {
        return $"<constant {Name} = {Value}>";
    }

    public override TResult Accept<TResult>(Visitor.IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("ConstantLangValue 暂不支持 Visitor 模式访问");
    }
}
