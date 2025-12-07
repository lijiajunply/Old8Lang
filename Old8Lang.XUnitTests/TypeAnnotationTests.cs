using Old8Lang.LangParser;
using Old8Lang.Error;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Expression;

namespace Old8Lang.XUnitTests;

public class TypeAnnotationTests
{
    private string GetProjectRoot()
    {
        // 获取当前目录的父目录，即项目根目录
        return Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()!)!.Parent!.FullName)!.FullName)!.FullName;
    }
    
    [Fact]
    public void TestTypeAnnotationParser()
    {
        // 测试带有类型注解的变量声明解析
        var code = "a:int <- 123";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }
    
    [Fact]
    public void TestFunctionTypeAnnotationParser()
    {
        // 测试带有类型注解的函数定义解析
        var code = "add:int (a:int, b:int) -> { return a + b }";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens);
        var result = parser.ParseProgram();
        
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }
    
    [Fact]
    public void TestRuntimeTypeCheck()
    {
        // 测试运行时类型检查 - 非法赋值
        var code = "a:int <- 123\na <- \"string\"";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens);
        var program = parser.ParseProgram();
        var manager = new VariateManager();
        
        // 执行第一次赋值（合法）
        var firstStatement = (SetStatement)program[0];
        firstStatement.Run(manager);
        
        // 执行第二次赋值（非法，应该抛出TypeError）
        var secondStatement = (SetStatement)program[1];
        Assert.Throws<TypeError>(() => secondStatement.Run(manager));
    }
    
    [Fact]
    public void TestValidTypeAssignment()
    {
        // 测试合法的类型赋值
        var code = "a:int <- 123\nb:string <- \"hello\"\nc:bool <- true";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens);
        var program = parser.ParseProgram();
        var manager = new VariateManager();
        
        // 所有赋值都应该成功执行，不抛出异常
        for (int i = 0; i < program.Count; i++)
        {
            var statement = (SetStatement)program[i];
            statement.Run(manager);
        }
        
        // 验证变量已正确设置
        var a = manager.GetValue(new OldId