using System.Diagnostics;
using Old8Lang.AST;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using sly.parser;
using sly.parser.generator;
using sly.parser.generator.visitor;

namespace Old8Lang.CslyParser;

public class Interpreter : IMiniInterpreter
{
    #region Code

    public readonly VariateManager Manager = new();
    private string Code { get; } = "";

    private Parser<OldTokenGeneric, OldLangTree>? parser;
    public AbsUseClass UseClass { get; set; } = new ConsoleUse();
    public bool IsCompileOptimization { get; set; }

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

    public void ParserRun()
    {
        var sw = new Stopwatch();
        sw.Start();
        var block = Build();
        sw.Stop();
        var ts = sw.Elapsed.TotalMilliseconds;
        var time = $"------------------\nParser Build Time : {ts}ms\n";
        var milliseconds = ts;

        sw.Restart();
        block.Run(Manager);
        sw.Stop();
        ts = sw.Elapsed.TotalMilliseconds;
        time += $"Process Run Time : {ts}ms\n";
        milliseconds += ts;
        time += $"Total : {milliseconds}ms";
        Console.WriteLine(time);
    }

    public BlockStatement Build(string code = "")
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
                    var lines = code.Split("\n");
                    Error.Add($"{lines[x.Line]}");
                }
                catch (Exception)
                {
                    Error.Add($"{x.ErrorType} in line {x.Line + 1} , col {x.Column}");
                    var lines = code.Split("\n");
                    Error.Add($"{lines[x.Line]}");
                }
            });
            throw new Exception(string.Join("\n", Error));
        }

        return result.Result as BlockStatement ?? new BlockStatement([]);
    }

    public List<string> GetError() => Error;
}