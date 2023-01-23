using sly.lexer;

namespace Old8Lang.OldLandParser;

[Lexer(IndentationAWare = true)]
public enum OldTokenGeneric
{
        #region keywords 0 -> 99
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
        [Keyword("TYPE")][Keyword("type")]TYPE = 20,
        #endregion

        #region literals 100~199
        [AlphaId]                    IDENTFIER = 100,
        [String()]                   STRING    = 101,
        [Int]                        INT       = 102,
        [Double]                     DOUBLE    = 103,
        [Lexeme(GenericToken.Char)]  CHAR      = 104,
        #endregion

        #region operators 200 -> 299
        
        
        [Sugar(">")]  GREATER    = 200,
        [Sugar("<")]  LESSER     = 201,
        [Sugar("==")] EQUALS     = 202,
        [Sugar("!=")] DIFFERENT  = 203,
        [Sugar(".")]  CONCAT     = 204,
        [Sugar("-*")] DIRECT     = 205,
        [Sugar("*-")] DIS_DIRECT = 206,
        [Sugar("+")]  PLUS       = 207,
        [Sugar("-")]  MINUS      = 208,
        [Sugar("*")]  TIMES      = 209,
        [Sugar("/")]  DIVIDE     = 210,
        [Sugar("<-")] SET        = 211,
        [Sugar("->")] DIS_SET    = 212,
        [Sugar(",")]  DOUHAO     = 213,

        #endregion

        #region sugar 300 -> 399

        [Sugar("(")]  LPAREN    = 300,
        [Sugar(")")]  RPAREN    = 301,
        [Sugar("[")]  L_BRACKET = 302,
        [Sugar("]")]  R_BRACKET = 303,
        [Sugar("{")]  L_BRACES  = 304,
        [Sugar("}")]  R_BRACES  = 305,
        [Sugar("=>")] LAMBDA    = 306,
        [Sugar(":")]  COLON     = 307,

        EOF = 0

        #endregion
}
