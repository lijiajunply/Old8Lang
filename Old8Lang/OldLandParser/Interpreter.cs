using System.Diagnostics;
using Old8Lang.AST;
using Old8Lang.AST.Statement;
using sly.lexer;
using sly.parser;
using sly.parser.generator;
using sly.parser.generator.visitor;

namespace Old8Lang.OldLandParser;

public class Interpreter
{
    private BlockStatement Block { get; set; }

    private string Time { get; set; }

    private VariateManager Manager;

    private double DoubleTime { get; set; }

    private readonly List<string> Error;

    private string Code { get; set; }

    public Interpreter(string path,bool isDir)
    {
        Error   = new List<string>();
        Manager = new VariateManager { Path = path };
        Code    = isDir ? APIs.FromDirectory(path) : APIs.FromFile(path);
    }
    public Interpreter(string code,VariateManager manager)
    {
        Error   = new List<string>();
        Manager = manager;
        Code    = code;
    }
    public Interpreter(string path,bool isDir,LangInfo info)
    {
        Error   = new List<string>();
        Manager = new VariateManager { Path = path,LangInfo = info };
        Manager.Init();
        Code = isDir ? APIs.FromDirectory(path) : APIs.FromFile(path);
    }

    public void ParserRun(bool Dot = false)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        if (Manager.LangInfo is null)
            Manager.LangInfo = APIs.Read_JSON();
        Block = Build(Dot);
        sw.Stop();
        TimeSpan ts = sw.Elapsed;
        Time       += $"Parser Build Time : {ts.TotalMilliseconds}ms\n";
        DoubleTime += ts.TotalMilliseconds;

        sw = new Stopwatch();
        sw.Start();
        Block.Run(ref Manager);
        sw.Stop();
        ts         =  sw.Elapsed;
        Time       += $"Process Run Time : {ts.TotalMilliseconds}ms\n";
        DoubleTime += ts.TotalMilliseconds;
        Time       += $"Total : {DoubleTime}ms";
    }
    public BlockStatement Build(bool isDot = false)
    {
        var Parser    = new ParserBuilder<OldTokenGeneric,OldLangTree>();
        var oldParser = new OldParser();
        var parserBuilder = Parser.BuildParser(oldParser,
                                               ParserType.EBNF_LL_RECURSIVE_DESCENT,"root");
        var buildResult = parserBuilder.Result;

        var result = buildResult.Parse(Code);
        Block = result.Result as BlockStatement;
        if (isDot)
            Dot(result);
        if (result.Errors != null && result.Errors.Any())
            result.Errors.ForEach(x => Error.Add(x.ToString()));
        return Block;
    }
    public void Dot(ParseResult<OldTokenGeneric,OldLangTree> result)
    {
        string dic      = Path.GetDirectoryName(Manager.Path);
        var    path     = dic+$"/tree_By_{Path.GetFileNameWithoutExtension(Manager.Path)}.dot";
        var    tree     = result.SyntaxTree;
        var    graphviz = new GraphVizEBNFSyntaxTreeVisitor<OldTokenGeneric>();
        graphviz.VisitTree(tree);
        var    graph    = graphviz.Graph.Compile();
        File.WriteAllText(path,graph);
    }
    public string         GetTime()           => Time;
    public List<string>   GetError()          => Error;
    public VariateManager GetVariateManager() => Manager;
}