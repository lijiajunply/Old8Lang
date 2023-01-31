using System.Diagnostics;

namespace Old8LangLib;

public static class Time
{
    private static Stopwatch sw;
    public static string   GetTimeNow(string x) => DateTime.Now.ToString(x);
    public static string[] TimeFormat()         => new[] { "yyyy-MM-dd" ,"hh:mm:ss"};
    public static void TimeStart()
    {
        sw = new Stopwatch();
        sw.Start();
    }
    public static double TimeStop()
    {
        sw.Stop();
        var ts = sw.Elapsed;
        return ts.TotalMilliseconds;
    }
    public static string TimeStamp()
    {
        var a = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        return a.ToString();
    }
}