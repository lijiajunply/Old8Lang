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
            Console.WriteLine($"值：{token.Value}, 类型：{token.Type}, 行：{token.Line}, 