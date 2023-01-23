using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using sly.lexer;
using sly.parser.generator;
using sly.parser.parser;

namespace Old8Lang.OldLandParser;

[ParserRoot("root")]
public class OldParser
{
    #region root and statement

    [Production("root: statement*")]
    public OldLangTree Root(List<OldLangTree> statement) => new BlockStatement(statement);

    [Production("statement: LPAREN[d] statement RPAREN[d]")]
    public OldLangTree LrBlock(OldLangTree statement) => statement;

    #endregion

    #region expr

    [Operation((int)OldTokenGeneric.LESSER,   Affix.InFix,Associativity.Right,10)]
    [Operation((int)OldTokenGeneric.GREATER,  Affix.InFix,Associativity.Right,10)]
    [Operation((int)OldTokenGeneric.EQUALS,   Affix.InFix,Associativity.Right,10)]
    [Operation((int)OldTokenGeneric.DIFFERENT,Affix.InFix,Associativity.Right,10)]
    public OldLangTree BinaryExpression(OldLangTree left,Token<OldTokenGeneric> operatorToken,
                                        OldLangTree right) =>
        new Operation(left as OldExpr,operatorToken.TokenID,right as OldExpr);

    [Operation((int)OldTokenGeneric.CONCAT,Affix.InFix,Associativity.Right,100)]
    public OldLangTree DotExpr(OldLangTree left,Token<OldTokenGeneric> oper,OldLangTree right) =>
        new Operation(left as OldExpr,oper.TokenID,right as OldExpr);

    [Operation((int)OldTokenGeneric.PLUS, Affix.InFix,Associativity.Right,20)]
    [Operation((int)OldTokenGeneric.MINUS,Affix.InFix,Associativity.Right,20)]
    public OldLangTree NumberOper1(OldLangTree left,Token<OldTokenGeneric> oper,OldLangTree right) =>
        new Operation(left as OldExpr,oper.TokenID,right as OldExpr);

    [Operation((int)OldTokenGeneric.TIMES, Affix.InFix,Associativity.Right,70)]
    [Operation((int)OldTokenGeneric.DIVIDE,Affix.InFix,Associativity.Right,70)]
    public OldLangTree NumBerOper2(OldLangTree left,Token<OldTokenGeneric> oper,OldLangTree right) =>
        new Operation(left as OldExpr,oper.TokenID,right as OldExpr);

    [Operation((int)OldTokenGeneric.AND,Affix.InFix,Associativity.Right,10)]
    [Operation((int)OldTokenGeneric.OR, Affix.InFix,Associativity.Right,10)]
    [Operation((int)OldTokenGeneric.XOR,Affix.InFix,Associativity.Right,10)]
    public OldLangTree BoolOper(OldExpr left,Token<OldTokenGeneric> oper,OldExpr right) =>
        new Operation(left,oper.TokenID,right);

    [Operation((int)OldTokenGeneric.NOT,Affix.PreFix,Associativity.Right,100)]
    public OldLangTree NotBool(Token<OldTokenGeneric> oper,OldExpr expr) =>
        new Operation(null,oper.TokenID,expr);

    [Operation((int)OldTokenGeneric.MINUS,Affix.PreFix,Associativity.Right,100)]
    public OldLangTree MINUS(Token<OldTokenGeneric> oper,OldExpr expr) =>
        new Operation(null,oper.TokenID,expr);

    #endregion

    #region primany

    [Operand]
    [Production("operand: primary")]
    public OldLangTree Operand(OldLangTree prim) => prim;

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
    public OldLangTree ListInit(Token<OldTokenGeneric> id,OldExpr a) =>
        new OldItem(new OldID(id.Value),a);

    [Production("primary: IDENTFIER LPAREN[d] OldParser_expressions* RPAREN[d]")]
    public OldLangTree Instantiate(Token<OldTokenGeneric> id,List<OldLangTree> ids)
    {
        List<OldExpr> IDs = new List<OldExpr>();
        ids.ForEach(x =>
                    {
                        if (x is OldExpr) IDs.Add(x as OldExpr);
                    });
        return new OldInstance(new OldID(id.Value),IDs);
    }
    [Production("primary: func")]
    public OldLangTree FuncValue(OldLangTree a)
    {
        var b = a as FuncInit;
        return b.Func;
    }

    [Production("primary: TYPE[d] LPAREN[d] IDENTFIER RPAREN[d]")]
    public OldLangTree TypeTree(Token<OldTokenGeneric> id) => new OldType(new OldID(id.Value));

    [Production("primary: DOUHAO")]
    public OldLangTree DouhaoTree(Token<OldTokenGeneric> _) => new OldInt(0);

    [Production("primary: L_BRACES[d] OldParser_expressions* R_BRACES[d]")]
    public OldLangTree List(List<OldLangTree> list)
    {
        List<OldExpr> a = new List<OldExpr>();
        list.ForEach(x => a.Add(x as OldExpr));
        return new OldList(a);
    }

    [Production("primary: LPAREN[d] OldParser_expressions* RPAREN[d]")]
    public OldLangTree Array(List<OldLangTree> list) => null;

    #endregion

    #region statement

    [Production("setid: IDENTFIER")]
    public OldLangTree SetId_ID(Token<OldTokenGeneric> a) => new OldID(a.Value);
    
    [Production("setid: IDENTFIER CONCAT[d] IDENTFIER")]
    
