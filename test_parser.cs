using System;
using System.Collections.Generic;
using Old8Lang.LangParser;

public class TestParser
{
    public static void Main(string[] args)
    {
        string code = "a <- 10";
        Console.WriteLine("原始代码：" + code);
        
        // 令牌化
        List<LangToken> tokens = LangTokenizer.Tokenize(code);
        Console.WriteLine("\nToken序列：");
        foreach (var token in tokens)
        {
            Console.WriteLine($"值：{token.Value}, 类型：{token.Type}, 行：{token.Line}, 列：{token.Column}");
        }
        
        // 解析
        try
        {
            var parser = new LangParser(tokens);
            var result = parser.ParseProgram();
            Console.WriteLine("\n解析成功！");
            Console.WriteLine("结果：" + result);
        }
        catch (Exception e)
        {
            Console.WriteLine($"\n解析失败：{e.Message}");
            Console.WriteLine(e.StackTrace);
        }
    }
}
