namespace Old8Lang.CslyParser;

public class ConsoleUse : AbsUseClass
{
    public override Action<string> Write => Console.Write;
    public override Action<string> WriteLine => Console.WriteLine;
}