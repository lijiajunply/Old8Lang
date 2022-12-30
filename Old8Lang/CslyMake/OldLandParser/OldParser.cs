using Old8Lang.CslyMake.OldExpression;
using sly.lexer;
using sly.parser.generator;
using sly.parser.parser;

namespace Old8Lang.CslyMake.OldLandParser;

[ParserRoot("root")]
public class OldParser
{
    #region root and statement 

    [Production("root: statement*")]
    public OldLangTree Root(List<OldStatement> statement) => new OldBlock(statement);

    [Production("statement: statement ANNOTATION")]
    public OldLangTree Annotation(OldStatement statement, Token<OldTokenGeneric> a) => statement;

    [Production("statement: LPAREN statement RPAREN")]
    public OldLangTree BOLCK1(Token<OldTokenGeneric> l, OldLangTree statement, Token<OldTokenGeneric> r) =>
        statement as OldStatement;

    [Production("statement: IDENTIFIER SET OldLangParser_expressions")]
    public OldLangTree Set(Token<OldTokenGeneric> a , OldID id,Token<OldTokenGeneric> b, OldExpr value) => new OldSet(id, value);

    #endregion

    #region expr

    [Operation((int) OldTokenGeneric.LESSER, Affix.InFix, Associativity.Right, 50)]
    [Operation((int) OldTokenGeneric.GREATER, Affix.InFix, Associativity.Right, 50)]
    [Operation((int) OldTokenGeneric.EQUALS, Affix.InFix, Associativity.Right, 50)]
    [Operation((int) OldTokenGeneric.DIFFERENT, Affix.InFix, Associativity.Right, 50)]
    public OldLangTree binaryComparisonExpression(OldLangTree left, Token<OldTokenGeneric> operatorToken,
        OldLangTree right) => new BinaryOperation(left as OldExpr, operatorToken.TokenID, right as OldExpr);

    [Operation((int)OldTokenGeneric.CONCAT, Affix.InFix, Associativity.Right, 100)]
    public OldLangTree DotExpr(OldLangTree left, Token<OldTokenGeneric> oper, OldLangTree right) =>
        new BinaryOperation(left as OldExpr, oper.TokenID, right as OldExpr);

    [Operation((int)OldTokenGeneric.PLUS, Affix.InFix, Associativity.Right, 20)]
    [Operation((int)OldTokenGeneric.MINUS, Affix.InFix, Associativity.Right, 20)]
    public OldLangTree BE1(OldLangTree left, Token<OldTokenGeneric> oper, OldLangTree right) =>
        new BinaryOperation(left as OldExpr, oper.TokenID, right as OldExpr);
    
    [Operation((int)OldTokenGeneric.TIMES, Affix.InFix, Associativity.Right, 70)]
    [Operation((int)OldTokenGeneric.DIVIDE, Affix.InFix, Associativity.Right, 70)]
    public OldLangTree BE2(OldLangTree left, Token<OldTokenGeneric> oper, OldLangTree right) =>
        new BinaryOperation(left as OldExpr, oper.TokenID, right as OldExpr);

    [Operation((int)OldTokenGeneric.AND, Affix.InFix, Associativity.Right, 50)]
    [Operation((int)OldTokenGeneric.OR, Affix.InFix, Associativity.Right, 50)]
    [Operation((int)OldTokenGeneric.XOR, Affix.InFix, Associativity.Right, 50)]
    public OldLangTree Bool1(OldExpr left, Token<OldTokenGeneric> oper, OldExpr right) =>
        new BinaryOperation(left, oper.TokenID, right);

    [Operation((int)OldTokenGeneric.NOT, Affix.PreFix, Associativity.Right, 100)]
    public OldLangTree Bool2(Token<OldTokenGeneric> oper, OldExpr expr) =>
        new BinaryOperation(null, oper.TokenID, expr);

    [Operation((int)OldTokenGeneric.MINUS, Affix.PreFix, Associativity.Right, 100)]
    public OldLangTree MINUS(Token<OldTokenGeneric> oper, OldExpr expr) =>
        new BinaryOperation(null, oper.TokenID, expr);


    #endregion

    #region primany

    [Operand]
    [Production("operand: primary")]
    public OldLangTree Operand(OldLangTree prim) => prim;

