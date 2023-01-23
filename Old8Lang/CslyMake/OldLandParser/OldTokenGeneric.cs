using sly.lexer;

namespace Old8Lang.CslyMake.OldLandParser;

[Lexer(IndentationAWare = true)]
public enum OldTokenGeneric
{
    #region keywords 0 -> 19

        [Keyword("IF")] [Keyword(   "if")]     IF     = 1,
        [Keyword("ELIF")] [Keyword( "elif")]   ELIF   = 2,
        [Keyword("ELSE")] [Keyword( "else")]   ELSE   = 3,
        [Keyword("WHILE")] [Keyword("while")]  WHILE  = 4,
        [Keyword("FOR")] [Keyword(  "for")]    FOR    = 5,
        [Keyword("TRUE")] [Keyword( "true")]   TRUE   = 6,
        [Keyword("FALSE")] [Keyword("false")]  FALSE  = 7,
        [Keyword("NOT")] [Keyword(  "not")]    NOT    = 8,
        [Keyword("AND")] [Keyword(  "and")]    AND    = 9,
        [Keyword("OR")] [Keyword(   "or")]     OR     = 10,
        [Keyword("XOR")][Keyword(   "xor")]    XOR    = 11,
        [Keyword("CLASS")][Keyword( "class")]  CLASS  = 14,
        [Keyword("FUNC")][Keyword(  "func")]   FUNC   = 15,
        [Keyword("RETURN")][Keyword("return")] RETURN = 16,
        [Keyword("IMPORT")][Keyword("import")] IMPORT = 17,
        [Comment("//","|*","*|")]
        COMMENT = 18,
        [Keyword("NEW")][Keyword("new")]NEW = 19,
        #endregion

        #region literals 20 -> 29
        [AlphaId]                    IDENTFIER = 20,
        [String()]                   STRING    = 21,
        [Int]                        INT       = 22,
        [Double]                     DOUBLE    = 23,
        [Lexeme(GenericToken.Char)]  CHAR      = 24,
        #endregion

        #region operators 30 -> 49
        
        
        [Sugar(">")]  GREATER    = 30,
        [Sugar("<")]  LESSER     = 31,
        [Sugar("==")] EQUALS     = 32,
        [Sugar("!=")] DIFFERENT  = 33,
        [Sugar(".")]  CONCAT     = 34,
        [Sugar("-*")] DIRECT     = 35,
        [Sugar("*-")] DIS_DIRECT = 43,
        [Sugar("+")]  PLUS       = 36,
        [Sugar("-")]  MINUS      = 37,
        [Sugar("*")]  TIMES      = 38,
        [Sugar("/")]  DIVIDE     = 39,
        [Sugar("<-")] SET        = 40,
        [Sugar("->")] DIS_SET    = 41,
        [Sugar(",")]  DOUHAO     = 42,

        #endregion

        #region sugar 50 ->

        [Sugar("(")] LPAREN    = 50,
        [Sugar(")")] RPAREN    = 51,
        [Sugar("[")] L_BRACKET = 52,
        [Sugar("]")] R_BRACKET = 53,

        EOF = 0

        #endregion
}
