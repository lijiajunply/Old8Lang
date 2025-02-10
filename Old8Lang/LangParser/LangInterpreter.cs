using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.LangParser;

public class LangInterpreter : IMiniInterpreter
{
    public readonly VariateManager Manager = new();
    
    public LangInterpreter()
    {
        Manager.Interpreter = this;
        Manager.LangInfo ??= Apis.ReadJson();
    }

    public BlockStatement Build(string code)
    {
        var parser = LangTokenizer.Tokenize(code);
        if (parser == null) throw new Exception("语法出错");
        parser.ForEach(x => Console.WriteLine(x));
        return new LangParser(parser).ParseProgram();
    }

    public static List<LangToken> Tokenize(string code)
    {
        return LangTokenizer.Tokenize(code);
    }

    public AbsUseClass UseClass { get; set; } = new ConsoleUse();
    public bool IsCompileOptimization { get; set; }
}