    [Production("primary: LPAREN primary RPAREN")]
    public OldLangTree LR(Token<OldTokenGeneric> l, OldLangTree prim, Token<OldTokenGeneric> r) =>
        prim as OldExpr;

    [Production("primary: STRING")]
    public OldLangTree STRING(Token<OldTokenGeneric> token) => new OldString(token.Value);

    [Production("primary: INT")]
    public OldLangTree INT(Token<OldTokenGeneric> token) => new OldInt(token.IntValue);

    [Production("primary: CHAR")]
    public OldLangTree CHAR(Token<OldTokenGeneric> token) => new OldChar(token.Value[0]);

    [Production("primary: DOUBLE")]
    public OldLangTree DOUBLE(Token<OldTokenGeneric> token) => new OldDouble(double.Parse(token.Value));

    [Production("primary: OldLangParser_expressions")]
    public OldLangTree Bool(OldLangTree expr)
    {
        var ex = expr as BinaryOperation;
        return new OldBool(ex);
    }

    [Production("primary: IDENTIFIER")]
    public OldLangTree IDENTIFIER(Token<OldTokenGeneric> id) => new OldID(id.Value);

    [Production("primary: TRUE")]
    public OldLangTree BoolTrue(Token<OldTokenGeneric> token) => new OldBool(true);

    [Production("primary: FALSE")]
    public OldLangTree BoolFalse(Token<OldTokenGeneric> token) => new OldBool(false);
    
    #endregion

    #region yuju

    [Production("statement : IF ifblock (ELIF ifblock)* (ELSE  block)?")]
    public OldLangTree IF(Token<OldTokenGeneric> v, OldIf ifBlock, List<Group<OldTokenGeneric,OldIf>> elif,ValueOption<Group<OldTokenGeneric,OldBlock>> Else)
    {
        var eGrp = Else.Match(x => x, () => null);
        var elseBlock = eGrp?.Value(0);
        List<OldIf> a = new List<OldIf>();
        foreach (var VARIABLE in elif)
        {
            a.Add(VARIABLE.Value(0));
        }
        return new OldIf_Elif_Else(ifBlock, a, elseBlock);
    }

    [Production("ifblock: OldLangParser_expressions block")]
    public OldLangTree IFBLOCK(BinaryOperation binaryOperation, OldBlock block) => new OldIf(binaryOperation, block);

    [Production("block: INDENT[d] statement* UINDENT[d]")]
    public OldLangTree Block(List<OldStatement> statements) => new OldBlock(statements);

    [Production("statement: FOR set , OldLangParser_expressions , statement  block")]
    public OldLangTree FOR(Token<OldTokenGeneric> a, OldSet set, BinaryOperation expr, OldStatement statement, OldBlock block) =>
        new OldFor(set, expr, statement, block);

    [Production("statement: WHILE expr : block")]
    public OldLangTree WHILE(Token<OldTokenGeneric> a, BinaryOperation expr, OldBlock block) => new OldWhile(expr, block);

    [Production("statement: IDENTIFIER DIRECT IDENTIFIER")]
    public OldLangTree DIRECT(OldID id1,Token<OldTokenGeneric> a, OldID id2) => new OldDirect(id1, id2);

    [Production("statement: FUNC IDENTFIER LPAREN RPAREN block")]
    public OldLangTree STAT_FUNC(Token<OldTokenGeneric> a, OldID id, Token<OldTokenGeneric> l, OldBlock block) => new OldFunc(id,block);

    [Production("statement: CLASS IDENTFIER set*")]
    public OldLangTree CLASS(Token<OldTokenGeneric> a , OldID id, List<OldSet> sets)
    {
        Dictionary<OldID, OldExpr> c = new Dictionary<OldID, OldExpr>();
        foreach (var VARIABLE in sets)
        {
            c.Add(VARIABLE.Id,VARIABLE.Value);
        }
        return new OldAny(id, c);
    }

    [Production("statement: IDENTFIER SET IDENTFIER LPAREN RPAREN")]
    public OldLangTree INSTANTIATE(OldID id, Token<OldTokenGeneric> a, OldID otherid, Token<OldTokenGeneric> b,
        Token<OldTokenGeneric> c) => new OldSet(id, otherid);

    #endregion
}