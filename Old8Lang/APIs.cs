using System.Diagnostics;
using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang;
public class APIs
{
    public static (VariateManager Manager ,List<string> Error, string Time) CslyUsing(string code)
    {
        var a = new OldLangInterpreter(code);
        
        Stopwatch sw = new Stopwatch();
        sw.Start();

        a.Use();

        sw.Stop();
        TimeSpan ts = sw.Elapsed;
        
        return (a.GetVariateManager(), a.GetError(),$"DateTime costed for Shuffle function is: {ts.TotalMilliseconds}ms");
    }
}