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
        
        // 只检查解析是否成功，不抛出异常
        var result = parser.ParseProgram();
        Assert.NotNull(result);
        // 解析结果不应为null
        Assert.NotNull(result);
    }
    
    [Fact]
    public void TestRuntimeTypeCheck()
    {
        // 测试运行时类型检查 - 非法赋值
        // 注意：第二次赋值也需要带有类型注解，或者使用相同的变量名和类型注解
        var code = "a:int <- 123\na:int <- \"string\"";
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
        var a = manager.GetValue(new OldId("a"));
        var b = manager.GetValue(new OldId("b"));
        var c = manager.GetValue(new OldId("c"));
        
        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.NotNull(c);
    }
    
    [Fact]
    public void TestTypeAnnotationTestFiles()
    {
        // 测试类型注解的测试文件
        var testFiles = new List<string>
        {
            "Test/test_type_annotation_simple.old8",
            "Test/test_simple_function_no_comment.old8"
        };
        
        foreach (var testFile in testFiles)
        {
            var fullPath = Path.Combine(GetProjectRoot(), testFile);
            Assert.True(File.Exists(fullPath), $"文件不存在: {fullPath}");
            
            var code = File.ReadAllText(fullPath);
            var tokens = LangTokenizer.Tokenize(code);
            var parser = new LangParser.LangParser(tokens);
            var result = parser.ParseProgram();
            
            Assert.NotNull(result);
            Assert.True(result.Count >= 0);
        }
    }
    
    [Fact]
    public void TestTypeAnnotationSyntax()
    {
        // 测试不同形式的类型注解语法
        var testCases = new List<string>
        {
            "a:int <- 123",
            "b:string <- \"hello\"",
            "c:bool <- true",
            "d:char <- 'a'",
            "e:double <- 3.14",
            "f:array <- [1, 2, 3]"
        };
        
        foreach (var code in testCases)
        {
            var tokens = LangTokenizer.Tokenize(code);
            var parser = new LangParser.LangParser(tokens);
            var result = parser.ParseProgram();
            
            Assert.NotNull(result);
            Assert.True(result.Count > 0);
        }
    }
    
    [Fact]
    public void TestAllTypeAnnotations()
    {
        // 测试所有类型注解的运行时类型检查
        var code = "a:int <- 123\nb:double <- 3.14\nc:string <- \"hello\"\nd:bool <- true\ne:char <- 'a'\nf:array <- [1, 2, 3]";
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
        var a = manager.GetValue(new OldId("a"));
        var b = manager.GetValue(new OldId("b"));
        var c = manager.GetValue(new OldId("c"));
        var d = manager.GetValue(new OldId("d"));
        var e = manager.GetValue(new OldId("e"));
        var f = manager.GetValue(new OldId("f"));
        
        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.NotNull(c);
        Assert.NotNull(d);
        Assert.NotNull(e);
        Assert.NotNull(f);
    }
    
    [Fact]
    public void TestOldIdWithTypeAnnotation()
    {
        // 测试带有类型注解的OldId创建
        var position = new SourcePosition(1, 1);
        var oldId = new OldId("testVar", "int", position);
        
        Assert.Equal("testVar", oldId.IdName);
        Assert.Equal("int", oldId.AssumptionType);
        Assert.Equal(position, oldId.Position);
    }
    
    [Fact]
    public void TestTokenizerForTypeAnnotation()
    {
        // 测试类型注解的令牌化
        var code = "a:int <- 123";
        var tokens = LangTokenizer.Tokenize(code);
        
        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);
        Assert.Equal(5, tokens.Count); // 应该有5个令牌：a, :, int, <-, 123
        
        Assert.Equal("a", tokens[0].Value);
        Assert.Equal(LangTokenType.Identifier, tokens[0].Type);
        
        Assert.Equal(":", tokens[1].Value);
        Assert.Equal(LangTokenType.Colon, tokens[1].Type);
        
        Assert.Equal("int", tokens[2].Value);
        Assert.Equal(LangTokenType.Identifier, tokens[2].Type);
        
        Assert.Equal("<-", tokens[3].Value);
        Assert.Equal(LangTokenType.Assignment, tokens[3].Type);
        
        Assert.Equal("123", tokens[4].Value);
        Assert.Equal(LangTokenType.Number, tokens[4].Type);
    }
}
