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

    /// <summary>
    /// ??
    /// </summary>
    NullishCoalescing,

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
    /// ~&lt; (范围表达式排除右边界)
    /// </summary>
    WavyLessThan,

    /// <summary>
    /// >~ (范围表达式排除左边界)
    /// </summary>
    GreaterThanWavy,

    /// <summary>
    /// &gt;~&lt; (范围表达式排除两边边界)
    /// </summary>
    GreaterThanWavyLessThan,

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
    Then,
    Case,
    Default,
    Return,
    As,
    Is,
    Break,
    Continue,
    This,
    Extends,
    Throw,

    /// <summary>
    /// where
    /// </summary>
    Where,

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
    Native,

    /// <summary>
    /// protected
    /// </summary>
    Protected,

    /// <summary>
    /// async
    /// </summary>
    Async,

    /// <summary>
    /// await
    /// </summary>
    Await,

    /// <summary>
    /// yield
    /// </summary>
    Yield,
    /// <summary>
    /// interface
    /// </summary>
    Interface,
    /// <summary>
    /// implements
    /// </summary>
    Implements,

    /// <summary>
    /// super
    /// </summary>
    Super,

    /// <summary>
    /// abstract
    /// </summary>
    Abstract,

    /// <summary>
    /// lazy
    /// </summary>
    Lazy,

    /// <summary>
    /// dynamic
    /// </summary>
    Dynamic,

    /// <summary>
    /// match
    /// </summary>
    Match,

    /// <summary>
    /// enum
    /// </summary>
    Enum,

    /// <summary>
    /// select (LINQ 查询关键字)
    /// </summary>
    Select,

    /// <summary>
    /// orderby (LINQ 排序关键字)
    /// </summary>
    OrderBy,

    /// <summary>
    /// ascending (LINQ 升序关键字)
    /// </summary>
    Ascending,

    /// <summary>
    /// descending (LINQ 降序关键字)
    /// </summary>
    Descending,

    /// <summary>
    /// group (LINQ 分组关键字)
    /// </summary>
    Group,

    /// <summary>
    /// by (LINQ by 关键字)
    /// </summary>
    By,

    /// <summary>
    /// join (LINQ 连接关键字)
    /// </summary>
    Join,

    /// <summary>
    /// on (LINQ 连接条件关键字)
    /// </summary>
    On,

    /// <summary>
    /// into (LINQ 延续关键字)
    /// </summary>
    Into,

    /// <summary>
    /// let (LINQ 赋值关键字)
    /// </summary>
    Let,

    /// <summary>
    /// params (可变参数关键字)
    /// </summary>
    Params,

    /// <summary>
    /// 文件头指令 (#!...)
    /// </summary>
    FileHeaderDirective,

    /// <summary>
    /// 文档注释 (///...)
    /// </summary>
    DocComment
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
    Then,
    Case,
    Default,
    Return,
    As,
    Is,
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
    Where,
    Mixin,
    With,
    Static,
    Public,
    Private,
    Throw,
    Native,
    Protected,
    Async,
    Await,
    Yield,
    Interface,
    Implements,
    Abstract,
    Lazy,
    Dynamic,
    Super,
    Match,
    Enum,
    Select,
    OrderBy,
    Ascending,
    Descending,
    Group,
    By,
    Join,
    On,
    Into,
    Let,
    Params
}