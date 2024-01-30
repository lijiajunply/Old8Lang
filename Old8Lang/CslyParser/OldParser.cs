using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using sly.lexer;
using sly.parser.generator;
using sly.parser.parser;

namespace Old8Lang.CslyParser;

[ParserRoot("root")]
public class OldParser
{
    #region root and statement

    [Production("root: statement*")]
    public OldLangTree Root(List<OldLangTree> statement) => new BlockStatement(statement);

    [Production("statement: LPAREN[d] statement RPAREN[d]")]
    public OldLangTree LrBlock(OldStatement statement) => statement;

    #endregion

    #region expr

    [Operation((int)OldTokenGeneric.LESSER, Affix.InFix, Associativity.Right, 10)]
    [Operation((int)OldTokenGeneric.GREATER, Affix.InFix, Associativity.Right, 10)]
    [Operation((int)OldTokenGeneric.EQUALS, Affix.InFix, Associativity.Right, 10)]
    [Operation((int)OldTokenGeneric.DIFFERENT, Affix.InFix, Associativity.Right, 10)]
    public OldLangTree BinaryExpression(OldLangTree left, Token<OldTokenGeneric> operatorToken,
        OldLangTree right) =>
        new Operation((left as OldExpr)!, operatorToken.TokenID, (right as OldExpr)!);

    [Operation((int)OldTokenGeneric.CONCAT, Affix.InFix, Associativity.Right, 100)]
    public OldLangTree DotExpr(OldLangTree left, Token<OldTokenGeneric> opera, OldLangTree right) =>
        new Operation((left as OldExpr)!, opera.TokenID, (right as OldExpr)!);

    [Operation((int)OldTokenGeneric.PLUS, Affix.InFix, Associativity.Right, 20)]
    [Operation((int)OldTokenGeneric.MINUS, Affix.InFix, Associativity.Right, 20)]
    public OldLangTree NumberOpera1(OldLangTree left, Token<OldTokenGeneric> opera, OldLangTree right) =>
        new Operation((left as OldExpr)!, opera.TokenID, (right as OldExpr)!);

    [Operation((int)OldTokenGeneric.TIMES, Affix.InFix, Associativity.Right, 70)]
    [Operation((int)OldTokenGeneric.DIVIDE, Affix.InFix, Associativity.Right, 70)]
    public OldLangTree NumBerOpera2(OldLangTree left, Token<OldTokenGeneric> opera, OldLangTree right) =>
        new Operation((left as OldExpr)!, opera.TokenID, (right as OldExpr)!);

    [Operation((int)OldTokenGeneric.AND, Affix.InFix, Associativity.Right, 1)]
    [Operation((int)OldTokenGeneric.OR, Affix.InFix, Associativity.Right, 1)]
    [Operation((int)OldTokenGeneric.XOR, Affix.InFix, Associativity.Right, 1)]
    public OldLangTree BoolOpera(OldExpr left, Token<OldTokenGeneric> opera, OldExpr right) =>
        new Operation(left, opera.TokenID, right);

    [Operation((int)OldTokenGeneric.NOT, Affix.PreFix, Associativity.Right, 100)]
    public OldLangTree NotBool(Token<OldTokenGeneric> opera, OldExpr expr) =>
        new Operation(null!, opera.TokenID, expr);

    [Operation((int)OldTokenGeneric.MINUS, Affix.PreFix, Associativity.Right, 100)]
    public OldLangTree MINUS(Token<OldTokenGeneric> opera, OldExpr expr) =>
        new Operation(null!, opera.TokenID, expr);

    #endregion

    #region primany

    [Operand]
    [Production("operand: primary")]
    public OldLangTree Operand(OldLangTree prim) => prim;

    [Production("primary: STRING")]
    public OldLangTree STRING(Token<OldTokenGeneric> token) =>
        new StringValue(token.Value[1..^1]) { Position = token.Position };

    [Production("primary: INT")]
    public OldLangTree INT(Token<OldTokenGeneric> token) => new IntValue(token.IntValue) { Position = token.Position };

