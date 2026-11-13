namespace Old8Lang.Interpreter;

public class ConsoleOutputProvider : AbsOutputProvider
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

    public override string ReadLine()
    {
        return Console.ReadLine() ?? string.Empty;
    }

    public override void Clear()
    {
        Console.Clear();
    }
}