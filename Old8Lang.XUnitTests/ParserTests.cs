using Old8Lang.LangParser;

namespace Old8Lang.XUnitTests;

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
    
    [Fact]
    public void TestParser_Clean()
    {
        // 测试简单的赋值语句
        var code = "a <- 10";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }
    
    [Fact]
    public void TestParser_Simple()
    {
        // 测试简单的测试文件
        var fullPath = Path.Combine(GetProjectRoot(), "Test/test_simple.old8");
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        var code = File.ReadAllText(fullPath);
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }
    
    [Fact]
    public void TestParser_Syntax()
    {
        // 测试全面的语法测试文件
        var fullPath = Path.Combine(GetProjectRoot(), "Test/test_syntax.old8");
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        var code = File.ReadAllText(fullPath);
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }
    
    [Fact]
    public void TestParser_Datatypes()
    {
        // 测试数据类型测试文件
        var fullPath = Path.Combine(GetProjectRoot(), "Test/test_datatypes.old8");
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        var code = File.ReadAllText(fullPath);
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }
    
    [Fact]
    public void TestParser_ListArray()
    {
        // 测试列表和数组测试文件
        var fullPath = Path.Combine(GetProjectRoot(), "Test/test_list_array.old8");
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        var code = File.ReadAllText(fullPath);
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }
    
    [Theory]
    [InlineData("Test/test_clean.old8")]
    [InlineData("Test/test_simple.old8")]
    [InlineData("Test/test_syntax.old8")]
    [InlineData("Test/test_datatypes.old8")]
    [InlineData("Test/test_list_array.old8")]
    public void TestParser_AllFiles(string testFile)
    {
        // 测试所有测试文件
        var fullPath = Path.Combine(GetProjectRoot(), testFile);
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        var code = File.ReadAllText(fullPath);
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.True(result.Count >= 0);
    }
    
    [Fact]
    public void TestTokenizer()
    {
        // 测试令牌化功能
        var code = "a <- 10 + 5 * 2";
        var tokens = LangTokenizer.Tokenize(code);
        
        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);
        Assert.Equal(7, tokens.Count); // 应该有7个令牌：a, <-, 10, +, 5, *, 2
    }
    
    [Fact]
    public void TestTokenizer_KeywordInVariableName()
    {
        // 测试包含关键字的变量名的令牌化
        var code = "finally_executed <- 1";
        var tokens = LangTokenizer.Tokenize(code);
        
        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);
        Assert.Equal(3, tokens.Count); // 应该有3个令牌：finally_executed, <-, 1
        Assert.Equal("finally_executed", tokens[0].Value);
        Assert.Equal(LangTokenType.Identifier, tokens[0].Type);
    }
    
    [Fact]
    public void TestParser_TryCatch()
    {
        // 测试try-catch语句
        var code = "try { a <- 10 / 0 } catch { result <- 1 }";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }
    
    [Fact]
    public void TestParser_TryCatchFinally()
    {
        // 测试try-catch-finally语句
        var code = "try { a <- 10 / 0 } catch { result <- 1 } finally { final <- 1 }";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }
    
    [Fact]
    public void TestParser_MultipleCatchBlocks()
    {
        // 测试多个catch块
        var code = "try { a <- 10 / 0 } catch (ZeroDivisionError) { result <- 1 } catch { result <- 2 }";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }
    
    [Fact]
    public void TestParser_CatchWithVariable()
    {
        // 测试带异常变量的catch块
        var code = "try { a <- 10 / 0 } catch (ZeroDivisionError e) { result <- 1 }";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }
    
    [Fact]
    public void TestApisFromFile()
    {
        // 测试 Apis.FromFile 方法
        var filename = "Test/test_clean.old8";
        var fullPath = Path.Combine(GetProjectRoot(), filename);
        
        // 确保文件存在
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        // 读取文件内容
        var expectedContent = File.ReadAllText(fullPath);
        
        // 使用 Apis.FromFile 读取文件
        var actualContent = Apis.FromFile(fullPath);
        
        // 验证内容一致
        Assert.Equal(expectedContent, actualContent);
    }
    
    [Fact]
    public void TestFileReading()
    {
        // 测试文件读取功能
        var filename = "Test/test_clean.old8";
        var fullPath = Path.Combine(GetProjectRoot(), filename);
        
        // 确保文件存在
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        // 读取文件内容
        var content = File.ReadAllText(fullPath);
        
        // 验证内容不为空
        Assert.NotNull(content);
        Assert.NotEmpty(content);
        
        // 验证内容长度大于0
        Assert.True(content.Length > 0);
        
        // 验证内容包含预期的赋值语句
        Assert.Contains("a <- 10", content);
    }
}