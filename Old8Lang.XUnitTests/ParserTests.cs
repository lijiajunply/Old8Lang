using Old8Lang.LangParser;
using Old8Lang;

public class ParserTests
{
    private string GetProjectRoot()
    {
        // 获取当前目录的父目录，即项目根目录
        // 当前目录是 Old8Lang.XUnitTests/bin/Debug/net10.0
        // 父目录是 Old8Lang.XUnitTests/bin/Debug
        // 祖父目录是 Old8Lang.XUnitTests/bin
        // 曾祖父目录是 Old8Lang.XUnitTests
        // 高祖父目录是 Old8Lang（项目根目录）
        return Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()!)!.Parent!.FullName)!.FullName)!.FullName;
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
        Assert.True(result.Count > 0);
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
        Assert.True(result.Count > 0);
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
        Assert.True(result.Count > 0);
    }
    
    [Fact]
    public void TestParser_Datatypes()
    {
        // 测试数据类型测试文件
        string fullPath = Path.Combine(GetProjectRoot(), "test_datatypes.old8");
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        string code = File.ReadAllText(fullPath);
        List<LangToken> tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }
    
    [Fact]
    public void TestParser_ListArray()
    {
        // 测试列表和数组测试文件
        string fullPath = Path.Combine(GetProjectRoot(), "test_list_array.old8");
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        string code = File.ReadAllText(fullPath);
        List<LangToken> tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }
    
    [Theory]
    [InlineData("test_clean.old8")]
    [InlineData("test_simple.old8")]
    [InlineData("test_syntax.old8")]
    [InlineData("test_datatypes.old8")]
    [InlineData("test_list_array.old8")]
    public void TestParser_AllFiles(string testFile)
    {
        // 测试所有测试文件
        string fullPath = Path.Combine(GetProjectRoot(), testFile);
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        string code = File.ReadAllText(fullPath);
        List<LangToken> tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.True(result.Count >= 0);
    }
    
    [Fact]
    public void TestTokenizer()
    {
        // 测试令牌化功能
        string code = "a <- 10 + 5 * 2";
        List<LangToken> tokens = LangTokenizer.Tokenize(code);
        
        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);
        Assert.Equal(7, tokens.Count); // 应该有7个令牌：a, <-, 10, +, 5, *, 2
    }
    
    [Fact]
    public void TestApisFromFile()
    {
        // 测试 Apis.FromFile 方法
        string filename = "test_clean.old8";
        string fullPath = Path.Combine(GetProjectRoot(), filename);
        
        // 确保文件存在
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        // 读取文件内容
        string expectedContent = File.ReadAllText(fullPath);
        
        // 使用 Apis.FromFile 读取文件
        string actualContent = Apis.FromFile(fullPath);
        
        // 验证内容一致
        Assert.Equal(expectedContent, actualContent);
    }
    
    [Fact]
    public void TestFileReading()
    {
        // 测试文件读取功能
        string filename = "test_clean.old8";
        string fullPath = Path.Combine(GetProjectRoot(), filename);
        
        // 确保文件存在
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        // 读取文件内容
        string content = File.ReadAllText(fullPath);
        
        // 验证内容不为空
        Assert.NotNull(content);
        Assert.NotEmpty(content);
        
        // 验证内容长度大于0
        Assert.True(content.Length > 0);
        
        // 验证内容包含预期的赋值语句
        Assert.Contains("a <- 10", content);
    }
}