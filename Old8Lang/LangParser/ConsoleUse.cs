namespace Old8Lang.LangParser;

public class ConsoleUse : AbsUseClass
{
    public override void WriteLine(string? text)
    {
        Console.WriteLine(text);
    }

    public override void Write(string text)
    {
        Console.Write(text);
    }

    public override void Error(string text)
    {
        Console.Error.WriteLine(text);
    }

    public override void Error(Exception e)
    {
        Console.Error.WriteLine(e);
    }

    public override string ReadLine()
    {
        return Console.ReadLine() ?? string.Empty;
    }

    public override void Clear()
    {
        Console.Clear();
    }
}