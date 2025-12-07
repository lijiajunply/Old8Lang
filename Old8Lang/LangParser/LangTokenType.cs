using Old8Lang.AST.Expression;

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
    Try,
    Catch,
    Finally,
    PlusPlus,
    MinusMinus,
    EndOfFile,
    In,
    Elif,
    Else,
    Case,
    Default,
    Return,
    As,
    List
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
    As,
    Try,
    Catch,
    Finally,
    And,
    Or,
    Xor,
    List
}

public static class TokenOpera
{
    public static OperationType GetGeneric(this LangTokenType type)
    {
        return type switch
        {
            LangTokenType.Plus => OperationType.PLUS,
            LangTokenType.Minus => OperationType.MINUS,
            LangTokenType.Star => OperationType.TIMES,
            LangTokenType.Slash => OperationType.DIVIDE,
            LangTokenType.Ampersand => OperationType.AND,
            LangTokenType.Pipe => OperationType.OR,
            LangTokenType.String => OperationType.STRING,
            LangTokenType.Number => OperationType.INT,
            LangTokenType.Identifier => OperationType.IDENTIFIER,
            LangTokenType.LeftParen => OperationType.LPAREN,
            LangTokenType.RightParen => OperationType.RPAREN,
            LangTokenType.LeftBrace => OperationType.L_BRACES,
            LangTokenType.RightBrace => OperationType.R_BRACES,
            LangTokenType.LeftBracket => OperationType.L_BRACKET,
            LangTokenType.RightBracket => OperationType.R_BRACKET,
            LangTokenType.Comma => OperationType.COMMA,
            LangTokenType.Colon => OperationType.COLON,
            LangTokenType.Dot => OperationType.CONCAT,
            LangTokenType.Exclamation => OperationType.NOT,
            LangTokenType.Assignment => OperationType.SET,
            LangTokenType.Equals => OperationType.EQUALS,
            LangTokenType.Percent => OperationType.EOF,
            LangTokenType.Caret => OperationType.EOF,
            LangTokenType.Question => OperationType.EOF,
            LangTokenType.LessThan => OperationType.LESSER,
            LangTokenType.GreaterThan => OperationType.GREATER,
            LangTokenType.LessThanEquals => OperationType.LESS_EQUAL,
            LangTokenType.GreaterThanEquals => OperationType.GREATER_EQUAL,
            LangTokenType.NotEquals => OperationType.DIFFERENT,
            LangTokenType.And => OperationType.AND,
            LangTokenType.Or => OperationType.OR,
            LangTokenType.Xor => OperationType.OR,
            LangTokenType.Null => OperationType.EOF,
            LangTokenType.True => OperationType.TRUE,
            LangTokenType.False => OperationType.FALSE,
            LangTokenType.Return => OperationType.RETURN,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}