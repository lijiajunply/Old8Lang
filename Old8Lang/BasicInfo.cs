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
    public string LangSample() => " Old8Land 与 C# 的区别 \n" +
                                  " var a <- 1 == var a = 1 \n" +
                                  " a -> var b == var b = a \n" +
                                  " 在Old8Lang，变量更像是C中的指针，为了好看，这里使用 -> 或 <- \n" +
                                  " Old8Lang支持函数式(是一个多范式语言) \n" +
                                  " eg: \n" +
                                  " for(<变量名>,bool,lambda函数 , lambde函数2)//函数for == for(int i ; bool ; 语句){<语句>} \n" +
                                  "  \n";
}