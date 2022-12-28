using Old8Lang.CslyMake.OldExpression;
using Old8Lang.CslyMake.OldLandParser;
using sly.lexer;
using sly.parser.generator;
using sly.parser.parser;

namespace Old8Lang.CslyMake;

public class OldParser
{
    [Production("root : statement*")]
    public OldLangTree Root(List<OldLangTree> statement) => new OldBlock(statement);

    [Production("statement : [set | compound]")]
    public OldLangTree Statament(OldLangTree stat) => stat as OldStatement;

    [Production("expr : id compare value")]
    public OldLangTree Expr(OldID id, OldCompare compare, OldValue value) => new OldExpr(id,compare,value);

    [Production("set : id SET[d] value")]
    public OldLangTree Set(OldID id, OldValue value) => new OldSet(id, value);

    [Production("value : [string|int|char|double]")]
    public OldLangTree Value(OldLangTree value) => value as OldValue;

    [Production("string : STRING")]
    public OldLangTree STRING(Token<OldTokenGeneric> token) => new OldString(token.Value);

    [Production("int : INT")]
    public OldLangTree INT(Token<OldTokenGeneric> token) => new OldInt(token.IntValue);

    [Production("char : CHAR")]
    public OldLangTree CHAR(Token<OldTokenGeneric> token) => new OldChar(token.Value[0]);

    [Production("double : DOUBLE")]
    public OldLangTree DOUBLE(Token<OldTokenGeneric> token) => new OldDouble(double.Parse(token.Value));

    [Production("compound : [ ifelifelse | while | for ]")]
    public OldLangTree Compound(OldLangTree comp) => comp as OldCompare;

    [Production("ifelifelse : IF[d] expr : block (ELIF[d] expr : block)* [ELSE[d] : block]")]
    public OldLangTree IF(OldExpr ifexpr , OldBlock ifBlock , OldExpr? elifExpr , OldBlock? elifBlock , ValueOption<Group<OldTokenGeneric,OldLangTree>> elseBlock)
    {
        var eGrp = elseBlock.Match(
            x => x,() => null
        );
        var elseblock = eGrp?.Value(0) as OldBlock;
        return new OldIf_Elif_Else(new OldIf(ifexpr,ifBlock),new OldIf(elifExpr,elifBlock),elseblock);
    }

    [Production("block : INDENT[d] statement* UINDENT[d]")]
    public OldLangTree Block(List<OldLangTree> statements) => new OldBlock(statements);

    [Production("for : FOR[d] set , expr , statement : block")]
    public OldLangTree FOR(OldSet set, OldExpr expr, OldStatement statement, OldBlock block) =>
        new OldFor(set, expr, statement, block);

    [Production("while : WHILE[d] expr : block")]
    public OldLangTree WHILE(OldExpr expr, OldBlock block) => new OldWhile(expr, block);
}