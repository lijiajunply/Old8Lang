using System;using System.Collections.Generic;using System.IO;using Xunit;using Old8Lang.LangParser;

public class ParserTests
{
    private string GetProjectRoot()
    {
        // 获取当前目录的父目录，即项目根目录
        return Directory.GetParent(Directory.GetCurrentDirectory())!.FullName;
    }
    
    private List<string> GetTestFiles()
    {
        return new List<string>
        {
            "test_clean.old8",
            "test_simple.old8",
            "test_syntax.old8",
            "test_datatypes.old8",
            "test_list_array.old8"
        };
    }
    
    [Fact]
    public void TestParser_Clean()
    {
        // 测试简单的赋值语句
        string code = "a <- 10";
        List<LangToken> tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.NotEmpty(result.Statements);
    }
    
    [Fact]
    public void TestParser_Simple()
    {
        // 测试简单的测试文件
        string fullPath = Path.Combine(GetProjectRoot(), "test_simple.old8");
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        string code = File.ReadAllText(fullPath);
        List<LangToken> tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.NotEmpty(result.Statements);
    }
    
    [Fact]
    public void TestParser_Syntax()
    {
        // 测试全面的语法测试文件
        string fullPath = Path.Combine(GetProjectRoot(), "test_syntax.old8");
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        string code = File.ReadAllText(fullPath);
        List<LangToken> tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.NotEmpty(result.Statements);
    }
    
    [Fact]
    public void TestParser_Datatypes()
    {
        // 测试数据类型测试文件
        string fullPath = Path.Combine(GetProjectRoot(), "test_datatypes.old8");
       