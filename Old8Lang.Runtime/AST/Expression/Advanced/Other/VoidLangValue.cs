using Old8Lang.Error;
using Old8Lang.Syntax;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 静态值，不可访问
/// </summary>
/// <param name="position">位置信息</param>
public partial class VoidLangValue(SourcePosition position = default) : LangValueType(position), ILiteralSyntax
{

    /// <inheritdoc />
    public object? LiteralValue => null;

    public LiteralKind Kind => LiteralKind.Void;

    /// <summary>
    /// 单例实例，用于减少对象创建开销（VoidLangValue 是无状态的）
    /// </summary>
    public static readonly VoidLangValue Instance = new();


    public override object GetValue() => throw new InvalidOperationError(this, "尝试访问无效值（VoidValue）");

    public override string ToString() => ""; // VoidValue 转换为字符串时返回空字符串，而不是抛出异常

}
