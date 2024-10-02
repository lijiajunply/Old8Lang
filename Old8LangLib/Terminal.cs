using System.Runtime.Versioning;

namespace Old8LangLib;

public static class Terminal
{
    public static void Title(string title) => Console.Title = title;
    public static int ReadAscii() => Console.Read();
    public static string ReadLine() => Console.ReadLine() ?? "";
    public static string ReadKey() => Console.ReadKey().Key.ToString();
    public static void Beep() => Console.Beep();
    public static void Clear() => Console.Clear();

    [SupportedOSPlatform("windows")]
    public static void BeepWindow(string tone, string duration) =>
        Console.Beep((int)Enum.Parse<Tone>(tone), (int)Enum.Parse<Duration>(duration));

    // ReSharper disable UnusedMember.Local
    private enum Tone
    {
        REST = 0,
        GBelowC = 196,
        A = 220,
        ASharp = 233,
        B = 247,
        C = 262,
        Csharp = 277,
        D = 294,
        DSharp = 311,
        E = 330,
        F = 349,
        Fsharp = 370,
        G = 392,
        GSharp = 415,
    }

    // Define the duration of a note in units of milliseconds.
    private enum Duration
    {
        WHOLE = 1600,
        HALF = WHOLE / 2,
        QUARTER = HALF / 2,
        EIGHTH = QUARTER / 2,
        SIXTEENTH = EIGHTH / 2,
    }
}