using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using sly.lexer;
using sly.parser.generator;
using sly.parser.parser;

// ReSharper disable UnusedMember.Global

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

    /// <summary>
    /// 二元布尔计算
    /// </summary>
    /// <param name="left">左值</param>
    /// <param name="operatorToken">运算符</param>
    /// <param name="right">右值</param>
    /// <returns></returns>
    [Operation((int)OldTokenGeneric.LESSER, Affix.InFix, Associativity.Right, 10)]
    [Operation((int)OldTokenGeneric.GREATER, Affix.InFix, Associativity.Right, 10)]
    [Operation((int)OldTokenGeneric.EQUALS, Affix.InFix, Associativity.Right, 10)]
    [Operation((int)OldTokenGeneric.DIFFERENT, Affix.InFix, Associativity.Right, 10)]
    [Operation((int)OldTokenGeneric.LESS_EQUAL, Affix.InFix, Associativity.Right, 10)]
    [Operation((int)OldTokenGeneric.GREATER_EQUAL, Affix.InFix, Associativity.Right, 10)]
    public OldLangTree BinaryExpression(OldLangTree left, Token<OldTokenGeneric> operatorToken,
        OldLangTree right) =>
        new Operation((left as OldExpr)!, operatorToken.TokenID, (right as OldExpr)!);

    /// <summary>
    /// 点运算
    /// </summary>
    /// <param name="left">左值</param>
    /// <param name="opera">运算符</param>
    /// <param name="right">右值</param>
    /// <returns></returns>
    [Operation((int)OldTokenGeneric.CONCAT, Affix.InFix, Associativity.Right, 100)]
    public OldLangTree DotExpr(OldLangTree left, Token<OldTokenGeneric> opera, OldLangTree right) =>
        new Operation((left as OldExpr)!, opera.TokenID, (right as OldExpr)!);

    /// <summary>
    /// 加减
    /// </summary>
    /// <param name="left">左值</param>
    /// <param name="opera">运算符</param>
    /// <param name="right">右值</param>
    /// <returns></returns>
    [Operation((int)OldTokenGeneric.PLUS, Affix.InFix, Associativity.Right, 20)]
    [Operation((int)OldTokenGeneric.MINUS, Affix.InFix, Associativity.Right, 20)]
    public OldLangTree NumberOpera1(OldLangTree left, Token<OldTokenGeneric> opera, OldLangTree right) =>
        new Operation((left as OldExpr)!, opera.TokenID, (right as OldExpr)!);

    /// <summary>
    /// 乘除
    /// </summary>
    /// <param name="left">左值</param>
    /// <param name="opera">运算符</param>
    /// <param name="right">右值</param>
    /// <returns></returns>
    [Operation((int)OldTokenGeneric.TIMES, Affix.InFix, Associativity.Right, 70)]
    [Operation((int)OldTokenGeneric.DIVIDE, Affix.InFix, Associativity.Right, 70)]
    public OldLangTree NumBerOpera2(OldLangTree left, Token<OldTokenGeneric> opera, OldLangTree right) =>
        new Operation((left as OldExpr)!, opera.TokenID, (right as OldExpr)!);

    /// <summary>
    /// 布尔运算
    /// </summary>
    /// <param name="left">左值</param>
    /// <param name="opera">运算符</param>
    /// <param name="right">右值</param>
    /// <returns></returns>
    [Operation((int)OldTokenGeneric.AND, Affix.InFix, Associativity.Right, 1)]
    [Operation((int)OldTokenGeneric.OR, Affix.InFix, Associativity.Right, 1)]
    [Operation((int)OldTokenGeneric.XOR, Affix.InFix, Associativity.Right, 1)]
    public OldLangTree BoolOpera(OldExpr left, Token<OldTokenGeneric> opera, OldExpr right) =>
        new Operation(left, opera.TokenID, right);

    /// <summary>
    /// 取反
    /// </summary>
    /// <param name="opera">运算符</param>
    /// <param name="expr">右值</param>
    /// <returns></returns>
    [Operation((int)OldTokenGeneric.NOT, Affix.PreFix, Associativity.Right, 100)]
    public OldLangTree NotBool(Token<OldTokenGeneric> opera, OldExpr expr) =>
        new Operation(null!, opera.TokenID, expr);

    /// <summary>
    /// 取负
    /// </summary>
    /// <param name="opera">运算符</param>
    /// <param name="expr">右值</param>
    /// <returns></returns>
    [Operation((int)OldTokenGeneric.MINUS, Affix.PreFix, Associativity.Right, 100)]
    public OldLangTree MINUS(Token<OldTokenGeneric> opera, OldExpr expr) =>
        new Operation(null!, opera.TokenID, expr);

    #endregion

    #region primany

    [Operand]
    [Production("operand: primary")]
    public OldLangTree Operand(OldLangTree prim) => prim;

    #region BaseType

    [Production("primary: STRING")]
    public OldLangTree STRING(Token<OldTokenGeneric> token) =>
        new StringValue(token.Value[1..^1]);

    [Production("primary: INT")]
    public OldLangTree INT(Token<OldTokenGeneric> token) => new IntValue(token.IntValue);

    [Production("primary: CHAR")]
    public OldLangTree CHAR(Token<OldTokenGeneric> token) =>
        new CharValue(token.CharValue);

    [Production("primary: DOUBLE")]
    public OldLangTree DOUBLE(Token<OldTokenGeneric> token) => new DoubleValue(double.Parse(token.Value));

    // [Production("primary: IDENTIFIER")]
    // public OldLangTree IDENTIFIER(Token<OldTokenGeneric> id) => new OldID(id.Value);

    [Production("primary: ident")]
    public OldLangTree Id(OldLangTree id) => id;

    [Production("ident: IDENTIFIER (COLON[d] IDENTIFIER)?")]
    public OldLangTree Id_1(Token<OldTokenGeneric> id,ValueOption<Group<OldTokenGeneric, OldLangTree>> assumptionType)
    {
        var eGrp = assumptionType.Match(
            x => x, () => null!);
        var assumption = eGrp?.Token(0).Value;
        return new OldID(id.Value,assumption ?? "");
    }

    [Production("primary: TRUE[d]")]
    public OldLangTree BoolTrue() => new BoolValue(true);

    [Production("primary: FALSE[d]")]
    public OldLangTree BoolFalse() => new BoolValue(false);

    #endregion

    #region Arg and Id Array

    [Production("idList: ident (COMMA[d] ident)*")]
    public OldLangTree IdList(OldLangTree first, List<Group<OldTokenGeneric, OldLangTree>> list)
    {
        var a = new List<OldID>();

        if (first is not OldID firstId) return new IdList(a);

        a.Add(firstId);
        list.ForEach(x => a.Add(x.Value(0) as OldID ?? new OldID("")));
        return new IdList(a);
    }

    [Production("argList: OldParser_expressions (COMMA[d] OldParser_expressions)*")]
    public OldLangTree ArgList(OldLangTree first,
        List<Group<OldTokenGeneric, OldLangTree>> list)
    {
        var a = new List<OldExpr>();

        if (first is not OldExpr firstValue) return new ArgList(a);

        a.Add(firstValue);
        list.ForEach(x => a.Add(x.Value(0) as OldExpr ?? new StringValue("")));
        return new ArgList(a);
    }

    #endregion

    #region Instantiate and Index

    /// <summary>
    /// 索引
    /// </summary>
    /// <param name="id">变量名</param>
    /// <param name="a">索引</param>
    /// <returns>eg : list[1]</returns>
    [Production("primary: IDENTIFIER L_BRACKET[d] OldParser_expressions R_BRACKET[d]")]
    public OldLangTree ListInit(Token<OldTokenGeneric> id, OldExpr a) =>
        new OldItem(new OldID(id.Value), a);

    /// <summary>
    /// 实例化 / 函数
    /// </summary>
    /// <param name="id">变量名</param>
    /// <param name="ids">参数</param>
    /// <returns>eg: a(1,2,3)</returns>
    [Production("primary: IDENTIFIER LPAREN[d] argList? RPAREN[d]")]
    public OldLangTree Instantiate(Token<OldTokenGeneric> id, ValueOption<OldLangTree> ids)
    {
        List<OldExpr> IDs = [];

        var value = ids.Match(x => x, () => null!);
        if (value is ArgList argList) IDs.AddRange(argList.Args);
        return new Instance(new OldID(id.Value), IDs);
    }

    #endregion

    [Production("primary: DOLLAR[d] (L_BRACES[d] OldParser_expressions R_BRACES[d])*")]
    public OldLangTree StringTree(List<Group<OldTokenGeneric, OldLangTree>> list)
    {
        var a = list.Select(x => x.Value(0)).ToList();
        return new StringTreeList(a.OfType<OldExpr>().ToList());
    }

    /// <summary>
    /// lambda表达式
    /// </summary>
    /// <param name="ids">参数</param>
    /// <param name="expr">表达式</param>
    /// <returns>(a,v) => a+v</returns>
    [Production("primary: LPAREN[d] idList? RPAREN[d] LAMBDA[d] OldParser_expressions")]
    public OldLangTree Lambda(ValueOption<OldLangTree> ids, OldExpr expr)
    {
        var value = ids.Match(x => x, () => null!);
        return new FuncValue(null, value is not IdList argList ? [] : argList.Args,
            new BlockStatement([new ReturnStatement(expr)]));
    }

    #region Array , List , Dictionary

    /// <summary>
    /// 列表
    /// </summary>
    /// <param name="ids">元素</param>
    /// <returns>eg : {1,2,3,4}</returns>
    [Production("primary: L_BRACES[d] argList? R_BRACES[d]")]
    public OldLangTree List(ValueOption<OldLangTree> ids)
    {
        var value = ids.Match(x => x, () => null!);
        return new ListValue(value is not ArgList argList ? [] : argList.Args);
    }

    [Production("primary: L_BRACKET[d] OldParser_expressions WAVY[d] OldParser_expressions R_BRACKET[d]")]
    public OldLangTree Range(OldLangTree start, OldLangTree end)
    {
        return new RangeValue(start as OldExpr, end as OldExpr);
    }

    /// <summary>
    /// 数组
    /// </summary>
    /// <param name="ids">元素</param>
    /// <returns>eg : [1,2,3,4]</returns>
    [Production("primary: L_BRACKET[d] argList? R_BRACKET[d]")]
    public OldLangTree Array(ValueOption<OldLangTree> ids)
    {
        var value = ids.Match(x => x, () => null!);
        return new ArrayValue(value is not ArgList argList ? [] : argList.Args);
    }

    /// <summary>
    /// 元组
    /// </summary>
    /// <param name="v1">value 1</param>
    /// <param name="v2">value 2</param>
    /// <returns>eg : ('a',1)</returns>
    [Production("primary: LPAREN[d] OldParser_expressions COMMA[d] OldParser_expressions RPAREN[d]")]
    public OldLangTree Tuple(OldExpr v1, OldExpr v2) => new TupleValue(v1, v2);


    /// <summary>
    /// 字典
    /// </summary>
    /// <param name="first"></param>
    /// <param name="list"></param>
    /// <returns>eg : {'1' : 1 'c' : 2}</returns>
    [Production("primary: L_BRACES[d] tuple (COMMA[d] tuple)* R_BRACES[d]")]
    public OldLangTree Dic(OldLangTree first, List<Group<OldTokenGeneric, OldLangTree>> list)
    {
        var b = new List<TupleValue>();
        if (first is not TupleValue firstValue) return new DictionaryValue([]);

        b.Add(firstValue);
        list.ForEach(x => b.Add(x.Value(0) as TupleValue ?? new TupleValue(new VoidValue(), new VoidValue())));
        return new DictionaryValue(b);
    }

    /// <summary>
    /// 字典元素
    /// </summary>
    /// <param name="v1">Key</param>
    /// <param name="v2">Value</param>
    /// <returns>eg : 'a':1</returns>
    [Production("tuple: OldParser_expressions COLON[d] OldParser_expressions")]
    public OldLangTree DicTuple(OldExpr v1, OldExpr v2) => new TupleValue(v1, v2);

    [Production("primary: IDENTIFIER L_BRACKET[d] OldParser_expressions? COLON[d] OldParser_expressions? R_BRACKET[d]")]
    public OldLangTree Slice(Token<OldTokenGeneric> id, ValueOption<OldLangTree> start, ValueOption<OldLangTree> end)
    {
        var startValue = start.Match(x => x, () => null!);
        var endValue = end.Match(x => x, () => null!);
        return new SliceValue(new OldID(id.Value), startValue as OldExpr, endValue as OldExpr);
    }

    #endregion

    /// <summary>
    /// as 语句
    /// </summary>
    /// <param name="id">变量</param>
    /// <param name="asId">需要转化的类型</param>
    /// <returns></returns>
    [Production("primary: IDENTIFIER AS[d] IDENTIFIER")]
    public OldLangTree As(Token<OldTokenGeneric> id, Token<OldTokenGeneric> asId)
    {
        return new AsValue(new OldID(id.Value), new OldID(asId.Value));
    }

    #endregion

    #region statement

    #region Set

    /// <summary>
    /// 
    /// </summary>
    /// <param name="classId"></param>
    /// <param name="aid"></param>
    /// <param name="expr"></param>
    /// <returns></returns>
    [Production("statement: IDENTIFIER L_BRACKET[d] OldParser_expressions R_BRACKET[d] SET[d] OldParser_expressions")]
    public OldLangTree ArraySetTree(Token<OldTokenGeneric> classId, OldExpr aid, OldExpr expr) =>
        new OtherVariateChanging(new OldID(classId.Value), aid, expr);

    [Production("statement: IDENTIFIER CONCAT[d] IDENTIFIER SET[d] OldParser_expressions")]
    public OldLangTree ClassSetTree(Token<OldTokenGeneric> classId, Token<OldTokenGeneric> aid, OldExpr expr) =>
        new OtherVariateChanging(new OldID(classId.Value), new OldID(aid.Value), expr);

    [Production("statement: RETURN[d] OldParser_expressions")]
    public OldLangTree ReturnTree(OldExpr expr) => new ReturnStatement(expr);


    [Production("statement: set")]
    public OldLangTree SetTree(SetStatement a) => a;

    [Production("set: IDENTIFIER SET[d] OldParser_expressions")]
    public OldLangTree Set(Token<OldTokenGeneric> id, OldExpr value) =>
        new SetStatement(new OldID(id.Value), value);

    [Production("set: OldParser_expressions DIS_SET[d] IDENTIFIER")]
    public OldLangTree DIS_SET(OldExpr value, Token<OldTokenGeneric> id) =>
        new SetStatement(new OldID(id.Value), value);

    [Production("statement: idList SET[d] argList")]
    public OldLangTree SetList(OldLangTree ids, OldLangTree values)
    {
        if (values is not ArgList argList || ids is not IdList idList) throw new Exception("both count is not ");
        return new SetListStatement(idList.Args, argList.Args);
    }

    [Production("statement: argList DIS_SET[d] idList")]
    public OldLangTree Dis_SetList(OldLangTree values, OldLangTree ids)
    {
        if (values is not ArgList argList || ids is not IdList idList) throw new Exception("both count is not ");
        return new SetListStatement(idList.Args, argList.Args);
    }

    #endregion

    #region If Tree

    [Production("statement : IF[d] if_block (ELIF[d] if_block)* (ELSE[d] block)?")]
    public OldLangTree IfTree(OldIf ifBlock, List<Group<OldTokenGeneric, OldLangTree>> elif,
        ValueOption<Group<OldTokenGeneric, OldLangTree>> Else)
    {
        var eGrp = Else.Match(
            x => x, () => null!);
        var elseBlock = eGrp?.Value(0) as BlockStatement;
        var a = elif.Select(x => x.Value(0) as OldIf).ToList();
        return new IfStatement(ifBlock, a, elseBlock);
    }

    [Production("if_block: OldParser_expressions block")]
    public OldLangTree IfBlock(OldExpr expr, BlockStatement blockStatement) =>
        new OldIf(expr, blockStatement);

    #endregion

    #region Block Tree

    [Production("block: L_BRACES[d] statement* R_BRACES[d]")]
    public OldLangTree Block(List<OldLangTree> statements) => new BlockStatement(statements);

    [Production("block : statement")]
    public OldLangTree BlockLine(OldLangTree statement) => new BlockStatement([statement]);

    #endregion

    #region For While

    [Production("statement: FOR[d] set COMMA[d] OldParser_expressions COMMA[d] statement block")]
    public OldLangTree For(SetStatement setStatement, Operation expr, OldStatement statement,
        BlockStatement blockStatement) =>
        new ForStatement(setStatement, expr, statement, blockStatement);

    [Production("statement: WHILE[d] OldParser_expressions block")]
    public OldLangTree While(OldExpr expr, BlockStatement blockStatement) => new WhileStatement(expr, blockStatement);

    [Production("statement: FOR[d] IDENTIFIER IN[d] OldParser_expressions block")]
    public OldLangTree ForIn(Token<OldTokenGeneric> id, OldExpr expr, BlockStatement blockStatement) =>
        new ForInStatement(new OldID(id.Value), expr, blockStatement);

    #endregion

    #region Switch

    [Production("statement: SWITCH[d] OldParser_expressions L_BRACES[d] caseBlock* (DEFAULT[d] block)? R_BRACES[d]")]
    public OldLangTree Switch(OldExpr expr, List<OldLangTree> caseBlock,
        ValueOption<Group<OldTokenGeneric, OldLangTree>> de)
    {
        var dGrp = de.Match(
            x => x, () => null!);
        var d = dGrp.Value(0) as BlockStatement;

        return new SwitchStatement(expr, caseBlock.OfType<OldCase>().ToList(), d);
    }

    [Production("caseBlock: CASE[d] OldParser_expressions block")]
    public OldLangTree Case(OldExpr expr, BlockStatement blockStatement) => new OldCase(expr, blockStatement);

    #endregion

    #region Func and Lambda

    [Production("statement: func")]
    public OldLangTree FuncTree(OldLangTree a) => a;

    [Production("func: ident LPAREN[d] idList? RPAREN[d] DIS_SET[d] block")]
    [Production("func: FUNC[d] ident LPAREN[d] idList? RPAREN[d] block")]
    public OldLangTree Func(OldLangTree id, ValueOption<OldLangTree> ids, BlockStatement blockStatement)
    {
        if (id is not OldID oldId) return id;
        var value = ids.Match(x => x, () => null!);
        return new FuncInit(new FuncValue(oldId,
            value is not IdList idList ? [] : idList.Args,
            blockStatement));
    }

    [Production("func: IDENTIFIER LPAREN[d] idList? RPAREN[d] LAMBDA[d] OldParser_expressions")]
    public OldLangTree LambdaFunc(Token<OldTokenGeneric> id, ValueOption<OldLangTree> ids, OldExpr expr)
    {
        var value = ids.Match(x => x, () => null!);
        return new FuncInit(new FuncValue(new OldID(id.Value),
            value is not IdList idList ? [] : idList.Args,
            new BlockStatement([new ReturnStatement(expr)])));
    }

    #endregion

    #region Class

    [Production("statement: CLASS[d] IDENTIFIER classBlock")]
    public OldLangTree Class(Token<OldTokenGeneric> id, BlockStatement statements)
    {
        return new ClassInit(new AnyValue(new OldID(id.Value), statements.ToAnyData()));
    }

    [Production("classBlock: L_BRACES[d] [set | func]* R_BRACES[d]")]
    public OldLangTree ClassBlock(List<OldLangTree> statements)
    {
        return new BlockStatement(statements);
    }

    #endregion

    #region Run

    [Production("statement: IDENTIFIER LPAREN[d] argList? RPAREN[d]")]
    public OldLangTree FuncRun(Token<OldTokenGeneric> id, ValueOption<OldLangTree> langTrees)
    {
        var value = langTrees.Match(x => x, () => null!);
        return new FuncRunStatement(new Instance(new OldID(id.Value),
            value is not ArgList argList ? [] : argList.Args));
    }

    [Production("statement: IDENTIFIER CONCAT[d] IDENTIFIER LPAREN[d] argList? RPAREN[d]")]
    public OldLangTree ClassFuncRun(Token<OldTokenGeneric> classId, Token<OldTokenGeneric> funcName,
        ValueOption<OldLangTree> langTrees)
    {
        var value = langTrees.Match(x => x, () => null!);
        return new FuncRunStatement(new Operation(new OldID(classId.Value),
            OldTokenGeneric.CONCAT,
            new Instance(new OldID(funcName.Value),
                value is not ArgList argList ? [] : argList.Args)));
    }

    #endregion

    #region Import

    [Production("statement: IMPORT[d] IDENTIFIER")]
    public OldLangTree ImportTree(Token<OldTokenGeneric> import) =>
        new ImportStatement(import.Value);


    [Production("statement: L_BRACKET[d] IMPORT[d] STRING IDENTIFIER IDENTIFIER IDENTIFIER? R_BRACKET[d]")]
    public OldLangTree NativeTree(Token<OldTokenGeneric> dllName, Token<OldTokenGeneric> className,
        Token<OldTokenGeneric> methodName, Token<OldTokenGeneric>? token) =>
        new NativeStatement(dllName.Value[1..^1], className.Value, methodName.Value, token!.Value);

    [Production("statement:L_BRACKET[d] IMPORT[d] STRING IDENTIFIER R_BRACKET[d] LAMBDA[d] STRING")]
    public OldLangTree NativeStatic(Token<OldTokenGeneric> dll, Token<OldTokenGeneric> classname,
        Token<OldTokenGeneric> _namespace) =>
        new NativeStatement(dll.Value[1..^1], classname.Value, _namespace.Value[1..^1]);

    [Production("statement:L_BRACKET[d] IMPORT[d] STRING IDENTIFIER R_BRACKET[d]")]
    public OldLangTree NativeClass(Token<OldTokenGeneric> dllName, Token<OldTokenGeneric> classname) =>
        new NativeStatement(dllName.Value[1..^1], classname.Value);

    #endregion

    #endregion

    #region sugar

    [Production("statement:IDENTIFIER PLUS[d] PLUS[d]")]
    public OldLangTree PlusPlus(Token<OldTokenGeneric> id) =>
        new SetStatement(new OldID(id.Value),
            new Operation(new OldID(id.Value), OldTokenGeneric.PLUS, new IntValue(1)));

    [Production("statement:IDENTIFIER MINUS[d] MINUS[d]")]
    public OldLangTree MinusMinus(Token<OldTokenGeneric> id) =>
        new SetStatement(new OldID(id.Value),
            new Operation(new OldID(id.Value), OldTokenGeneric.MINUS, new IntValue(1)));

    #endregion
}