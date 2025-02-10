using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.LangParser;

public class LangInterpreter : IMiniInterpreter
{
    public BlockStatement Build(string code)
    {
        var parser = LangTokenizer.Tokenize(code);
        if (parser == null) throw new Exception("语法出错");
        return new LangParser(parser).ParseProgram();
    }
    
    public static List<LangToken> Tokenize(string code)
    {
        return LangTokenizer.Tokenize(code);
    }

    public AbsUseClass UseClass { get; set; } = new ConsoleUse();
    public bool IsCompileOptimization { get; set; }
}