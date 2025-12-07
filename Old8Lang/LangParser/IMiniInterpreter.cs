namespace Old8Lang.LangParser;

public interface IMiniInterpreter
{
    public AbsUseClass UseClass { get; set; }
    public bool IsCompileOptimization { get; set; }
}