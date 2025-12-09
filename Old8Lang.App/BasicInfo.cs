using System.Text;

namespace Old8Lang.App;

public static class BasicInfo
{
    /// <summary>
    /// 帮助文档
    /// </summary>
    /// <returns></returns>
    public static string Help => @"Old8Lang 命令行帮助文档

命令格式：
  Old8Lang.App [选项] [参数]

运行模式：
  解释模式：直接解释执行 Old8Lang 代码
  编译模式：将 Old8Lang 代码编译为中间代码后执行

可用命令：
  -f <文件路径>          解释执行指定的 .old8 或 .ol 文件
  -c <文件路径>          编译并执行指定的 .old8 或 .ol 文件
  -s <文件路径>          对指定的 .old8 或 .ol 文件进行语法测试
  info                   显示 Old8Lang 语言信息
  -var                   显示当前版本号
  import                 显示导入库信息
  -change <路径>         修改导入路径
  -h                     显示此帮助信息

使用示例：
  解释执行文件：
    Old8Lang.App -f example.old8
  
  编译执行文件：
    Old8Lang.App -c example.old8
  
  语法测试：
    Old8Lang.App -s example.old8
  
  查看语言信息：
    Old8Lang.App info
  
  查看帮助：
    Old8Lang.App -h

注意事项：
  - 仅支持 .old8 和 .ol 扩展名的文件
  - 编译模式会显示执行时间统计
  - 语法测试会显示解析时间和生成的代码结构";

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
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(Apis.CodePath)!, "Old8Lang", "LangSample.txt"));

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
        { "Remove", "-r" },
        { "Compiler", "-c" },
        { "SyntaxTest", "-s" }
    };
}