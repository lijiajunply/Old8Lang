using sly.lexer;

namespace Old8Lang.CslyMake.OldLandParser;

[Lexer(IndentationAWare = true)]
public enum OldTokenGeneric
{
    #region keywords 0 -> 19

        [Lexeme(GenericToken.KeyWord, "IF")] [Lexeme(GenericToken.KeyWord, "if")]
        IF = 1,

        [Lexeme(GenericToken.KeyWord, "ELIF")] [Lexeme(GenericToken.KeyWord, "elif")]
        ELIF = 2,

        [Lexeme(GenericToken.KeyWord, "ELSE")] [Lexeme(GenericToken.KeyWord, "else")]
        ELSE = 3,

        [Lexeme(GenericToken.KeyWord, "WHILE")] [Lexeme(GenericToken.KeyWord, "while")]
        WHILE = 4,

        [Lexeme(GenericToken.KeyWord, "FOR")] [Lexeme(GenericToken.KeyWord, "for")]
        FOR = 5,
        
        [Lexeme(GenericToken.KeyWord, "TRUE")] [Lexeme(GenericToken.KeyWord, "true")]
        TRUE = 6,

        [Lexeme(GenericToken.KeyWord, "FALSE")] [Lexeme(GenericToken.KeyWord, "false")]
        FALSE = 7,

        [Lexeme(GenericToken.KeyWord, "NOT")] [Lexeme(GenericToken.KeyWord, "not")]
        NOT = 8,

        [Lexeme(GenericToken.KeyWord, "AND")] [Lexeme(GenericToken.KeyWord, "and")]
        AND = 9,
        
        [Lexeme(GenericToken.KeyWord, "OR")] [Lexeme(GenericToken.KeyWord, "or")]
        OR = 10,
        
        [Lexeme(GenericToken.KeyWord,"XOR")][Lexeme(GenericToken.KeyWord,"xor")]
        XOR = 11,
        
        [Lexeme(GenericToken.KeyWord,"CLASS")][Lexeme(GenericToken.KeyWord,"class")]
        CLASS = 14,
        
        [Lexeme(GenericToken.KeyWord,"FUNC")][Lexeme(GenericToken.KeyWord,"func")]
        FUNC = 15,
        
        [Lexeme(GenericToken.KeyWord,"RETURN")][Lexeme(GenericToken.KeyWord,"return")]
        RETURN = 16,
        
        [Lexeme(GenericToken.KeyWord,"IMPORT")][Lexeme(GenericToken.KeyWord,"import")]
        IMPORT = 17,

        [Comment("//","|*","*|")]
        COMMENT = 18,

        #endregion

        #region literals 20 -> 29
        [Lexeme(GenericToken.Identifier)] IDENTFIER = 20,
        [Lexeme(GenericToken.String)] STRING = 21,
        [Lexeme(GenericToken.Int)] INT = 22,
        [Lexeme(GenericToken.Double)] DOUBLE = 23,
        [Lexeme(GenericToken.Char)] CHAR = 24,
        #endregion

        #region operators 30 -> 49
        
        [Sugar("<-")] SET = 40,

        [Sugar("->")] DIS_SET = 41,
        
        [Sugar( ">")] GREATER = 30,

        [Sugar( "<")] LESSER = 31,

        [Sugar( "==")] EQUALS = 32,

        [Sugar( "!=")] DIFFERENT = 33,

        [Sugar( ".")] CONCAT = 34,

        [Sugar( "-*")] DIRECT = 35,
        
        [Sugar("*-")] DIS_DIRECT = 43,

        [Sugar( "+")] PLUS = 36,

        [Sugar( "-")] MINUS = 37,
        
        [Sugar( "*")] TIMES = 38,

        [Sugar( "/")] DIVIDE = 39,
        
        [Sugar( ",")] DOUHAO = 42,

        #endregion

        #region sugar 50 ->

        [Sugar( "(")] LPAREN = 50,

        [Sugar( ")")] RPAREN = 51,
        [Sugar("[")] L_BRACKET = 52,
        [Sugar("]")] R_BRACKET = 53,

        EOF = 0

        #endregion
}
