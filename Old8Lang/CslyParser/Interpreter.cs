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

    private BlockStatement Block { get; set; }
    
    private VariateManager Manager;
    private string Code { get; set; }

    #endregion

    #region RunTime

    private string Time { get; set; }
    private double DoubleTime { get; set; }
    
    public DotGraph Graph { get; set; }
    #endregion
    
    

    private readonly List<string> Error;


    public Interpreter(string path, bool isDir)
    {
        Error = new List<string>();
        Manager = new VariateManager { Path = path };
        Code = isDir ? Apis.FromDirectory(path) : Apis.FromFile(path);
    }

    public Interpreter(string code, VariateManager manager)
    {
        Error = new List<string>();
        Manager = manager;
        Code = code;
    }

    public Interpreter(string path, bool isDir, LangInfo info)
    {
        Error = new List<string>();
        Manager = new VariateManager(info) { Path = path};
        Manager.Init();
        Code = isDir ? Apis.FromDirectory(path) : Apis.FromFile(path);
    }

    public void ParserRun(bool dot = false)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        Manager.LangInfo ??= Apis.ReadJson();
        Block = Build(dot);
        sw.Stop();
        TimeSpan ts = sw.Elapsed;
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

    public BlockStatement Build(bool isDot = false)
    {
        var Parser = new ParserBuilder<OldTokenGeneric, OldLangTree>();
        var oldParser = new OldParser();
        var parserBuilder = Parser.BuildParser(oldParser,
            ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
        var buildResult = parserBuilder.Result;

        var result = buildResult.Parse(Code);
        Block = result.Result as BlockStatement;
        if (isDot)
            Dot(result);
        if (result.Errors != null && result.Errors.Any())
            result.Errors.ForEach(x => Error.Add(x.ToString()));
        return Block;
    }

    public void Dot(ParseResult<OldTokenGeneric, OldLangTree> result)
    {
        var tree = result.SyntaxTree;
        var graphviz = new GraphVizEBNFSyntaxTreeVisitor<OldTokenGeneric>();
        graphviz.VisitTree(tree);
        Graph = graphviz.Graph;
    }

    public string GetTime() => Time;
    public List<string> GetError() => Error;
    public VariateManager GetVariateManager() => Manager;
}