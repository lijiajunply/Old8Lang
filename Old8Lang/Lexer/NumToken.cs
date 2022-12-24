namespace Old8Lang.Lexer;

public class NumToken : Token 
{
    private int value;
    public NumToken(int line, int v) : base(line)
    {
        value = v;
    }
    public bool isNumber() => true;

    public override string ToString() =>  value.ToString();
    public new int GetNumber() => value;
}