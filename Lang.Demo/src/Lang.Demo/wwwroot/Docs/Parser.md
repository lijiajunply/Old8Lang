# Parser

在Lexer这篇文章中我们已经构建了Lexer，那么现在就在构建Parser使csly能够分析出语法

我们这里使用EBNF来进行书写，因为csly的EBNF拥有更多的用法

那么我们先讲一下csly的EBNF应该怎么书写：

```csharp
<rule_name> : <terminal or non-terminal> <some other terminal or non-terminal> ...
eg:
statement: IDENTIFIER SET INT
```

这里我们定义了一个statement分句，statement长成这样：标识符 符号（set） 整数

例如，如果我们在OldTokenGeneric中定义了SET为“=”，那么下面这个句子就可以被识别到：

```csharp
a = 1
```

当有很多符号而且还有一些符号不需要我们使用，例如上面的statement语句，SET(即“=”符号我们不需要)就是我们不需要的，因为我们在这里只需要标识符和整数即可。

这时我们就可以在SET后面加上一个[d]表示丢弃，此时csly就只会传入标识符和整数

现在学会了csly中的EBNF表示方法，那么，我们该如何应用呢？

先创建一个类叫：OldParser来表示Parser

在csly中，我们可以在函数前添加修饰器来使用EBNF语句：

```csharp
 [Production("setStatement: IDENTFIER SET[d] OldParser_expressions")]
    public OldLangTree Set( Token<OldTokenGeneric> id, OldExpr value) => new SetStatement(new OldID(id.Value), value);
```

形如：

[Production("`<EBNF语句>`")]

public `<baseclass> <methodname>(<传入的变量>) => <baseclass>;`

就这样，很简单

注意，标识符等需要使用Token `<OldTokenGeneric> 来表示`

该类型有几个属性，我这里讲两个：Value(输出string类型)和ToeknID(传出OldTokenGeneric类型)

而子语法（如前面的OldParser_expressions），可以使用包装好的类型(比如说OldExpr)

现在，我贴出Old8Lang的Parser:

```csharp
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
  
    [Production("statement: setStatement")]
    public OldLangTree SET(SetStatement a) => a;
  
    [Production("setStatement: IDENTFIER SET[d] OldParser_expressions")]
    public OldLangTree Set( Token<OldTokenGeneric> id, OldExpr value) => new SetStatement(new OldID(id.Value), value);

    [Production("setStatement: OldParser_expressions DIS_SET[d] IDENTFIER")]
    public OldLangTree DIS_SET(OldExpr value, Token<OldTokenGeneric> id) => new SetStatement(new OldID(id.Value), value);

    [Production("statement : IF[d] ifblock (ELIF ifblock)* (ELSE  blockStatement)?")]
    public OldLangTree IF( OldIf ifBlock, List<Group<OldTokenGeneric,OldLangTree>> elif,ValueOption<Group<OldTokenGeneric,OldLangTree>> Else)
    {
        var eGrp = Else.Match(x => x, () => null);
        var elseBlock = eGrp?.Value(0) as BlockStatement;
        var a = elif.Select(x => x.Value(0) as OldIf).ToList();
        return new If_Elif_ElseStatement(ifBlock, a, elseBlock);
    }

    [Production("ifblock: OldParser_expressions blockStatement")]
    public OldLangTree IFBLOCK(BinaryOperation binaryOperation, BlockStatement blockStatement) => new OldIf(binaryOperation, blockStatement);

    [Production("blockStatement: INDENT[d] statement* UINDENT[d]")]
    public OldLangTree Block(List<OldLangTree> statements) => new BlockStatement(statements);

    [Production("statement: FOR[d] setStatement DOUHAO[d] OldParser_expressions DOUHAO[d] statement  blockStatement")]
    public OldLangTree FOR(SetStatement setStatement, BinaryOperation expr, OldStatement statement, BlockStatement blockStatement) =>
        new ForStatement(setStatement, expr, statement, blockStatement);

    [Production("statement: WHILE[d] OldParser_expressions blockStatement")]
    public OldLangTree WHILE(BinaryOperation expr, BlockStatement blockStatement) => new WhileStatement(expr, blockStatement);

    [Production("statement: IDENTFIER DIRECT[d] IDENTFIER")]
    public OldLangTree DIRECT(Token<OldTokenGeneric> id1, Token<OldTokenGeneric> id2) => new DirectStatement(new OldID(id1.Value), new OldID(id2.Value));

    [Production("statement: FUNC[d] IDENTFIER LPAREN[d] IDENTFIER* RPAREN[d] blockStatement (INDENT[d] RETURN OldParser_expressions UINDENT[d])?")]
    public OldLangTree STAT_FUNC(Token<OldTokenGeneric> id, List<Token<OldTokenGeneric>> a,BlockStatement blockStatement,
        ValueOption<Group<OldTokenGeneric, OldLangTree>> returnExpr)
    {
        var b = new List<OldID>();
        foreach (var VARIABLE in a)
        {
            b.Add(new OldID(VARIABLE.Value));
        }
        return new FuncInit( new OldFunc(new OldID(id.Value),b,blockStatement,returnExpr.Match(x => x,() => null)?.Value(0) as OldExpr));
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

            if (VARIABLE is SetStatement)
            {
                var a = VARIABLE as SetStatement;
                c.Add(a.Id,a.Value);
            }
        }
        return new OldClassInit(new OldAny(new OldID(id.Value), c));
    }

    [Production("classinfo: setStatement")]
    public OldLangTree ClassInfo_Set(SetStatement a) => a;

    [Production("classinfo: FUNC[d] IDENTFIER LPAREN[d] IDENTFIER* RPAREN[d] blockStatement (RETURN[d] OldParser_expressions)?")]
    public OldLangTree ClassInfo_Func(Token<OldTokenGeneric> id, List<Token<OldTokenGeneric>> a, BlockStatement blockStatement,
        ValueOption<Group<OldTokenGeneric, OldLangTree>> returnExpr)
    {
        var b = new List<OldID>();
        foreach (var VARIABLE in a)
        {
            b.Add(new OldID(VARIABLE.Value));
        }
        return new FuncInit( new OldFunc(new OldID(id.Value),b,blockStatement,returnExpr.Match(x => x,() => null)?.Value(0) as OldExpr));
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
        return new SetStatement(new OldID(id.Value), new OldID(otherid.Value),a);
    }

    [Production("statement: IMPORT[d] IDENTFIER")]
    public OldLangTree IMPORT(Token<OldTokenGeneric> import) => new ImportStatement(import.Value);

    [Production("statement: L_BRACKET[d] IMPORT[d] STRING IDENTFIER IDENTFIER IDENTFIER? R_BRACKET[d]")]
    public OldLangTree NATIVE(Token<OldTokenGeneric> DLLName, Token<OldTokenGeneric> ClassName,
        Token<OldTokenGeneric> MethodName, Token<OldTokenGeneric>? token) =>
        new NativeStatement(DLLName.Value, ClassName.Value, MethodName.Value,token.Value);

    #endregion
}
```

现在，我们写出了Parser，将所有语法都识别并转成了OldLangTree这一基础类。
