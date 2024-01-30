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

    private VariateManager Manager;
    private string Code { get; set; }

    private Parser<OldTokenGeneric, OldLangTree>? parser;

    #endregion

    #region RunTime

    private string Time { get; set; } = "";
    private double DoubleTime { get; set; }

    private DotGraph? Graph { get; set; }

    #endregion
    
    private readonly List<string> Error;

    private void Init()
    {
        var Parser = new ParserBuilder<OldTokenGeneric, OldLangTree>();
        var oldParser = new OldParser();
        var parserBuilder = Parser.BuildParser(oldParser,
            ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
        parser = parserBuilder.Result;
    }
    
    public Interpreter(string path, bool isDir)
    {
        Error = new List<string>();
        Manager = new VariateManager { Path = path };
        Code = isDir ? Apis.FromDirectory(path) : Apis.FromFile(path);
        Init();
        Manager.Interpreter = this;
    }
    
    public Interpreter(string code, VariateManager manager)
    {
        Error = new List<string>();
        Manager = manager;
        Code = code;
        Init();
        Manager.Interpreter = this;
    }

    public Interpreter(string path, bool isDir, LangInfo info)
    {
        Error = new List<string>();
        Manager = new VariateManager(info) { Path = path };
        Manager.Init();
        Code = isDir ? Apis.FromDirectory(path) : Apis.FromFile(path);
        Init();
        Manager.Interpreter = this;
    }

    public void ParserRun(bool dot = false)
    {
        var sw = new Stopwatch();
        sw.Start();
        Manager.LangInfo ??= Apis.ReadJson();
        var Block = Build(dot);
        sw.Stop();
        var ts = sw.Elapsed;
        Time += $"Parser Build Time : {ts.TotalMilliseconds}ms\n";
        DoubleTime += ts.TotalMilliseconds;

        sw = new Stopwatch();
        sw.Start();
        Block.Run(ref Manager);
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
    public VariateManager GetVariateManager() => Manager;
    public DotGraph? GetGraph() => Graph;
}