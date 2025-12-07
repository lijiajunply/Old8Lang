using System.Text;

namespace Old8Lang.LangParser;

public abstract class AbsUseClass
{
    public abstract void WriteLine(string text);
    public abstract void Write(string text);
    public abstract void Error(string text);
    public abstract void Error(Exception e);
    public abstract string ReadLine();
    public abstract void Clear();
}