    [Production("primary: CHAR")]
    public OldLangTree CHAR(Token<OldTokenGeneric> token) =>
        new CharValue(token.CharValue) { Position = token.Position };

    [Production("primary: DOUBLE")]
    public OldLangTree DOUBLE(Token<OldTokenGeneric> token) => new DoubleValue(double.Parse(token.Value))
        { Position = token.Position };

    [Production("primary: IDENTIFIER")]
    public OldLangTree IDENTIFIER(Token<OldTokenGeneric> id) => new OldID(id.Value) { Position = id.Position };

    [Production("primary: TRUE")]
    public OldLangTree BoolTrue(Token<OldTokenGeneric> token) => new BoolValue(true) { Position = token.Position };

    [Production("primary: FALSE")]
    public OldLangTree BoolFalse(Token<OldTokenGeneric> token) => new BoolValue(false) { Position = token.Position };

    [Production("primary: IDENTIFIER L_BRACKET[d] OldParser_expressions R_BRACKET[d]")]
    public OldLangTree ListInit(Token<OldTokenGeneric> id, OldExpr a) =>
        new OldItem(new OldID(id.Value), a) { Position = id.Position };

    [Production("primary: IDENTIFIER LPAREN[d] OldParser_expressions* RPAREN[d]")]
    public OldLangTree Instantiate(Token<OldTokenGeneric> id, List<OldLangTree> ids)
    {
        List<OldExpr> IDs = [];
        ids.ForEach(x =>
        {
            if (x is OldExpr expr) IDs.Add(expr);
        });
        return new Instance(new OldID(id.Value) { Position = id.Position }, IDs) { Position = id.Position };
    }

    [Production("primary: LPAREN[d] IDENTIFIER* RPAREN[d] LAMBDA[d] OldParser_expressions")]
    public OldLangTree Lambda(List<Token<OldTokenGeneric>> ids, OldExpr expr)
    {
        List<OldLangTree> returnState = [new ReturnStatement(expr)];
        var a = ids.Select(x => new OldID(x.Value)).ToList();
        return new FuncValue(null, a, new BlockStatement(returnState)) { Position = expr.Position };
    }

    [Production("primary: COMMA")]
    public OldLangTree DouHaoTree(Token<OldTokenGeneric> _) => new IntValue(0) { Position = _.Position };

    [Production("primary: L_BRACES[d] OldParser_expressions* R_BRACES[d]")]
    public OldLangTree List(List<OldLangTree> list)
    {
        var a = new List<OldExpr>();
        list.ForEach(x => a.Add(x as OldExpr ?? new StringValue("")));
        return new ListValue(a) { Position = list.Count == 0 ? new LexerPosition(0, 0, 0) : list[0].Position };
    }

    [Production("primary: L_BRACKET OldParser_expressions* R_BRACKET[d]")]
    public OldLangTree Array(Token<OldTokenGeneric> _, List<OldLangTree> list)
    {
        var a = list.OfType<OldExpr>().ToList();
        return new ArrayValue(a) { Position = _.Position };
    }

    [Production("primary: LPAREN[d] OldParser_expressions COMMA[d] OldParser_expressions RPAREN[d]")]
    public OldLangTree Tuple(OldExpr v1, OldExpr v2) => new TupleValue(v1, v2) { Position = v1.Position };


    [Production("primary: L_BRACES[d] tuple+ R_BRACES[d]")]
    public OldLangTree Dic(List<OldLangTree> a)
    {
        var b = a.OfType<TupleValue>().ToList();
        return new DictionaryValue(b) { Position = b[0].Position };
    }

    [Production("tuple: OldParser_expressions COLON[d] OldParser_expressions")]
    public OldLangTree DicTuple(OldExpr v1, OldExpr v2) => new TupleValue(v1, v2) { Position = v1.Position };

    #endregion

    #region statement

