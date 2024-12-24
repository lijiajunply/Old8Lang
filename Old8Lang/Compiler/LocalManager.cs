using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Statement;
using Old8Lang.CslyParser;
using sly.parser;
using sly.parser.generator;

namespace Old8Lang.Compiler;

public class LocalManager
{
    private readonly Dictionary<string, LocalBuilder> LocalVar = [];
    public readonly Dictionary<string, MethodInfo> DelegateVar = [];
    public readonly Dictionary<string, Type> ClassVar = [];
    public Type? InClassEnv { get; init; }
    public string FilePath { get; set; } = "";
    public MiniInterpreter Interpreter { get; init; } = new();

    public LocalManager New()
    {
        return new LocalManager() { FilePath = FilePath, Interpreter = Interpreter };
    }

    public LocalBuilder? GetLocalVar(string name)
    {
        return LocalVar.GetValueOrDefault(name);
    }

    public void AddLocalVar(string name, LocalBuilder index)
    {
        LocalVar[name] = index;
    }

    public void RemoveLocalVar(string name)
    {
        LocalVar.Remove(name);
    }

    public bool IsHasVar(string name) => LocalVar.ContainsKey(name);

    public int GetCount() => LocalVar.Count;
}

public class MiniInterpreter
{
    private readonly Parser<OldTokenGeneric, OldLangTree>? parser;

    public MiniInterpreter()
    {
        var Parser = new ParserBuilder<OldTokenGeneric, OldLangTree>();
        var oldParser = new OldParser();
        var parserBuilder = Parser.BuildParser(oldParser,
            ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
        parser = parserBuilder.Result;
    }

    public BlockStatement Build(string code)
    {
        var result = parser?.Parse(code);

        if (result == null) throw new Exception("语法出错");
        List<string> Error = [];
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
}