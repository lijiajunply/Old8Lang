using System.Diagnostics;
using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.OldLandParser;

namespace Old8Lang;

public class APIs
{
    #region OtherFunc

    public static string ListToString<T>(List<T> a)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < a.Count; i++)
        {
            var b = i == 0 && a.Count == 1 || i == a.Count-1 ? "" : ",";
            builder.Append(a[i]+b);
        }
        return builder.ToString();
    }
    public static string ArrayToString<T>(T[] a)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < a.Length; i++)
        {
            var b = i == 0 && a.Length == 1 || i == a.Length-1 ? "" : ",";
            builder.Append(a[i]+b);
        }
        return builder.ToString();
    }
    public static List<object> ListToObjects(List<OldValue> a) => a.Select(x => x.Value).ToList();

    #endregion
    
    

    public static string Path { get; set; }

    public static string FromFile(string filename)
    {
        if (File.Exists(filename))
        {
            string a = File.ReadAllText(filename,Encoding.UTF8);
            return a;
        }
        else
        {
            return "a <- 1";
        }
    }

    public static string FromDirectory(string DirectoryName)
    {
        StringBuilder sb        = new StringBuilder();
        DirectoryInfo info      = new DirectoryInfo(DirectoryName);
        var           fileInfos = info.GetFiles();
        bool          a         = false;
        sb.Append(FromFile(DirectoryName+"/"+info.Name));
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
    public static (VariateManager Manager,List<string> Error,string Time) CslyUsing(string code)
    {
        var a = new OldLangInterpreter(code);

        Stopwatch sw = new Stopwatch();
        sw.Start();

        a.Use();

        sw.Stop();
        TimeSpan ts = sw.Elapsed;

        return (a.GetVariateManager(),a.GetError(),
                $"DateTime costed for Shuffle function is: {ts.TotalMilliseconds}ms");
    }

    public static string ImportSearch(string ImportString)
    {
        Path = Environment.CurrentDirectory;
        return Path+ImportString;
    }
}