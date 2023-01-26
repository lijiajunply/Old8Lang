namespace Old8LangLib;

public static class Terminal
{
    public static void   Title(string     title)   => Console.Title = title;
    public static void   Print(object     context) => Console.Write(context);
    public static void   PrintLine(object context) => Console.WriteLine(context);
    public static int    ReadAscii()               => Console.Read();
    public static string ReadLine()                => Console.ReadLine();
    public static string ReadKey()                 => Console.ReadKey().Key.ToString();
    public static void   Beep()                    => Console.Beep();
    public static void   Clear()                   => Console.Clear();
    public static void BeepWindow(string tone,string duration) =>
        Console.Beep((int)Enum.Parse<Tone>(tone),(int)Enum.Parse<Duration>(duration));

    private enum Tone
    {
        REST    = 0,
        GbelowC = 196,
        A       = 220,
        Asharp  = 233,
        B       = 247,
        C       = 262,
        Csharp  = 277,
        D       = 294,
        Dsharp  = 311,
        E       = 330,
        F       = 349,
        Fsharp  = 370,
        G       = 392,
        Gsharp  = 415,
    }

// Define the duration of a note in units of milliseconds.
    private enum Duration
    {
        WHOLE     = 1600,
        HALF      = WHOLE   / 2,
        QUARTER   = HALF    / 2,
        EIGHTH    = QUARTER / 2,
        SIXTEENTH = EIGHTH  / 2,
    }
}