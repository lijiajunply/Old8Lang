namespace Old8LangLib;

public static class Terminal
{
    public static void Title(string title) => Console.Title = title;
    public static void Print(string context) => Console.Write(context);
    public static void PrintLine(string context) => Console.WriteLine(context);
    public static int ReadAsllc() => Console.Read();
    public static string ReadLine() => Console.ReadLine();
    public static string ReadKey() => Console.ReadKey().Key.ToString();
    public static void Beep() => Console.Beep();
    public static void Clear() => Console.Clear();
}