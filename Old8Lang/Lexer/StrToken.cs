namespace Old8Lang.Lexer;

public class StrToken : Token 
{
    private String literal;
    public StrToken(int line, String str) : base(line)
    {
        literal = str;
    }
    public new bool isString() => true;
    public override string ToString() => literal;
}