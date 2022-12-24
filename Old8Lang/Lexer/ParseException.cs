namespace Old8Lang.Lexer;



class  Old8LangException : Exception
{
    public Old8LangException(Token t) : base(t.ToString()){}
    public Old8LangException(String msg, Token t) : base("syntax error around " + location(t) + ". " + msg){}
    private static String location(Token t)
    {
        if (t == Token.EOF)
            return "the last line";
        else
            return "\"" + t.ToString() + "\" at line " + t.GetLineNum();
    }
    public Old8LangException(IOException e) : base(e.ToString()){}
    public Old8LangException(string msg) : base(msg){}
}