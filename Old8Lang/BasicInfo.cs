using System.Text;
using Old8Lang.Lib;

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
        StringBuilder builder =
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
    public static string LangSample() =>
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(CodePath)!,"Old8Lang", "LangSample.txt"));

    /// <summary>
    /// Old8Lang的关键字
    /// </summary>
    public static string[] KeyWords { get; set; } =
    [
        "class new", // => C# : class new
        "if for while", // => C# : if , for , while
        "elif else", // => C# : else if , else
        "and or not", // => C# : and or not
        "return func"
    ];

    public static string JsonPath 
        => Path.Combine(Path.GetDirectoryName(CodePath)!,"Old8Lang", "LangInfo.json");

    public static Dictionary<string, string> Order { get; set; } = new()
    {
        { "FromFile", "-f" },
        { "FromDir", "-d" },
        { "Import", "import" },
        { "LibPath", "lib" },
        { "ChangeImport", "-cimp" },
        { "Var", "-var" },
        { "Info", "info" },
        { "Install", "-i" },
        { "Help", "-h" },
        { "Remove", "-r" }
    };

    public static List<LibInfo> OldLangLib { get; set; } =
    [
        new LibInfo() { IsDir = false, Var = 0.3, LibName = "OS" },
        new LibInfo() { IsDir = false, Var = 0.3, LibName = "File" },
        new LibInfo() { IsDir = false, Var = 0.3, LibName = "Terminal" },
        new LibInfo() { IsDir = true, Var = 0.3, LibName = "Net" },
        new LibInfo() { IsDir = false, Var = 0.3, LibName = "Time" }
    ];
    public static string CodePath
    {
        get
        {
            var directory = AppContext.BaseDirectory.Split(Path.DirectorySeparatorChar);
            var slice = new ArraySegment<string>(directory, 0, directory.Length - 4);
            return Path.Combine(slice.ToArray());
        }
    }
}