namespace Old8Lang.Lexer;

public class IdToken : Token 
{
    private String text; 
    public IdToken(int line, String id) : base(line)
    {
        text = id;
    }
    public new bool isIdentifier() => true;
    public override string ToString() => text;
    
}