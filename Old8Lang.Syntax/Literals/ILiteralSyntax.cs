namespace Old8Lang.Syntax;

/// <summary>
/// 与执行后端无关的字面量语法数据。
/// </summary>
public interface ILiteralSyntax
{
    LiteralKind Kind { get; }

    object? LiteralValue { get; }

    SourcePosition Position { get; }
}
