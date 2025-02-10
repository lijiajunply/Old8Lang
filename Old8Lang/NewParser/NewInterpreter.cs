using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.NewParser;

public class NewInterpreter : IMiniInterpreter
{
    public BlockStatement Build(string code)
    {
        var parser = NewTokenizer.Tokenize(code);
        if (parser == null) throw new Exception("语法出错");
        return new NewParser(parser).ParseProgram();
    }
    
    public static List<NewToken> Tokenize(string code)
    {
        return NewTokenizer.Tokenize(code);
    }

    public AbsUseClass UseClass { get; set; } = new ConsoleUse();
    public bool IsCompileOptimization { get; set; }
}