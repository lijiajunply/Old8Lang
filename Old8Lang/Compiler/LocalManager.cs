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
    public IMiniInterpreter? Interpreter { get; init; }

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

public class MiniInterpreter : IMiniInterpreter
{
    private readonly Parser<OldTokenGeneric, OldLangTree>? Parser;

    public MiniInterpreter()
    {
        var parser = new ParserBuilder<OldTokenGeneric, OldLangTree>();
        var oldParser = new OldParser();
        var parserBuilder = parser.BuildParser(oldParser,
            ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
        Parser = parserBuilder.Result;
    }

    public BlockStatement Build(string code)
    {
        var result = Parser?.Parse(code);

        if (result == null) throw new Exception("语法出错");
        List<string> error = [];
        if (error.Count != 0) error.Clear();
        if (result.Errors == null || result.Errors.Count == 0)
            return result.Result as BlockStatement ?? new BlockStatement([]);
        result.Errors.ForEach(x =>
        {
            try
            {
                error.Add($"{x.ErrorType} : {x.ErrorMessage ?? ""}");
                var lines = code.Split("\n");
                error.Add($"{lines[x.Line]}");
            }
            catch (Exception)
            {
                error.Add($"{x.ErrorType} in line {x.Line + 1} , col {x.Column}");
                var lines = code.Split("\n");
                error.Add($"{lines[x.Line]}");
            }
        });
        throw new Exception(string.Join("\n", error));
    }

    public AbsUseClass UseClass { get; set; } = new ConsoleUse();
    public bool IsCompileOptimization { get; set; }
}

public interface IMiniInterpreter
{
    public BlockStatement Build(string code);
    public AbsUseClass UseClass { get; set; }
    public bool IsCompileOptimization { get; set; }
}