using Old8Lang.Lexer;

namespace Old8Lang.OldTrees;

public class NumberLiteral : OldLeaf
{
    public NumberLiteral(Token t) : base(t){}
    public int value() => token().GetNumber();
}