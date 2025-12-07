using System;
using System.Collections.Generic;
using Old8Lang.LangParser;

public class TestTokenizer
{
    public static void Main(string[] args)
    {
        string code = "a <- 10";
        List<LangToken> tokens = LangTokenizer.Tokenize(code);
        
        Console.WriteLine("Token序列：");
        foreach (var token in tokens)
        {
            Console.WriteLine($"值：{token.Value}, 类型：{token.Type}, 行：{token.Line}, 列：{token.Column}");
        }
    }
}
