namespace Old8Lang.Interpreter;

public abstract class AbsOutputProvider
{
    public abstract void WriteLine(string? text);
    public abstract void Write(string text);
    public abstract void Error(string text);
    public abstract string ReadLine();
    public abstract void Clear();
}