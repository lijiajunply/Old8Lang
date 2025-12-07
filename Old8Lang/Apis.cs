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
            builder.Append(a[i] + (i == a.Count - 1 ? "" : ","));

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
    {
        // 处理macOS上缺少开头斜杠的绝对路径
        if (filename.StartsWith("Users/") || filename.StartsWith("Volumes/"))
        {
            filename = "/" + filename;
        }
        
        // 如果是绝对路径，直接使用
        if (Path.IsPathFullyQualified(filename))
        {
            return File.Exists(filename) ? File.ReadAllText(filename, Encoding.UTF8) : filename;
        }
        
        // 如果是相对路径，尝试从当前目录或应用程序目录查找
        var fullPath = Path.GetFullPath(filename);
        if (File.Exists(fullPath))
        {
            return File.ReadAllText(fullPath, Encoding.UTF8);
        }
        
        // 尝试从应用程序基目录查找
        var appPath = Path.Combine(AppContext.BaseDirectory, filename);
        if (File.Exists(appPath))
        {
            return File.ReadAllText(appPath, Encoding.UTF8);
        }
        
        // 所有尝试都失败，返回原始文件名
        return filename;
    }


    public static string FromDirectory(string directoryName)
    {
        var builder = new StringBuilder();
        builder.Append(FromFile(directoryName + "/" + "init.ws"));
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
        LangInfo langInfo;
        if (File.Exists(BasicInfo.JsonPath))
        {
            var jsonString = File.ReadAllText(BasicInfo.JsonPath, Encoding.UTF8);
            langInfo = JsonSerializer.Deserialize<LangInfo>(jsonString)!;
        }
        else
        {
            // 如果文件不存在，创建一个默认的 LangInfo 对象
            langInfo = new LangInfo { LibInfos = [], Ver = "1.0.0", Url = "https://downland.old8lang.com" };
        }
        
        if (Directory.Exists(langInfo.ImportPath)) return langInfo;
        var s = Path.GetDirectoryName(BasicInfo.CodePath);
#if RELEASE
        s = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#endif
        langInfo.ImportPath = Path.Combine(s ?? "", "Old8LangLib", "OldLib");

        return langInfo;
    }

    public static bool ImportInstall(string context)
    {
        //以后再说
        if (string.IsNullOrEmpty(context)) return false;
        //var _ = new HttpClient();

        return false;
    }
}