using System.Diagnostics;
using Old8Lang.AST;
using Old8Lang.AST.Statement;
using sly.parser;
using sly.parser.generator;
using sly.parser.generator.visitor;

namespace Old8Lang.CslyParser;

public class Interpreter
{
    #region Code

    public VariateManager Manager = new();
    private string Code { get; } = "";

    private Parser<OldTokenGeneric, OldLangTree>? parser;
    public AbsUseClass UseClass { get; set; } = new ConsoleUse();

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
        var ts = sw.Elapsed.TotalMilliseconds;
        var time = $"------------------\nParser Build Time : {ts}ms\n";
        var milliseconds = ts;

        sw.Restart();
        block.Run( Manager);
        sw.Stop();
        ts = sw.Elapsed.TotalMilliseconds;
        time += $"Process Run Time : {ts}ms\n";
        milliseconds += ts;
        time += $"Total : {milliseconds}ms";
        Console.WriteLine(time);
    }

    public BlockStatement Build(bool isDot = false, string code = "")
    {
        code = string.IsNullOrEmpty(code) ? Code : code;
        var result = parser?.Parse(code);

        if (result == null) throw new Exception("语法出错");

        if (Error.Count != 0) Error.Clear();
        if (result.Errors != null && result.Errors.Count != 0)
        {
            result.Errors.ForEach(x =>
            {
                try
                {
                    Error.Add($"{x.ErrorType} : {x.ErrorMessage ?? ""}");
                }catch (Exception)
                {
                    Error.Add(x.ErrorType.ToString());
                }
            });
            throw new Exception(string.Join("\n", Error));
        }

        if (isDot) Dot(result);
        return result.Result as BlockStatement ?? new BlockStatement([]);
    }

    private void Dot(ParseResult<OldTokenGeneric, OldLangTree> result)
    {
        var tree = result.SyntaxTree;
        var graphviz = new GraphVizEBNFSyntaxTreeVisitor<OldTokenGeneric>();
        graphviz.VisitTree(tree);
        File.WriteAllText(Manager.Path.Replace("ws", "dot"), graphviz.Graph.Compile());
    }

    public List<string> GetError() => Error;
}