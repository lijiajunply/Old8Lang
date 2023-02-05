using System.Diagnostics;
using Old8Lang.AST;
using sly.lexer;
using sly.parser.generator;
using sly.parser.generator.visitor;

namespace Old8Lang.OldLandParser;

public class Interpreter
{
    private          string         Time { get; set; }
    private          VariateManager Manager;
    private          double         DoubleTime { get; set; }
    private readonly List<string>   Error;

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
        Manager = new VariateManager { Path = path ,LangInfo = info};
        Manager.Init();
        Code    = isDir ? APIs.FromDirectory(path) : APIs.FromFile(path);
    }

    public void Run(bool Dot = false)
    {
        //Build(Dot);
        Stopwatch sw = new Stopwatch();
        sw.Start();
        if (Manager.LangInfo is null)
            Manager.LangInfo = APIs.Read_JSON();

        var Parser = new ParserBuilder<OldTokenGeneric,OldLangTree>();
        var oldParser = new OldParser();
        var parserBuilder = Parser.BuildParser(oldParser,
                                               ParserType.EBNF_LL_RECURSIVE_DESCENT,"root");
        var buildResult = parserBuilder.Result;
        
        var result = buildResult.Parse(Code);
        var run    = result.Result as OldStatement;
        sw.Stop();
        TimeSpan ts = sw.Elapsed;
        Time       += $"Parser Build Time : {ts.TotalMilliseconds}ms\n";
        DoubleTime += ts.TotalMilliseconds;
        if (result.Errors != null && result.Errors.Any()) 
            result.Errors.ForEach(x => Error.Add(x.ToString()));
        else
        {
            sw = new Stopwatch();
            sw.Start();
            run.Run(ref Manager);
            sw.Stop();
            ts         =  sw.Elapsed;
            Time       += $"Process Run Time : {ts.TotalMilliseconds}ms\n";
            DoubleTime += ts.TotalMilliseconds;
            // .dot
            if (Dot)
            {
                string dic      = Path.GetDirectoryName(Manager.Path);
                var    path     = dic+$"/tree_By_{Path.GetFileNameWithoutExtension(Manager.Path)}.dot";
                var    tree     = result.SyntaxTree;
                var    graphviz = new GraphVizEBNFSyntaxTreeVisitor<OldTokenGeneric>();
                var    _        = graphviz.VisitTree(tree);
                var    graph    = graphviz.Graph.Compile();
                File.WriteAllText(path,graph);
            }
        }
        Time += $"Total : {DoubleTime}ms";
    }
    public void Build(bool isUse = false)
    {
        if (isUse)
        {
            Stopwatch               sw     = new Stopwatch();
            sw.Start();
            ILexer<OldTokenGeneric> lexer  = LexerBuilder.BuildLexer<OldTokenGeneric>().Result;
            var                     tokens = lexer.Tokenize(Code).Tokens.Tokens;
            foreach (var token in tokens)
            {
                Console.Write($"[{token.Value} in {token.Position}] ");
            }
            sw.Stop();
            var ts = sw.Elapsed;
            Time       += $"Lexer Build Time : {ts.TotalMilliseconds}ms\n";
            DoubleTime += ts.TotalMilliseconds;
        }
    }
    public string GetTime() => Time;
    public List<string>   GetError()          => Error;
    public VariateManager GetVariateManager() => Manager;
}