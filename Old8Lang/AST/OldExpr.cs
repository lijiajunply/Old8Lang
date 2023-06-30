using Old8Lang.AST.Expression;
using Old8Lang.CslyParser;
using sly.lexer;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.AST;

/// <summary>
/// expr ::= id compare value 
/// </summary>
public class OldExpr : OldLangTree
{
    public virtual ValueType Run(ref VariateManager Manager) => null;
    public LexerPosition Position { get; set; }
}