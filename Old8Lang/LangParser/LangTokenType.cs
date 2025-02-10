using Old8Lang.CslyParser;

namespace Old8Lang.LangParser;

public enum LangTokenType
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
    /// ~
    /// </summary>
    Wavy,

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
    public static OldTokenGeneric GetGeneric(this LangTokenType type)
    {
        return type switch
        {
            LangTokenType.Plus => OldTokenGeneric.PLUS,
            LangTokenType.Minus => OldTokenGeneric.MINUS,
            LangTokenType.Star => OldTokenGeneric.TIMES,
            LangTokenType.Slash => OldTokenGeneric.DIVIDE,
            LangTokenType.Ampersand => OldTokenGeneric.AND,
            LangTokenType.Pipe => OldTokenGeneric.OR,
            LangTokenType.String => OldTokenGeneric.STRING,
            LangTokenType.Number => OldTokenGeneric.INT,
            LangTokenType.Identifier => OldTokenGeneric.IDENTIFIER,
            LangTokenType.LeftParen => OldTokenGeneric.LPAREN,
            LangTokenType.RightParen => OldTokenGeneric.RPAREN,
            LangTokenType.LeftBrace => OldTokenGeneric.L_BRACES,
            LangTokenType.RightBrace => OldTokenGeneric.R_BRACES,
            LangTokenType.LeftBracket => OldTokenGeneric.L_BRACKET,
            LangTokenType.RightBracket => OldTokenGeneric.R_BRACKET,
            LangTokenType.Comma => OldTokenGeneric.COMMA,
            LangTokenType.Colon => OldTokenGeneric.COLON,
            LangTokenType.Dot => OldTokenGeneric.CONCAT,
            LangTokenType.Exclamation => OldTokenGeneric.NOT,
            LangTokenType.Assignment => OldTokenGeneric.SET,
            LangTokenType.Equals => OldTokenGeneric.EQUALS,
            LangTokenType.Percent => OldTokenGeneric.EOF,
            LangTokenType.Caret => OldTokenGeneric.EOF,
            LangTokenType.Question => OldTokenGeneric.EOF,
            LangTokenType.LessThan => OldTokenGeneric.LESSER,
            LangTokenType.GreaterThan => OldTokenGeneric.GREATER,
            LangTokenType.LessThanEquals => OldTokenGeneric.LESS_EQUAL,
            LangTokenType.GreaterThanEquals => OldTokenGeneric.GREATER_EQUAL,
            LangTokenType.NotEquals => OldTokenGeneric.DIFFERENT,
            LangTokenType.And => OldTokenGeneric.AND,
            LangTokenType.Or => OldTokenGeneric.OR,
            LangTokenType.Null => OldTokenGeneric.EOF,
            LangTokenType.True => OldTokenGeneric.TRUE,
            LangTokenType.False => OldTokenGeneric.FALSE,
            LangTokenType.Return => OldTokenGeneric.RETURN,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}