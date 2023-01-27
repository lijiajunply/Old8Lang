using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Old8Lang.AST.Expression;
using Old8Lang.OldLandParser;
using ValueType = Old8Lang.AST.Expression.ValueType;

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
    public static List<object> ListToObjects(List<ValueType> a) =>
        a.Count == 0 ? new List<object>() : a.Select(x => x.GetValue()).ToList();

    #endregion

    #region ReadFileOrDir

    public static string FromFile(string filename) => File.ReadAllText(filename,Encoding.UTF8);

    public static string FromDirectory(string DirectoryName)
    {
        var sb        = new StringBuilder();
        var info      = new DirectoryInfo(DirectoryName);
        var           fileInfos = info.GetFiles();
        var           a         = false;
        sb.Append(FromFile(DirectoryName+"/"+"init.ws"));
        foreach (var VARIABLE in fileInfos)
            sb.Append(FromFile(VARIABLE.FullName));


        var b = info.GetDirectories();
        foreach (var VARIABLE in b)
            sb.Append(FromDirectory(VARIABLE.FullName));

        return sb.ToString();
    }

    #endregion

    public static (VariateManager Manager,List<string> Error,string Time) CslyUsing(string code,bool isdir)
    {
        var a = new Interpreter(code,isdir);

        Stopwatch sw = new Stopwatch();
        sw.Start();
        a.Use();
        sw.Stop();
        TimeSpan ts = sw.Elapsed;

        return (a.GetVariateManager(),a.GetError(),
                $"DateTime costed for Shuffle function is: {ts.TotalMilliseconds}ms");
    }

    public static LangInfo JSON_Info(string langlib,string import,string ver,string uri = "https://downland.old8lang.com")
    {
        var      dictionary = new Dictionary<string,bool>() { { "OS",false }, {"Terminal",false },{"File",false} };
        LangInfo langInfo   = new LangInfo() { ImPortTable = dictionary,LangLibDllPath = langlib,ImportPath = import,Ver = ver,Url = uri};
        string   jsonString = JsonSerializer.Serialize(langInfo);
        File.WriteAllText(BasicInfo.JSONPath,jsonString);
        return langInfo;
    }
    public static LangInfo Read_JSON()
    {
        var jsonString = File.ReadAllText(BasicInfo.JSONPath);
        LangInfo a          = JsonSerializer.Deserialize<LangInfo>(jsonString);
        return a;
    }
    public static bool ImportInstall(string context)
    {
        //以后再说
        return false;
    }
}