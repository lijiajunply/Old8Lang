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
    public OldLangTree Root(List<OldLangTree> statement) => new BlockStatement(statement);
    
    [Production("statement: LPAREN[d] statement RPAREN[d]")]
    public OldLangTree BOLCK(OldLangTree statement) => statement as OldStatement;
    
    
    #endregion

    #region expr

    [Operation((int) OldTokenGeneric.LESSER, Affix.InFix, Associativity.Right, 50)]
    [Operation((int) OldTokenGeneric.GREATER, Affix.InFix, Associativity.Right, 50)]
    [Operation((int) OldTokenGeneric.EQUALS, Affix.InFix, Associativity.Right, 50)]
    [Operation((int) OldTokenGeneric.DIFFERENT, Affix.InFix, Associativity.Right, 50)]
    public OldLangTree BinaryExpression(OldLangTree left, Token<OldTokenGeneric> operatorToken,
        OldLangTree right) => new BinaryOperation(left as OldExpr, operatorToken.TokenID, right as OldExpr);

    [Operation((int)OldTokenGeneric.CONCAT, Affix.InFix, Associativity.Right, 100)]
    public OldLangTree DotExpr(OldLangTree left, Token<OldTokenGeneric> oper, OldLangTree right) =>
        new BinaryOperation(left as OldExpr, oper.TokenID, right as OldExpr);

    [Operation((int)OldTokenGeneric.PLUS, Affix.InFix, Associativity.Right, 20)]
    [Operation((int)OldTokenGeneric.MINUS, Affix.InFix, Associativity.Right, 20)]
    public OldLangTree NumberOper1(OldLangTree left, Token<OldTokenGeneric> oper, OldLangTree right) =>
        new BinaryOperation(left as OldExpr, oper.TokenID, right as OldExpr);
    
    [Operation((int)OldTokenGeneric.TIMES, Affix.InFix, Associativity.Right, 70)]
    [Operation((int)OldTokenGeneric.DIVIDE, Affix.InFix, Associativity.Right, 70)]
    public OldLangTree NumBerOper2(OldLangTree left, Token<OldTokenGeneric> oper, OldLangTree right) =>
        new BinaryOperation(left as OldExpr, oper.TokenID, right as OldExpr);

    [Operation((int)OldTokenGeneric.AND, Affix.InFix, Associativity.Right, 50)]
    [Operation((int)OldTokenGeneric.OR, Affix.InFix, Associativity.Right, 50)]
    [Operation((int)OldTokenGeneric.XOR, Affix.InFix, Associativity.Right, 50)]
    public OldLangTree BoolOper(OldExpr left, Token<OldTokenGeneric> oper, OldExpr right) =>
        new BinaryOperation(left, oper.TokenID, right);

    [Operation((int)OldTokenGeneric.NOT, Affix.PreFix, Associativity.Right, 100)]
    public OldLangTree NotBool(Token<OldTokenGeneric> oper, OldExpr expr) =>
        new BinaryOperation(null, oper.TokenID, expr);

    [Operation((int)OldTokenGeneric.MINUS, Affix.PreFix, Associativity.Right, 100)]
    public OldLangTree MINUS(Token<OldTokenGeneric> oper, OldExpr expr) =>
        new BinaryOperation(null, oper.TokenID, expr);


    #endregion

    #region primany

    [Operand]
    [Production("operand: primary")]
    public OldLangTree Operand(OldLangTree prim) => prim;

    [Production("primary: LPAREN[d] primary RPAREN[d]")]
    public OldLangTree LR(OldLangTree prim) => prim as OldExpr;

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

    [Production("primary: IDENTFIER L_BRACKET[d] OldParser_expressions R_BRACKET[d]")]
    public OldLangTree LIST(Token<OldTokenGeneric> id, OldExpr a) =>
        new OldItem(new OldID(id.Value), a);

    [Production("primary: IDENTFIER LPAREN[d] IDENTFIER* RPAREN[d]")]
    public OldLangTree INSTANTIATE(Token<OldTokenGeneric> id, List<Token<OldTokenGeneric>> ids)
    {
        List<OldID> IDs = new List<OldID>();
        foreach (var VARIABLE in ids)
        {
            IDs.Add(new OldID(VARIABLE.Value));
        }

        return new OldInstance(new OldID(id.Value), IDs);
    }

    [Production("primary: DOUHAO")]
    public OldLangTree DOUHAO(Token<OldTokenGeneric> a) => new OldInt(0);
    #endregion

    #region yuju
    
    [Production("statement: RETURN[d] OldParser_expressions")]
    public OldLangTree RETURN(OldExpr expr) => new ReturnStatement(expr);
    
    [Production("statement: IDENTFIER DIRECT[d] IDENTFIER")]
    public OldLangTree DIRECT(Token<OldTokenGeneric> id1,Token<OldTokenGeneric> id2) => 
        new DirectStatement(new OldID(id1.Value), new OldID(id2.Value));

    [Production("statement: IDENTFIER SET[d] '{'[d] OldParser_expressions* '}'[d] ")]
    public OldLangTree LIST(Token<OldTokenGeneric> id, List<OldLangTree> exprs) =>
        new ListInitStatement(new OldID(id.Value), exprs);

    [Production("statement: set")]
    public OldLangTree SET(SetStatement a) => a;
    
    [Production("set: IDENTFIER SET[d] OldParser_expressions")]
    public OldLangTree Set(Token<OldTokenGeneric> id, OldExpr value) => new SetStatement(new OldID(id.Value), value);

    [Production("set: OldParser_expressions DIS_SET[d] IDENTFIER")]
    public OldLangTree DIS_SET(OldExpr value, Token<OldTokenGeneric> id) => new SetStatement(new OldID(id.Value), value);

    [Production("statement : IF[d] ifblock (ELIF ifblock)* (ELSE  block)?")]
    public OldLangTree IF( OldIf ifBlock, List<Group<OldTokenGeneric,OldLangTree>> elif,ValueOption<Group<OldTokenGeneric,OldLangTree>> Else)
    {
        var eGrp = Else.Match(x => x, () => null);
        var elseBlock = eGrp?.Value(0) as BlockStatement;
        var a = elif.Select(x => x.Value(0) as OldIf).ToList();
        return new IfStatement(ifBlock, a, elseBlock);
    }

    [Production("ifblock: OldParser_expressions block")]
    public OldLangTree IFBLOCK(OldExpr expr, BlockStatement blockStatement) => new OldIf(expr, blockStatement);

    [Production("block: INDENT[d] statement* UINDENT[d]")]
    public OldLangTree Block(List<OldLangTree> statements) => new BlockStatement(statements);

    
    [Production("statement: FOR[d] set DOUHAO[d] OldParser_expressions DOUHAO[d] statement  block")]
    public OldLangTree For(SetStatement setStatement, BinaryOperation expr, OldStatement statement, BlockStatement blockStatement) =>
        new ForStatement(setStatement, expr, statement, blockStatement);

    [Production("statement: WHILE[d] OldParser_expressions block")]
    public OldLangTree While(OldExpr expr, BlockStatement blockStatement) => new WhileStatement(expr, blockStatement);


    [Production("statement: func")]
    public OldLangTree FUNC(OldLangTree a) => a;
    
    [Production("func: IDENTFIER LPAREN[d] IDENTFIER* RPAREN[d] DIS_SET[d] block")]

    [Production("func: FUNC[d] IDENTFIER LPAREN[d] IDENTFIER* RPAREN[d] block")]
    public OldLangTree Func(Token<OldTokenGeneric> id, List<Token<OldTokenGeneric>> a,BlockStatement blockStatement)
    {
        var b = new List<OldID>();
        foreach (var VARIABLE in a)
        {
            b.Add(new OldID(VARIABLE.Value));
        }
        return new FuncInit( new OldFunc(new OldID(id.Value),b,blockStatement));
    }
    
    [Production("statement: CLASS[d] IDENTFIER class")]
    public OldLangTree Class( Token<OldTokenGeneric> id, ClassInfoStatement statement)
    {
        Dictionary<OldID, OldExpr> c = new Dictionary<OldID, OldExpr>();
        foreach (var VARIABLE in statement.Values)
        {
            if (VARIABLE is OldFunc)
            {
                var a = VARIABLE as OldFunc;
                c.Add(a.ID,a);
            }

            if (VARIABLE is SetStatement)
            {
                var a = VARIABLE as SetStatement;
                c.Add(a.Id,a.Value);
            }
        }
        return new OldClassInit(new OldAny(new OldID(id.Value), c));
    }

    [Production("class: INDENT[d] classinfo* UINDENT[d]")]
    public OldLangTree ClassInfo(List<OldLangTree> a) => new ClassInfoStatement(a);

    [Production("classinfo: set")]
    public OldLangTree ClassInfo_Set(SetStatement a) => a;

    [Production("classinfo: FUNC[d] IDENTFIER LPAREN[d] IDENTFIER* RPAREN[d] block ")]
    public OldLangTree ClassInfo_Func(Token<OldTokenGeneric> id, List<Token<OldTokenGeneric>> a, BlockStatement blockStatement)
    {
        var b = new List<OldID>();
        foreach (var VARIABLE in a)
        {
            b.Add(new OldID(VARIABLE.Value));
        }
        return new FuncInit( new OldFunc(new OldID(id.Value),b,blockStatement));
    }
    

    [Production("statement: IMPORT[d] IDENTFIER")]
    public OldLangTree IMPORT(Token<OldTokenGeneric> import) => new ImportStatement(import.Value);

    [Production("statement: L_BRACKET[d] IMPORT[d] STRING IDENTFIER IDENTFIER IDENTFIER? R_BRACKET[d]")]
    public OldLangTree NATIVE(Token<OldTokenGeneric> DLLName, Token<OldTokenGeneric> ClassName,
        Token<OldTokenGeneric> MethodName, Token<OldTokenGeneric>? token) =>
        new NativeStatement(DLLName.Value, ClassName.Value, MethodName.Value,token.Value);

    #endregion
}