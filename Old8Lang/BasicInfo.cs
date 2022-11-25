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
    public string INFO() => "Old8Lang是一个脚本语言，主要就是为了好玩而写的[doge]";
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
        "old",      // => C# : var
        "eight",    // => C# : class
        "XAUAT",    // => C# : this
        "null",     // => C# : null
        "if for while",  // => C# : if for while
        ""
    };
}