    [Production("statement: IDENTIFIER L_BRACKET[d] OldParser_expressions R_BRACKET[d] SET[d] OldParser_expressions")]
    public OldLangTree ArraySetTree(Token<OldTokenGeneric> classId, OldExpr aid, OldExpr expr) =>
        new OtherVariateChanging(new OldID(classId.Value), aid, expr) { Position = classId.Position };

    [Production("statement: IDENTIFIER CONCAT[d] IDENTIFIER SET[d] OldParser_expressions")]
    public OldLangTree ClassSetTree(Token<OldTokenGeneric> classId, Token<OldTokenGeneric> aid, OldExpr expr) =>
        new OtherVariateChanging(new OldID(classId.Value), new OldID(aid.Value), expr) { Position = classId.Position };

    [Production("statement: RETURN[d] OldParser_expressions")]
    public OldLangTree ReturnTree(OldExpr expr) => new ReturnStatement(expr) { Position = expr.Position };


    [Production("statement: set")]
    public OldLangTree SetTree(SetStatement a) => a;

    [Production("set: IDENTIFIER SET[d] OldParser_expressions")]
    public OldLangTree Set(Token<OldTokenGeneric> id, OldExpr value) =>
        new SetStatement(new OldID(id.Value) { Position = id.Position }, value) { Position = id.Position };

    [Production("set: OldParser_expressions DIS_SET[d] IDENTIFIER")]
    public OldLangTree DIS_SET(OldExpr value, Token<OldTokenGeneric> id) =>
        new SetStatement(new OldID(id.Value), value);

    [Production("statement : IF[d] if_block (ELIF[d] if_block)* (ELSE[d] block)?")]
    public OldLangTree IfTree(OldIf ifBlock, List<Group<OldTokenGeneric, OldLangTree>> elif,
        ValueOption<Group<OldTokenGeneric, OldLangTree>> Else)
    {
        var eGrp = Else.Match(
            x => x, () => null!);
        var elseBlock = eGrp?.Value(0) as BlockStatement;
        var a = elif.Select(x => x.Value(0) as OldIf).ToList();
        return new IfStatement(ifBlock, a, elseBlock) { Position = ifBlock.Position };
    }

    [Production("if_block: OldParser_expressions block")]
    public OldLangTree IfBlock(OldExpr expr, BlockStatement blockStatement) =>
        new OldIf(expr, blockStatement) { Position = expr.Position };

    [Production("block: INDENT[d] statement* UINDENT[d]")]
    public OldLangTree Block(List<OldLangTree> statements) => new BlockStatement(statements);


    [Production("statement: FOR[d] set COMMA[d] OldParser_expressions COMMA[d] statement  block")]
    public OldLangTree For(SetStatement setStatement, Operation expr, OldStatement statement,
        BlockStatement blockStatement) =>
        new ForStatement(setStatement, expr, statement, blockStatement) { Position = setStatement.Position };

    [Production("statement: WHILE[d] OldParser_expressions block")]
    public OldLangTree While(OldExpr expr, BlockStatement blockStatement) => new WhileStatement(expr, blockStatement)
        { Position = expr.Position };


    [Production("statement: func")]
    public OldLangTree FuncTree(OldLangTree a) => a;

    [Production("func: IDENTIFIER LPAREN[d] IDENTIFIER* RPAREN[d] DIS_SET[d] block")]
    [Production("func: FUNC[d] IDENTIFIER LPAREN[d] IDENTIFIER* RPAREN[d] block")]
    public OldLangTree Func(Token<OldTokenGeneric> id, List<Token<OldTokenGeneric>> a, BlockStatement blockStatement)
    {
        var b = new List<OldID>();
        a.ForEach(x => b.Add(new OldID(x.Value)));
        return new FuncInit(new FuncValue(new OldID(id.Value), b, blockStatement)) { Position = id.Position };
    }

