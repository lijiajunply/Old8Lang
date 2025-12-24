using System.Text;

namespace Old8Lang.App;

/// <summary>
/// 命令行应用的基本信息类，提供帮助文档、语言信息和命令映射
/// </summary>
public static class BasicInfo
{
    /// <summary>
    /// 获取 Old8Lang 命令行帮助文档
    /// </summary>
    /// <returns>格式化的帮助文档字符串</returns>
    public static string Help => @"Old8Lang 命令行帮助文档

命令格式：
  Old8Lang.App [选项] [参数]

运行模式：
  解释模式：直接解释执行 Old8Lang 代码
  编译模式：将 Old8Lang 代码编译为中间代码后执行

可用命令：
  执行命令：
    -f <文件路径>          解释执行指定的 .old8 或 .ol 文件
    -c <文件路径>          编译并执行指定的 .old8 或 .ol 文件
    -s <文件路径>          对指定的 .old8 或 .ol 文件进行语法测试

  项目管理命令：
    init                   初始化 Old8Lang 项目
    add <包名>             添加依赖包到项目
    remove <包名>          从项目移除依赖包
    install                安装项目的所有依赖
    list, ls               列出已安装的依赖包
    venv <子命令>          管理虚拟环境

  信息命令：
    info                   显示 Old8Lang 语言信息
    -var                   显示当前版本号
    import                 显示导入库信息
    -change <路径>         修改导入路径
    -h                     显示此帮助信息

  调试选项：
    -d, --debug            启用调试输出，显示详细的编译过程信息
    -l, --log-level <级别> 设置日志输出级别 (error, warning, info, debug)

使用示例：
  执行文件：
    Old8Lang.App -f example.old8
    Old8Lang.App -c example.old8

  项目管理：
    Old8Lang.App init                    # 初始化项目
    Old8Lang.App add Logger              # 添加包
    Old8Lang.App install                 # 安装依赖
    Old8Lang.App list                    # 列出已安装包
    Old8Lang.App venv status             # 查看虚拟环境状态

  查看信息：
    Old8Lang.App info
    Old8Lang.App -h

注意事项：
  - 仅支持 .old8 和 .ol 扩展名的文件
  - 编译模式会显示执行时间统计
  - 语法测试会显示解析时间和生成的代码结构
  - 项目管理命令需要在项目目录中执行（init 除外）";

    /// <summary>
    /// 获取 Old8Lang 语言的详细信息，包括关键字和示例代码
    /// </summary>
    /// <returns>格式化的语言信息字符串</returns>
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
    /// 从文件中读取 Old8Lang 示例代码
    /// </summary>
    /// <returns>示例代码字符串</returns>
    private static string LangSample()
    {
        var samplePath = Path.Combine(Path.GetDirectoryName(Apis.CodePath)!, "Old8Lang", "LangSample.txt");
        return File.Exists(samplePath) 
            ? File.ReadAllText(samplePath) 
            : "// LangSample.txt not found\nPrintLine(\"Hello, Old8Lang!\")";
    }

    /// <summary>
    /// Old8Lang 语言的关键字列表
    /// </summary>
    private static string[] KeyWords =>
    [
        "class new", // => C# : class new
        "if for while", // => C# : if , for , while
        "elif else", // => C# : else if , else
        "and or not", // => C# : and or not
        "return func"
    ];

    /// <summary>
    /// 命令名称与命令行参数的映射字典
    /// </summary>
    public static Dictionary<string, string> Order => new()
    {
        { "FromFile", "-f" },        // 解释执行文件
        { "FromDir", "-d" },        // 从目录加载
        { "Import", "import" },     // 显示导入库信息
        { "LibPath", "lib" },       // 库路径
        { "ChangeImport", "-change" }, // 修改导入路径
        { "Var", "-var" },          // 显示版本号
        { "Info", "info" },         // 显示语言信息
        { "Install", "-i" },        // 安装库
        { "Help", "-h" },           // 显示帮助
        { "Remove", "-r" },         // 移除库
        { "Compiler", "-c" },       // 编译执行
        { "SyntaxTest", "-s" }       // 语法测试
    };
}