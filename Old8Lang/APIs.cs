namespace Old8Lang;

public class APIs
{
    public static StreamReader Reader { get; set; }
    public APIs(string Context)
    {
        Reader = new StreamReader(Context);
    }

    public void LexerUsing()
    {
        var _lexer = new Lexer.Lexer(Reader);
        //_lexer.Peek(line:1);
    }
}