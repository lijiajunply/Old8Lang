namespace Old8Lang;

public class BasicInfo
{
    /// <summary>
    /// 帮助文档
    /// </summary>
    /// <returns></returns>
    public string HELP() => "笑死，根本就没有帮助这一说[doge]";
    /// <summary>
    /// 语言信息
    /// </summary>
    /// <returns></returns>
    public string INFO() => "Old8Lang是一个脚本语言，主要就是为了好玩而写的[doge]\n" +
                            "使用两种方式书写：自己手撸和使用csly(https://github.com/b3b00/csly)编写";
    /// <summary>
    /// 还没想好
    /// </summary>
    /// <returns>这...实例</returns>
    public string LangSample() => File.ReadAllText("LangSample.txt");
    /// <summary>
    /// Old8Lang的关键字
    /// </summary>
    public string[] KeyWords { get; set; } =
    {
        "class",    // => C# : class
        "this",    // => C# : this
        "null",     // => C# : null
        "if for while",  // => C# : if , for , while
        "elif else", // => C# : else if , else
    };
    /// <summary>
    /// 文档，使用Old8Down书写(之后，现在先不这样写)
    /// </summary>
    /// <returns></returns>
    public string Docs() => "学习资料：\n" +
                            "1:《两周自制脚本语言》\n" +
                            "2: https://www.bilibili.com/video/BV15v41147Zg （国内）/ " +
                            "https://www.youtube.com/watch?v=wgHIkdUQbp0&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y (国外) \n" +
                            "3: https://www.zhihu.com/column/c_1383722427357159424  https://www.zhihu.com/column/c_1538128122850877440 \n" +
                            "";
}