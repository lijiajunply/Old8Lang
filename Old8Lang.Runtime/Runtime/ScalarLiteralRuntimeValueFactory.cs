using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Syntax;

namespace Old8Lang.Runtime;

/// <summary>
/// 标量字面量从无后端语法数据进入既有运行时值模型的唯一转换点。
/// </summary>
internal static class ScalarLiteralRuntimeValueFactory
{
    public static LangValueType Create(ILiteralSyntax literal) => literal.Kind switch
    {
        LiteralKind.Int when literal.LiteralValue is int value => new IntLangValue(value, literal.Position),
        LiteralKind.Double when literal.LiteralValue is double value => new DoubleLangValue(value, literal.Position),
        LiteralKind.String when literal.LiteralValue is string value => new StringLangValue(value, literal.Position),
        LiteralKind.Bool when literal.LiteralValue is bool value => new BoolLangValue(value, literal.Position),
        LiteralKind.Char when literal.LiteralValue is char value => new CharLangValue(value, literal.Position),
        LiteralKind.Null => new NullLangValue(literal.Position),
        LiteralKind.Void => new VoidLangValue(literal.Position),
        _ => throw new ArgumentException($"字面量类别 {literal.Kind} 的值无效。", nameof(literal))
    };
}
