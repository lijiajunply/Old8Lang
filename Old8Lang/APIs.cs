using System.Diagnostics;
using System.Text;
using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang;
public class APIs
{
    public static string Path { get; set; }
    public static string FromFile(string filename)
    {
        if (File.Exists(filename))
        {
            string a = File.ReadAllText(filename, Encoding.UTF8);
            return a;
        }
        else
        {
            return "a <- 1";
        }
    }

    public static string FromDirectory(string DirectoryName)
    {
        StringBuilder sb = new StringBuilder();
        DirectoryInfo info = new DirectoryInfo(DirectoryName);
        var fileInfos = info.GetFiles();
        bool a = false; 
        sb.Append(FromFile(DirectoryName + "/"+info.Name));
        foreach (var VARIABLE in fileInfos)
        {
            sb.Append(FromFile(VARIABLE.FullName));
        }

        var b = info.GetDirectories();
        foreach (var VARIABLE in b)
        {
            sb.Append(FromDirectory(VARIABLE.FullName));
        }
        return sb.ToString();
    }
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

    public static string ImportSearch(string ImportString)
    {
        //
        //
        Path = System.Environment.CurrentDirectory;
        return Path + ImportString;
    }
}