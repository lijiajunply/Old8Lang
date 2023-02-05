using System.Drawing;
using Console = Colorful.Console;

namespace Old8LangLib;

public static class ColorfulTerminal
{
    public static void PrintColorful(string context,string color) => Console.Write(context,Color.FromName(color));
    public static void PrintLineColorful(string context,string color) =>
        Console.WriteLine(context,Color.FromName(color));
    public static void PrintAscii(string context) => Console.WriteAscii(context);
    public static void PrintAsciiColorful(string context,string color) =>
        Console.WriteAscii(context,Color.FromName(color));
}