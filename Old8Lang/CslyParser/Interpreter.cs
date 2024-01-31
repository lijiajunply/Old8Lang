using System.Diagnostics;
using Old8Lang.AST;
using Old8Lang.AST.Statement;
using sly.parser;
using sly.parser.generator;
using sly.parser.generator.visitor;
using sly.parser.generator.visitor.dotgraph;

namespace Old8Lang.CslyParser;

public class Interpreter
{
    #region Code

    public VariateManager Manager = new();
    private string Code { get; } = "";

    private Parser<OldTokenGeneric, OldLangTree>? parser;

    #endregion

    #region RunTime

    private string Time { get; set; } = "";
    private double DoubleTime { get; set; }

    private DotGraph? Graph { get; set; }

    #endregion

    private readonly List<string> Error = [];

    private void Init()
    {
        var Parser = new ParserBuilder<OldTokenGeneric, OldLangTree>();
        var oldParser = new OldParser();
        var parserBuilder = Parser.BuildParser(oldParser,
            ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
        parser = parserBuilder.Result;
        Manager.Interpreter = this;
        Manager.LangInfo ??= Apis.ReadJson();
    }
    
    public Interpreter(string path, bool isDir)
    {
        Manager.Path = path;
        Code = isDir ? Apis.FromDirectory(path) : Apis.FromFile(path);
        Init();
    }

    public Interpreter()
    {
        Init();
    }
    
    public void ParserRun(bool dot = false)
    {
        var sw = new Stopwatch();
        sw.Start();
        var block = Build(dot);
        sw.Stop();
        var ts = sw.Elapsed;
        Time += $"Parser Build Time : {ts.TotalMilliseconds}ms\n";
        DoubleTime += ts.TotalMilliseconds;

        sw.Restart();
        block.Run(ref Manager);
        sw.Stop();
        ts = sw.Elapsed;
        Time += $"Process Run Time : {ts.TotalMilliseconds}ms\n";
        DoubleTime += ts.TotalMilliseconds;
        Time += $"Total : {DoubleTime}ms";
    }

    public BlockStatement Build(bool isDot = false,string code = "")
    {
        var result = parser?.Parse(string.IsNullOrEmpty(code) ? Code : code);

        if (result is null) return new BlockStatement([]);
        
        if (isDot)
            Dot(result);
        if(Error.Count != 0)Error.Clear();
        if (result.Errors != null && result.Errors.Count != 0)
            result.Errors.ForEach(x => Error.Add($"{x.ErrorType} : {x.ErrorMessage} in {x.Line} {x.Column}"));
        return result.Result as BlockStatement ?? new BlockStatement([]);
    }

    private void Dot(ParseResult<OldTokenGeneric, OldLangTree> result)
    {
        var tree = result.SyntaxTree;
        var graphviz = new GraphVizEBNFSyntaxTreeVisitor<OldTokenGeneric>();
        graphviz.VisitTree(tree);
        Graph = graphviz.Graph;
    }

    public string GetTime() => Time;
    public List<string> GetError() => Error;
    public DotGraph? GetGraph() => Graph;
}