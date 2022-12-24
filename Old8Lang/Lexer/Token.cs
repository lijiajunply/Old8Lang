namespace Old8Lang.Lexer;

public class Token
{
    /// <summary>
    /// 结束
    /// </summary>
    public static Token EOF { get; set; } = new Token(-1);
    public static string EOL { get; set; } = "\n";
    private int LineNum;
    public Token(int line)
    {
        LineNum = line;
    }

    public int GetLineNum() => LineNum;
    public bool isIdentifier() => false;
    public bool isNum() => false;
    public bool isString() => false;
    public int GetNumber() => throw new Old8LangException("not number token");
    public string GetString() => "";
}