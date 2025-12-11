using System.Diagnostics;

namespace Old8LangLib;

public static class Time
{
    private static readonly Stopwatch Sw = Stopwatch.StartNew();
    public static string GetTimeNow(string x) => DateTime.Now.ToString(x);
    public static string[] TimeFormat() => ["yyyy-MM-dd", "hh:mm:ss"];

    public static void TimeStart()
    {
        Sw.Start();
    }

    public static double TimeStop()
    {
        Sw.Stop();
        var ts = Sw.Elapsed;
        return ts.TotalMilliseconds;
    }

    public static string TimeStamp()
    {
        var a = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        return a.ToString();
    }
}