    [Production("statement: IDENTFIER CONCAT[d] IDENTFIER SET[d] OldParser_expressions")]
    public OldLangTree ClassSetTree(Token<OldTokenGeneric> classid,Token<OldTokenGeneric> aid,OldExpr expr) =>
        new OtherVariateChanging(new OldID(classid.Value),new OldID(aid.Value),expr);

    [Production("statement: RETURN[d] OldParser_expressions")]
    public OldLangTree ReturnTree(OldExpr expr) => new ReturnStatement(expr);

    [Production("statement: IDENTFIER DIRECT[d] IDENTFIER")]
    public OldLangTree DirectTree(Token<OldTokenGeneric> id1,Token<OldTokenGeneric> id2) =>
        new DirectStatement(new OldID(id1.Value),new OldID(id2.Value));

    [Production("statement: set")]
    public OldLangTree SetTree(SetStatement a) => a;

    [Production("set: IDENTFIER SET[d] OldParser_expressions")]
    public OldLangTree Set(Token<OldTokenGeneric> id,OldExpr value) => new SetStatement(new OldID(id.Value),value);

    [Production("set: OldParser_expressions DIS_SET[d] IDENTFIER")]
    public OldLangTree DIS_SET(OldExpr value,Token<OldTokenGeneric> id) => new SetStatement(new OldID(id.Value),value);

    [Production("statement : IF[d] ifblock (ELIF[d] ifblock)* (ELSE[d] block)?")]
    public OldLangTree IfTree(OldIf ifBlock,List<Group<OldTokenGeneric,OldLangTree>> elif,
                          ValueOption<Group<OldTokenGeneric,OldLangTree>> Else)
    {
        var eGrp = Else.Match(
                              x => { return x; },() => { return null; });
        var elseBlock = eGrp?.Value(0) as BlockStatement;
        var a         = elif.Select(x => x.Value(0) as OldIf).ToList();
        return new IfStatement(ifBlock,a,elseBlock);
    }

    [Production("ifblock: OldParser_expressions block")]
    public OldLangTree IfBlock(OldExpr expr,BlockStatement blockStatement) => new OldIf(expr,blockStatement);

    [Production("block: INDENT[d] statement* UINDENT[d]")]
    public OldLangTree Block(List<OldLangTree> statements) => new BlockStatement(statements);


    [Production("statement: FOR[d] set DOUHAO[d] OldParser_expressions DOUHAO[d] statement  block")]
    public OldLangTree For(SetStatement   setStatement,Operation expr,OldStatement statement,
                           BlockStatement blockStatement) =>
        new ForStatement(setStatement,expr,statement,blockStatement);

    [Production("statement: WHILE[d] OldParser_expressions block")]
    public OldLangTree While(OldExpr expr,BlockStatement blockStatement) => new WhileStatement(expr,blockStatement);


    [Production("statement: func")]
    public OldLangTree FuncTree(OldLangTree a) => a;

    [Production("func: IDENTFIER LPAREN[d] IDENTFIER* RPAREN[d] DIS_SET[d] block")]
    [Production("func: FUNC[d] IDENTFIER LPAREN[d] IDENTFIER* RPAREN[d] block")]
    public OldLangTree Func(Token<OldTokenGeneric> id,List<Token<OldTokenGeneric>> a,BlockStatement blockStatement)
    {
        var b = new List<OldID>();
        a.ForEach(x => b.Add(new OldID(x.Value)));
        return new FuncInit(new OldFunc(new OldID(id.Value),b,blockStatement));
    }

    [Production("func: IDENTFIER LPAREN[d] IDENTFIER* RPAREN[d] LAMBDA[d] OldParser_expressions")]
    public OldLangTree LambdaFunc(Token<OldTokenGeneric> id,List<Token<OldTokenGeneric>> ids,OldExpr asdf)
    {
        List<OldLangTree> retrunState = new List<OldLangTree>() { new ReturnStatement(asdf) };
        List<OldID>            a      = new List<OldID>();
        ids.ForEach(x => a.Add(new OldID(x.Value)));
        return new FuncInit(new OldFunc(new OldID(id.Value),a,new BlockStatement(retrunState)));
    }

    [Production("statement: CLASS[d] IDENTFIER block")]
    public OldLangTree Class(Token<OldTokenGeneric> id,BlockStatement statements)
    {
        var c = new Dictionary<OldID,OldExpr>();
        statements.Statements.ForEach(x => c.Add(GetTuple(x).id,GetTuple(x).Expr));
        return new OldClassInit(new OldAny(new OldID(id.Value),c));
    }

    [Production("statement: IMPORT[d] IDENTFIER")]
    public OldLangTree ImportTree(Token<OldTokenGeneric> import) => new ImportStatement(import.Value);

    [Production("statement: L_BRACKET[d] IMPORT[d] STRING IDENTFIER IDENTFIER IDENTFIER? R_BRACKET[d]")]
    public OldLangTree NativeTree(Token<OldTokenGeneric> DllName,   Token<OldTokenGeneric>  ClassName,
                              Token<OldTokenGeneric> MethodName,Token<OldTokenGeneric>? token) =>
        new NativeStatement(DllName.Value,ClassName.Value,MethodName.Value,token.Value);

    #endregion

    #region Func

    private (OldID id,OldExpr Expr) GetTuple(OldLangTree a)
    {
        if (a is SetStatement)
        {
            var b = a as SetStatement;
            return (id: b.Id,Expr: b.Value);
        }
        if (a is FuncInit)
        {
            var b = a as FuncInit;
            return (b.Func.ID,b.Func);
        }
        return (null,null);
    }

    #endregion
    
    
}