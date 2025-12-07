using Old8Lang.AST.Statement;
using Old8Lang.Compiler;

namespace Old8Lang.LangParser;

public class LangInterpreter : IMiniInterpreter
{
    public readonly VariateManager Manager = new();
    
    public LangInterpreter()
    {
        Manager.Interpreter = this;
        Manager.LangInfo ??= Apis.ReadJson();
    }

    // 实现接口方法
    public BlockStatement Build(string code)
    {
        return Build(code, null);
    }
    
    // 主要的构建方法，支持传递文件名
    public BlockStatement Build(string code, string? fileName)
    {
        var parser = LangTokenizer.Tokenize(code);
        if (parser == null) throw new Exception("语法出错");
        //parser.ForEach(x => Console.WriteLine(x));
        return new LangParser(parser, code, fileName).ParseProgram();
    }

    public static List<LangToken> Tokenize(string code)
    {
        return LangTokenizer.Tokenize(code);
    }

    public AbsUseClass UseClass { get; set; } = new ConsoleUse();
    public bool IsCompileOptimization { get; set; }
}