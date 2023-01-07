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
    public OldLangTree Root(List<OldLangTree> statement) => new OldBlock(statement);
    
    [Production("statement: LPAREN[d] statement RPAREN[d]")]
    public OldLangTree BOLCK1(OldLangTree statement) => statement as OldStatement;
    
    
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

    [Production("primary: IDENTFIER")]
    public OldLangTree IDENTIFIER(Token<OldTokenGeneric> id) => new OldID(id.Value);

    [Production("primary: TRUE")]
    public OldLangTree BoolTrue(Token<OldTokenGeneric> token) => new OldBool(true);

    [Production("primary: FALSE")]
    public OldLangTree BoolFalse(Token<OldTokenGeneric> token) => new OldBool(false);
    
    #endregion

    #region yuju
    
    [Production("statement: set")]
    public OldLangTree SET(OldSet a) => a;
    
    [Production("set: IDENTFIER SET[d] OldParser_expressions")]
    public OldLangTree Set( Token<OldTokenGeneric> id, OldExpr value) => new OldSet(new OldID(id.Value), value);

    [Production("set: OldParser_expressions DIS_SET[d] IDENTFIER")]
    public OldLangTree DIS_SET(OldExpr value, Token<OldTokenGeneric> id) => new OldSet(new OldID(id.Value), value);

    [Production("statement : IF[d] ifblock (ELIF ifblock)* (ELSE  block)?")]
    public OldLangTree IF( OldIf ifBlock, List<Group<OldTokenGeneric,OldLangTree>> elif,ValueOption<Group<OldTokenGeneric,OldLangTree>> Else)
    {
        var eGrp = Else.Match(x => x, () => null);
        var elseBlock = eGrp?.Value(0) as OldBlock;
        var a = elif.Select(x => x.Value(0) as OldIf).ToList();
        return new OldIf_Elif_Else(ifBlock, a, elseBlock);
    }

    [Production("ifblock: OldParser_expressions block")]
    public OldLangTree IFBLOCK(BinaryOperation binaryOperation, OldBlock block) => new OldIf(binaryOperation, block);

    [Production("block: INDENT[d] statement* UINDENT[d]")]
    public OldLangTree Block(List<OldLangTree> statements) => new OldBlock(statements);

    [Production("statement: FOR[d] set OldParser_expressions  statement  block")]
    public OldLangTree FOR(OldSet set, BinaryOperation expr, OldStatement statement, OldBlock block) =>
        new OldFor(set, expr, statement, block);

    [Production("statement: WHILE[d] OldParser_expressions block")]
    public OldLangTree WHILE(BinaryOperation expr, OldBlock block) => new OldWhile(expr, block);

    [Production("statement: IDENTFIER DIRECT[d] IDENTFIER")]
    public OldLangTree DIRECT(Token<OldTokenGeneric> id1, Token<OldTokenGeneric> id2) => new OldDirect(new OldID(id1.Value), new OldID(id2.Value));

    [Production("statement: FUNC[d] IDENTFIER LPAREN[d] IDENTFIER* RPAREN[d] block (RETURN[d] OldParser_expressions)?")]
    public OldLangTree STAT_FUNC(Token<OldTokenGeneric> id, List<Token<OldTokenGeneric>> a,OldBlock block,
        ValueOption<Group<OldTokenGeneric, OldLangTree>> returnExpr)
    {
        var b = new List<OldID>();
        foreach (var VARIABLE in a)
        {
            b.Add(new OldID(VARIABLE.Value));
        }
        return new OldFuncInit( new OldFunc(new OldID(id.Value),b,block,returnExpr.Match(x => x,() => null)?.Value(0) as OldExpr));
    }

    /// <summary>
    /// Class
    /// </summary>
    /// <param name="id">Class Name</param>
    /// <param name="sets">类的属性和方法</param>
    /// <returns></returns>
    [Production("statement: CLASS[d] IDENTFIER classinfo*")]
    public OldLangTree CLASS( Token<OldTokenGeneric> id, List<OldLangTree> sets)
    {
        Dictionary<OldID, OldExpr> c = new Dictionary<OldID, OldExpr>();
        foreach (var VARIABLE in sets)
        {
            if (VARIABLE is OldFunc)
            {
                var a = VARIABLE as OldFunc;
                c.Add(a.ID,a);
            }

            if (VARIABLE is OldSet)
            {
                var a = VARIABLE as OldSet;
                c.Add(a.Id,a.Value);
            }
        }
        return new OldClassInit(new OldAny(new OldID(id.Value), c));
    }

    [Production("classinfo: set")]
    public OldLangTree ClassInfo_Set(OldSet a) => a;

    [Production("classinfo: FUNC[d] IDENTFIER LPAREN[d] IDENTFIER* RPAREN[d] block (RETURN[d] OldParser_expressions)?")]
    public OldLangTree ClassInfo_Func(Token<OldTokenGeneric> id, List<Token<OldTokenGeneric>> a, OldBlock block,
        ValueOption<Group<OldTokenGeneric, OldLangTree>> returnExpr)
    {
        var b = new List<OldID>();
        foreach (var VARIABLE in a)
        {
            b.Add(new OldID(VARIABLE.Value));
        }
        return  new OldFuncInit( new OldFunc(new OldID(id.Value),b,block,returnExpr.Match(x => x,() => null)?.Value(0) as OldExpr));
    }

    /// <summary>
    /// a = A(); 类,函数的初始化
    /// </summary>
    /// <param name="id">a</param>
    /// <param name="otherid">A</param>
    /// <returns></returns>
    [Production("statement: IDENTFIER SET[d] IDENTFIER LPAREN[d] IDENTFIER* RPAREN[d]")]
    public OldLangTree INSTANTIATE(Token<OldTokenGeneric> id, Token<OldTokenGeneric> otherid,
        List<Token<OldTokenGeneric>> valve)
    {
        List<OldID> a = new List<OldID>();
        foreach (var VARIABLE in valve)
        {
            a.Add(new OldID(VARIABLE.Value));
        }
        return new OldSet(new OldID(id.Value), new OldID(otherid.Value),a);
    }

    #endregion
}