    [Production("func: IDENTIFIER LPAREN[d] IDENTIFIER* RPAREN[d] LAMBDA[d] OldParser_expressions")]
    public OldLangTree LambdaFunc(Token<OldTokenGeneric> id, List<Token<OldTokenGeneric>> ids, OldExpr expr)
    {
        List<OldLangTree> returnState = [new ReturnStatement(expr)];
        List<OldID> a = new List<OldID>();
        ids.ForEach(x => a.Add(new OldID(x.Value)));
        return new FuncInit(new FuncValue(new OldID(id.Value) { Position = id.Position }, a,
            new BlockStatement(returnState))) { Position = id.Position };
    }

    [Production("statement: CLASS[d] IDENTIFIER block")]
    public OldLangTree Class(Token<OldTokenGeneric> id, BlockStatement statements)
    {
        var c = new Dictionary<OldID, OldExpr>();
        statements.OtherStatements.ForEach(x => c.Add(GetTuple(x).id, GetTuple(x).Expr));
        return new ClassInit(new AnyValue(new OldID(id.Value), c)) { Position = id.Position };
    }

    [Production("statement: IDENTIFIER LPAREN[d] OldParser_expressions* RPAREN[d]")]
    public OldLangTree FuncRun(Token<OldTokenGeneric> id, List<OldLangTree> langTrees) =>
        new FuncRunStatement(new Instance(new OldID(id.Value) { Position = id.Position },
            langTrees.OfType<OldExpr>().ToList())) { Position = id.Position };

    [Production("statement: IDENTIFIER CONCAT[d] IDENTIFIER LPAREN[d] OldParser_expressions* RPAREN[d]")]
    public OldLangTree ClassFuncRun(Token<OldTokenGeneric> classId, Token<OldTokenGeneric> funcName,
        List<OldLangTree> expressions) =>
        new FuncRunStatement(new Operation(new OldID(classId.Value) { Position = classId.Position },
            OldTokenGeneric.CONCAT,
            new Instance(new OldID(classId.Value) { Position = classId.Position },
                expressions.OfType<OldExpr>().ToList()) { Position = classId.Position }));

    [Production("statement: IMPORT[d] IDENTIFIER")]
    public OldLangTree ImportTree(Token<OldTokenGeneric> import) =>
        new ImportStatement(import.Value) { Position = import.Position };


    [Production("statement: L_BRACKET[d] IMPORT[d] STRING IDENTIFIER IDENTIFIER IDENTIFIER? R_BRACKET[d]")]
    public OldLangTree NativeTree(Token<OldTokenGeneric> dllName, Token<OldTokenGeneric> className,
        Token<OldTokenGeneric> methodName, Token<OldTokenGeneric>? token) =>
        new NativeStatement(dllName.Value[1..^1], className.Value, methodName.Value, token!.Value)
            { Position = dllName.Position };

    [Production("statement:L_BRACKET[d] IMPORT[d] STRING IDENTIFIER R_BRACKET[d] LAMBDA[d] STRING")]
    public OldLangTree NativeStatic(Token<OldTokenGeneric> dll, Token<OldTokenGeneric> classname,
        Token<OldTokenGeneric> _namespace) =>
        new NativeStatement(dll.Value[1..^1], classname.Value, _namespace.Value[1..^1]);

    [Production("statement:L_BRACKET[d] IMPORT[d] STRING IDENTIFIER R_BRACKET[d]")]
    public OldLangTree NativeClass(Token<OldTokenGeneric> dllName, Token<OldTokenGeneric> classname) =>
        new NativeStatement(dllName.Value[1..^1], classname.Value);

    #endregion

    #region sugar

    [Production("statement:IDENTIFIER PLUS[d] PLUS[d]")]
    public OldLangTree PlusPlus(Token<OldTokenGeneric> id) =>
        new SetStatement(new OldID(id.Value), new Operation(new OldID(id.Value), OldTokenGeneric.PLUS, new IntValue(1)))
            { Position = id.Position };

    #endregion

    #region FuncValue

    private (OldID id, OldExpr Expr) GetTuple(OldLangTree a)
    {
        return a switch
        {
            SetStatement statement => (id: statement.Id, Expr: statement.Value),
            FuncInit init => (init.FuncValue.Id!, init.FuncValue),
            _ => (null!, null!)
        };
    }

    #endregion
}