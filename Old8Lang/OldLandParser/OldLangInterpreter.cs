using Old8Lang.AST;
using sly.parser.generator;
using sly.parser.generator.visitor;

namespace Old8Lang.OldLandParser;

public class OldLangInterpreter
{
    private VariateManager Manager;

    private readonly List<string> Error;

    private string Path { get; set; }

    public OldLangInterpreter(string path)
    {
        Path    = path;
        Error   = new List<string>();
        Manager = new VariateManager { Path = path };
    }

    public void Use()
    {

        var Parser = new ParserBuilder<OldTokenGeneric,OldLangTree>();
        var oldParser = new OldParser();
        var parserBuilder = Parser.BuildParser(oldParser,
                                               ParserType.EBNF_LL_RECURSIVE_DESCENT,"root");
        var buildResult = parserBuilder.Result;

        var result = buildResult.Parse(APIs.FromFile(Path));
        var RUN    = result.Result;
        if (result.Errors != null && result.Errors.Any())
            result.Errors.ForEach(x => Error.Add(x.ToString()));
        else
        {
            var run = RUN as OldStatement;
            Console.WriteLine(run);
            run.Run(ref Manager);
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