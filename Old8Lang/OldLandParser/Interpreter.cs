using Old8Lang.AST;
using sly.lexer;
using sly.parser.generator;
using sly.parser.generator.visitor;

namespace Old8Lang.OldLandParser;

public class Interpreter
{
    private VariateManager Manager;

    private readonly List<string> Error;

    private string Code { get; set; }

    public Interpreter(string path,bool isDir)
    {
        Error   = new List<string>();
        Manager = new VariateManager { Path = path };
        Code    = isDir ? APIs.FromDirectory(path) : APIs.FromFile(path);
    }

    public void Use()
    {

        var Parser = new ParserBuilder<OldTokenGeneric,OldLangTree>();
        var oldParser = new OldParser();
        var parserBuilder = Parser.BuildParser(oldParser,
                                               ParserType.EBNF_LL_RECURSIVE_DESCENT,"root");
        var buildResult = parserBuilder.Result;
        
        var result = buildResult.Parse(Code);
        var run    = result.Result as OldStatement;
        if (result.Errors != null && result.Errors.Any()) 
            result.Errors.ForEach(x => Error.Add(x.ToString()));
        else
        {
            //Console.WriteLine(run);
            run.Run(ref Manager);
            // .dot
            var tree     = result.SyntaxTree;
            var graphviz = new GraphVizEBNFSyntaxTreeVisitor<OldTokenGeneric>();
            var _        = graphviz.VisitTree(tree);
            var graph    = graphviz.Graph.Compile();
            File.WriteAllLines("tree.dot",graph.Split("\n"));
        }
    }

    public List<string>   GetError()          => Error;
    public VariateManager GetVariateManager() => Manager;
}