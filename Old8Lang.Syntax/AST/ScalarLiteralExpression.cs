using Old8Lang.AST.Visitor;
using Old8Lang.Syntax;
using System.Globalization;

namespace Old8Lang.AST.Expression;

/// <summary>标量字面量表达式；语法数据独立于求值结果。</summary>
public sealed class ScalarLiteralExpression(LiteralSyntax literal) : LangExpression(literal.Position), ILiteralSyntax
{
    /// <summary>字面量的纯语法数据。</summary>
    public LiteralSyntax Literal { get; } = literal;

    /// <inheritdoc />
    public LiteralKind Kind => Literal.Kind;

    /// <inheritdoc />
    public object? LiteralValue => Literal.LiteralValue;

    /// <inheritdoc />
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitScalarLiteralExpression(this);

    /// <inheritdoc />
    public override string ToString() => Kind switch
    {
        LiteralKind.String => $"\"{((string)LiteralValue!).Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\r", "\r").Replace(@"\\\\", @"\\")}\"",
        LiteralKind.Bool => (bool)LiteralValue! ? "true" : "false",
        LiteralKind.Double => ((double)LiteralValue!).ToString(CultureInfo.InvariantCulture),
        LiteralKind.Null => "Null",
        _ => LiteralValue?.ToString() ?? "Void"
    };
}
