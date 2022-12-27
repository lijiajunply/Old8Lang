using System.Text;
using System.Text.RegularExpressions;

namespace Old8Lang.Lexer;

public class Lexer
{
    public static String RegexPat
        => "\\s*((//.*)|([0-9]+)|(\"(\\\\\"|\\\\\\\\|\\\\n|[^\"])*\")"
          + "|[A-Z_a-z][A-Z_a-z0-9]*|==|<=|>=|&&|\\|\\||\\p{Punct})?";
    private static Regex regex => new Regex(RegexPat);
    private List<Token> Queue => new List<Token>();
    private int NowLine { get; set; } = 1;
    /// <summary>
    /// 行数有更多
    /// </summary>
    private bool HaveMore;
    /// <summary>
    /// 
    /// </summary>
    private StreamReader Reader { get; set; }

    public Lexer(StreamReader r) {
        HaveMore = true;
        Reader = r;
    }

    public string UseLexer()
    {
        int i = 1;
        Token aas = new Token(i);
        while (aas != Token.EOF)
        {
            aas = TokenRead();
            if (aas == Token.EOF)
            {
                var oueueReaing = QueueReaing();
                return oueueReaing;
            }
            aas = Peek(i);
        }
        return this.QueueReaing();
    }

    public string QueueReaing()
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (var VARIABLE in Queue)
        {
            stringBuilder.Append($"{VARIABLE.ToString()} ");
        }

        return stringBuilder.ToString();
    }
        /// <summary>
    /// 将第一个读取,并删除
    /// </summary>
    /// <returns></returns>
    public Token TokenRead()
    {
        if (FillQueue(0))
        {
            var aa = Queue[0];
            Queue.RemoveAt(0);
            return aa;
        }
        else
            return Token.EOF;
    }
    /// <summary>
    /// 预读
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public Token Peek(int line) {
        if (FillQueue(line))
            return Queue[line];
        else
            return Token.EOF; 
    }
    /// <summary>
    /// 填充列表
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    private bool FillQueue(int line) {
        while (line >= Queue.Count)
        {
            if (HaveMore)
                ReadLine();
            else
                return false;
        }
        return true;
    }
    /// <summary>
    /// 读取行
    /// </summary>
    /// <exception cref="ParseException"></exception>
    protected void ReadLine()
    {
        String line = null;
        try 
        {
            line = Reader.ReadLine();
        } 
        catch (IOException e)
        {
            throw new Old8LangException(e);
        }
        if (line == null) {
            HaveMore = false;
            return;
        }
        int lineNo = NowLine;
        var matches = regex.Matches(line);
        foreach (Match match in matches)
        {
            if (match.Success)
            {
                AddToken(lineNo,match);
            }
            else
            {
                throw new Old8LangException(" " + lineNo+$"bad _token at position{lineNo}:{match.Index}");
            }
        }
        Queue.Add(new IdToken(lineNo, Token.EOL));
        NowLine++;
    }
    protected void AddToken(int lineNo, Match match) {
        String m = match.Groups[1].ToString();
        if (m != null) // 当不为空时
            if (match.Groups[2] == null) { // if not a comment
                Token token;
                if (match.Groups[3] != null)
                    token = new NumToken(lineNo, Int32.Parse(m));
                else if (match.Groups[4] != null)
                    token = new StrToken(lineNo, ToStringLiteral(m));
                else
                    token = new IdToken(lineNo, m);
                Queue.Add(token);
            }
    }
    protected String ToStringLiteral(string s) {
        StringBuilder sb = new StringBuilder();
        int len = s.Length - 1;
        for (int i = 1; i < len; i++) {
            char c = s[i];
            if (c == '\\' && i + 1 < len) {
                int c2 = s[i + 1];
                if (c2 == '"' || c2 == '\\')
                    c = s[++i];
                else if (c2 == 'n') {
                    ++i;
                    c = '\n';
                }
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
}
