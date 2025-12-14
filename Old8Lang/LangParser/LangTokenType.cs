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
    /// ;
    /// </summary>
    Semicolon,

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
    
    /// <summary>
    /// !
    /// </summary>
    Exclamation,

    /// <summary>
    /// &lt;-
    /// </summary>
    Assignment,

    /// <summary>
    /// ==
    /// </summary>
    Equals,
    
    /// <summary>
    /// &lt;
    /// </summary>
    LessThan,

    /// <summary>
    /// &gt;
    /// </summary>
    GreaterThan,
    
    /// <summary>
    /// &lt;=
    /// </summary>
    LessThanEquals,

    /// <summary>
    /// &gt;=
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
    From,
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
    Break,
    Continue,
    This,
    Extends,
    Throw,
    /// <summary>
    /// mixin
    /// </summary>
    Mixin,
    /// <summary>
    /// with
    /// </summary>
    With,
    /// <summary>
    /// static
    /// </summary>
    Static,
    /// <summary>
    /// public
    /// </summary>
    Public,
    /// <summary>
    /// private
    /// </summary>
    Private,
    /// <summary>
    /// native
    /// </summary>
    Native
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
    From,
    In,
    Elif,
    Else,
    Case,
    Default,
    Return,
    As,
    Break,
    Continue,
    Try,
    Catch,
    Finally,
    And,
    Or,
    Xor,
    This,
    Extends,
    Mixin,
    With,
    Static,
    Public,
    Private,
    Throw,
    Native
}

