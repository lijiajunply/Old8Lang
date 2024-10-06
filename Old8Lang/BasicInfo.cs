using System.Reflection;
using System.Text;

namespace Old8Lang;

public static class BasicInfo
{
    /// <summary>
    /// 帮助文档
    /// </summary>
    /// <returns></returns>
    public static string Help => "笑死，根本就没有帮助这一说[doge]";

    /// <summary>
    /// 语言信息
    /// </summary>
    /// <returns></returns>
    public static string Info()
    {
        var builder =
            new StringBuilder("Old8Lang是一个脚本语言，主要就是为了好玩而写的[doge]\n使用csly(https://github.com/b3b00/csly)编写");
        builder.Append("\nthe keyword:");
        foreach (var variable in KeyWords)
            builder.Append(variable + "\n");
        builder.Append("LangSample:\n");
        builder.Append(LangSample());
        return builder.ToString();
    }

    /// <summary>
    /// 还没想好
    /// </summary>
    /// <returns>这...实例</returns>
    private static string LangSample() =>
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(CodePath)!, "Old8Lang", "LangSample.txt"));

    /// <summary>
    /// Old8Lang的关键字
    /// </summary>
    private static string[] KeyWords =>
    [
        "class new", // => C# : class new
        "if for while", // => C# : if , for , while
        "elif else", // => C# : else if , else
        "and or not", // => C# : and or not
        "return func"
    ];

    public static string JsonPath
    {
        get
        {
#if DEBUG
            return Path.Combine(Path.GetDirectoryName(CodePath)!, "Old8Lang", "LangInfo.json");
#endif
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "LangInfo.json");;
        }
    }

    public static Dictionary<string, string> Order => new()
    {
        { "FromFile", "-f" },
        { "FromDir", "-d" },
        { "Import", "import" },
        { "LibPath", "lib" },
        { "ChangeImport", "-change" },
        { "Var", "-var" },
        { "Info", "info" },
        { "Install", "-i" },
        { "Help", "-h" },
        { "Remove", "-r" }
    };

    public static string CodePath
    {
        get
        {
#if DEBUG
            var directory = AppContext.BaseDirectory.Split(Path.DirectorySeparatorChar);
            var slice = new ArraySegment<string>(directory, 0, directory.Length - 4);
            return Path.Combine(slice.ToArray());
#endif
            return "";
        }
    }
}