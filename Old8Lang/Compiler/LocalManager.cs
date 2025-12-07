using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser;

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

public interface IMiniInterpreter
{
    public BlockStatement Build(string code);
    public AbsUseClass UseClass { get; set; }
    public bool IsCompileOptimization { get; set; }
}