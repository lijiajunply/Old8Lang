namespace Old8Lang.CslyParser;

public abstract class AbsUseClass
{
    public abstract Action<string> Write { get;}
    public abstract Action<string> WriteLine { get; }
}