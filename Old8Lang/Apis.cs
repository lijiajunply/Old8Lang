using System.Reflection;
using System.Text;
using System.Text.Json;
using ValueType = Old8Lang.AST.Expression.ValueType;
using Old8Lang.CslyParser;

namespace Old8Lang;

public static class Apis
{
    #region OtherFunc

    public static string ListToString<T>(List<T>? a)
    {
        if (a == null)
            return "";
        var builder = new StringBuilder();
        for (var i = 0; i < a.Count; i++)
            builder.Append(a[i] + (i == a.Count - 1 ? "" : " "));

        return builder.ToString();
    }

    public static string ArrayToString<T>(T[] a)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < a.Length; i++)
        {
            var b = i == 0 && a.Length == 1 || i == a.Length - 1 ? "" : ",";
            builder.Append(a[i] + b);
        }

        return builder.ToString();
    }

    public static List<object> ListToObjects(List<ValueType>? a)
    {
        if (a == null)
            return [];
        if (a.Count == 0)
            return [];
        return a[0] == null! ? [] : a.Select(x => x.GetValue()).ToList();
    }

    #endregion

    #region ReadFileOrDir

    public static string FromFile(string filename)
        => File.Exists(filename) ? File.ReadAllText(filename, Encoding.UTF8) : filename;


    public static string FromDirectory(string DirectoryName)
    {
        var builder = new StringBuilder();
        builder.Append(FromFile(DirectoryName + "/" + "init.ws"));
        return builder.ToString();
    }

    #endregion

    public static void CslyUsing(string path, bool isDir)
    {
        var a = new Interpreter(path, isDir);
        a.ParserRun();
    }

    public static LangInfo ChangeBasicInfo(string import, string ver, string uri = "https://downland.old8lang.com")
    {
        var langInfo = new LangInfo { LibInfos = ReadJson().LibInfos, ImportPath = import, Ver = ver, Url = uri };
        var jsonString = JsonSerializer.Serialize(langInfo);
        File.WriteAllText(BasicInfo.JsonPath, jsonString);
        return langInfo;
    }

    public static LangInfo ReadJson()
    {
        var jsonString = FromFile(BasicInfo.JsonPath);
        var a = JsonSerializer.Deserialize<LangInfo>(jsonString)!;
        if (Directory.Exists(a.ImportPath)) return a;
        var s = Path.GetDirectoryName(BasicInfo.CodePath);
#if RELEASE
        s = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#endif
        a.ImportPath = Path.Combine(s ?? "", "Old8LangLib", "OldLib");

        return a;
    }

    public static bool ImportInstall(string context)
    {
        //以后再说
        if (string.IsNullOrEmpty(context)) return false;
        //var _ = new HttpClient();

        return false;
    }
}