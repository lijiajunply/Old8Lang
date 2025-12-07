using Old8Lang.LangParser;

namespace Old8Lang.XUnitTests;

public class CompilerTests
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
    public void TestCompiler_BasicExpressions()
    {
        // 测试编译基本表达式类型
        var fullPath = Path.Combine(GetProjectRoot(), "CompilerTest/test_basic_expressions.old8");
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        // 创建解释器实例
        var interpreter = new LangInterpreter();
        
        // 编译代码
        var action = Old8Lang.Compiler.Compiler.Compile(fullPath, interpreter);
        
        // 验证编译结果不为空
        Assert.NotNull(action);
    }
    
    [Fact]
    public void TestCompiler_ComplexDataTypes()
    {
        // 测试编译复杂数据类型
        var fullPath = Path.Combine(GetProjectRoot(), "CompilerTest/test_complex_data_types.old8");
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        // 创建解释器实例
        var interpreter = new LangInterpreter();
        
        // 编译代码
        var action = Old8Lang.Compiler.Compiler.Compile(fullPath, interpreter);
        
        // 验证编译结果不为空
        Assert.NotNull(action);
    }
    
    [Fact]
    public void TestCompiler_FunctionsAndAny()
    {
        // 测试编译函数和AnyValue类型
        var fullPath = Path.Combine(GetProjectRoot(), "CompilerTest/test_functions_and_any.old8");
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        // 创建解释器实例
        var interpreter = new LangInterpreter();
        
        // 编译代码
        var action = Old8Lang.Compiler.Compiler.Compile(fullPath, interpreter);
        
        // 验证编译结果不为空
        Assert.NotNull(action);
    }
    
    [Theory]
    [InlineData("CompilerTest/test_basic_expressions.old8")]
    [InlineData("CompilerTest/test_complex_data_types.old8")]
    [InlineData("CompilerTest/test_functions_and_any.old8")]
    public void TestCompiler_AllFiles(string testFile)
    {
        // 测试所有编译测试文件
        var fullPath = Path.Combine(GetProjectRoot(), testFile);
        Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
        
        // 创建解释器实例
        var interpreter = new LangInterpreter();
        
        // 编译代码
        var action = Old8Lang.Compiler.Compiler.Compile(fullPath, interpreter);
        
        // 验证编译结果不为空
        Assert.NotNull(action);
    }
}
