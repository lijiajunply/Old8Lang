namespace Old8Lang.AST.Expression;

public enum OperationType
{
    #region keywords 0 -> 99

    IF = 1,
    ELIF = 2,
    ELSE = 3,
    WHILE = 4,
    FOR = 5,
    TRUE = 6,
    FALSE = 7,
    NOT = 8,
    AND = 9,
    OR = 10,
    XOR = 11,
    CLASS = 14,
    FUNC = 15,
    RETURN = 16,
    IMPORT = 17,
    NEW = 19,
    PASS = 20,
    AS = 21,
    SWITCH = 22,
    CASE = 23,
    DEFAULT = 24,
    IN = 25,

    #endregion

    #region literals 100 -> 199

    IDENTIFIER = 100,
    STRING = 101,
    INT = 102,
    DOUBLE = 103,
    CHAR = 104,

    #endregion

    #region operators 200 -> 299

    GREATER = 200,
    LESSER = 201,
    EQUALS = 202,
    DIFFERENT = 203,
    CONCAT = 204,
    DIRECT = 205,
    DIS_DIRECT = 206,
    PLUS = 207,
    MINUS = 208,
    TIMES = 209,
    DIVIDE = 210,
    MODULO = 211,
    POWER = 212,
    SET = 213,
    DIS_SET = 214,
    COMMA = 215,
    LESS_EQUAL = 216,
    GREATER_EQUAL = 217,

    #endregion

    #region sugar 300 -> 399

    LPAREN = 300,
    RPAREN = 301,
    L_BRACKET = 302,
    R_BRACKET = 303,
    L_BRACES = 304,
    R_BRACES = 305,
    LAMBDA = 306,
    COLON = 307,
    WAVY = 308,
    QUOTES = 309,
    DOLLAR = 310,

    EOF = 0

    #endregion
}