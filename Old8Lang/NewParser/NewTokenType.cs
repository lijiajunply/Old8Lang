using Old8Lang.CslyParser;

namespace Old8Lang.NewParser;

public enum NewTokenType
{
    /// <summary>
    /// +
    /// </summary>
    Plus,

    /// <summary>
    /// -
    /// </summary>
    Minus,

    /// <summary>
    /// *
    /// </summary>
    Star,

    /// <summary>
    /// /
    /// </summary>
    Slash,

    /// <summary>
    /// %
    /// </summary>
    Percent,

    /// <summary>
    /// ^
    /// </summary>
    Caret,

    /// <summary>
    /// &
    /// </summary>
    Ampersand,

    /// <summary>
    /// |
    /// </summary>
    Pipe,

    /// <summary>
    /// "~"
    /// </summary>
    String,

    /// <summary>
    /// '~'
    /// </summary>
    Char,

    /// <summary>
    /// 0-9
    /// </summary>
    Number,

    /// <summary>
    /// a-zA-Z_
    /// </summary>
    Identifier,

    /// <summary>
    /// (
    /// </summary>
    LeftParen,

    /// <summary>
    /// )
    /// </summary>
    RightParen,

    /// <summary>
    /// {
    /// </summary>
    LeftBrace,

    /// <summary>
    /// }
    /// </summary>
    RightBrace,

    /// <summary>
    /// [
    /// </summary>
    LeftBracket,

    /// ]
    RightBracket,

    /// <summary>
    ///  ,
    /// </summary>
    Comma,

    /// <summary>
    /// :
    /// </summary>
    Colon,

    /// <summary>
    /// .
    /// </summary>
    Dot,

    /// <summary>
    /// ?
    /// </summary>
    Question,

    /// !
    Exclamation,

    // <-
    Assignment,

    /// <summary>
    /// ==
    /// </summary>
    Equals,

    LessThan,

    /// <summary>
    /// >
    /// </summary>
    GreaterThan,

    LessThanEquals,

    /// <summary>
    /// >=
    /// </summary>
    GreaterThanEquals,

    /// <summary>
    /// !=
    /// </summary>
    NotEquals,

    /// <summary>
    /// &&
    /// </summary>
    And,

    /// <summary>
    /// ||
    /// </summary>
    Or,
    Xor,

    /// <summary>
    /// ->
    /// </summary>
    Arrow,

    /// <summary>
    /// $
    /// </summary>
    Dollar,

    /// <summary>
    /// ..
    /// </summary>
    DotDot,

    /// <summary>
    /// not
    /// </summary>
    Not,
    Null,
    True,
    False,
    If,
    For,
    While,
    Switch,
    Func,
    Class,
    Import,
    PlusPlus,
    MinusMinus,
    EndOfFile,
    In,
    Elif,
    Else,
    Case,
    Default,
    Return,
    As
}

public enum KeywordType
{
    Not,
    Null,
    True,
    False,
    If,
    For,
    While,
    Switch,
    Func,
    Class,
    Import,
    In,
    Elif,
    Else,
    Case,
    Default,
    Return,
    As
}

public static class TokenOpera
{
    public static OldTokenGeneric GetGeneric(this NewTokenType type)
    {
        return type switch
        {
            NewTokenType.Plus => OldTokenGeneric.PLUS,
            NewTokenType.Minus => OldTokenGeneric.MINUS,
            NewTokenType.Star => OldTokenGeneric.TIMES,
            NewTokenType.Slash => OldTokenGeneric.DIVIDE,
            NewTokenType.Ampersand => OldTokenGeneric.AND,
            NewTokenType.Pipe => OldTokenGeneric.OR,
            NewTokenType.String => OldTokenGeneric.STRING,
            NewTokenType.Number => OldTokenGeneric.INT,
            NewTokenType.Identifier => OldTokenGeneric.IDENTIFIER,
            NewTokenType.LeftParen => OldTokenGeneric.LPAREN,
            NewTokenType.RightParen => OldTokenGeneric.RPAREN,
            NewTokenType.LeftBrace => OldTokenGeneric.L_BRACES,
            NewTokenType.RightBrace => OldTokenGeneric.R_BRACES,
            NewTokenType.LeftBracket => OldTokenGeneric.L_BRACKET,
            NewTokenType.RightBracket => OldTokenGeneric.R_BRACKET,
            NewTokenType.Comma => OldTokenGeneric.COMMA,
            NewTokenType.Colon => OldTokenGeneric.COLON,
            NewTokenType.Dot => OldTokenGeneric.CONCAT,
            NewTokenType.Exclamation => OldTokenGeneric.NOT,
            NewTokenType.Assignment => OldTokenGeneric.SET,
            NewTokenType.Equals => OldTokenGeneric.EQUALS,
            NewTokenType.Percent => OldTokenGeneric.EOF,
            NewTokenType.Caret => OldTokenGeneric.EOF,
            NewTokenType.Question => OldTokenGeneric.EOF,
            NewTokenType.LessThan => OldTokenGeneric.LESSER,
            NewTokenType.GreaterThan => OldTokenGeneric.GREATER,
            NewTokenType.LessThanEquals => OldTokenGeneric.LESS_EQUAL,
            NewTokenType.GreaterThanEquals => OldTokenGeneric.GREATER_EQUAL,
            NewTokenType.NotEquals => OldTokenGeneric.DIFFERENT,
            NewTokenType.And => OldTokenGeneric.AND,
            NewTokenType.Or => OldTokenGeneric.OR,
            NewTokenType.Null => OldTokenGeneric.EOF,
            NewTokenType.True => OldTokenGeneric.TRUE,
            NewTokenType.False => OldTokenGeneric.FALSE,
            NewTokenType.Return => OldTokenGeneric.RETURN,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}