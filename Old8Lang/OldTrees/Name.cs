using Old8Lang.Lexer;

namespace Old8Lang.OldTrees;

public class Name : OldLeaf 
{
    public Name(Token t) : base(t){ }
    public String name() => token().ToString();
}