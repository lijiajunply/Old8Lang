using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Error.Compilation;

/// <summary>
/// 编译器错误处理测试
/// </summary>
[Collection("Sequential")]
public class ErrorCompilationTests
{
    [Fact]
    public void MissingTypeAnnotation_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func processData(value:int) -> int {
                return value * 2
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        
        // Assert - 编译器应因缺少返回类型注解而报错
        var exception = Assert.Throws<InvalidOperationError>(() => Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
        Assert.Contains("函数必须包含类型注解", exception.Message);
    }

    [Fact]
    public void InvalidTypeAnnotation_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func processData(value:int) -> string {
                return value.ToStr()
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        
        // Assert - 编译器应因无效类型注解而报错
        var exception = Assert.Throws<InvalidOperationError>(() => Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
        Assert.Contains("无效的类型注解", exception.Message);
    }

    [Fact]
    public void IncompatibleType_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func processData(a:int, b:string) -> int {
                return a + b.Length
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        
        // Assert - 编译器应因不兼容类型而报错
        var exception = Assert.Throws<InvalidOperationError>(() => Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
        Assert.Contains("类型不兼容", exception.Message);
    }

    [Fact]
    public void UnresolvedVariable_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            result <- undefined_var + 1
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        
        // Assert - 编译器应因未解析的变量而报错
        var exception = Assert.Throws<InvalidOperationError>(() => Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
        Assert.Contains("未定义的变量", exception.Message);
    }

    [Fact]
    public void UndefinedMethod_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            result <- SomeMethod()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        
        // Assert - 编译器应因未解析的方法而报错
        var exception = Assert.Throws<InvalidOperationError>(() => Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
        Assert.Contains("未定义的方法", exception.Message);
    }

    [Fact]
    public void CircularReference_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func A() -> int {
                return B()
            }
            
            func B() -> int {
                return A()
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        
        // Assert - 编译器应因循环引用而报错
        var exception = Assert.Throws<InvalidOperationError>(() => Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
        Assert.Contains("循环引用", exception.Message);
    }

    [Fact]
    public void DivisionByZero_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 10 / 0
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        
        // Assert - 编译器应因除零而报错
        var exception = Assert.Throws<Old8Exception>(() => Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
        Assert.Contains("除零错误", exception.Message);
    }

    [Fact]
    public void IndexOutOfBounds_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3]
            element <- arr[5]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        
        // Assert - 编译器应因索引越界而报错
        var exception = Assert.Throws<IndexOutOfRangeException>(() => Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
    }

    [Fact]
    public void InvalidSyntax_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            // This should cause a syntax error
            if a > 0 {
                print(""a is greater than 0"")
            } else {
                print(""a is not greater than 0"")
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        
        // Assert - 编译器应因语法错误而报错
        var exception = Assert.Throws<SyntaxError>(() => Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
        Assert.Contains("语法错误", exception.Message);
    }

    [Fact]
    public void MissingReturnStatement_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func ShouldReturn() -> int {
                // Missing return statement
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        
        // Assert - 编译器应因缺少return语句而报错
        var exception = Assert.Throws<InvalidOperationError>(() => Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
        Assert.Contains("return", exception.Message);
    }

    [Fact]
    public void DuplicateIdentifier_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            // Duplicate identifier
            var a <- 1
            var a <- 2
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        
        // Assert - 编译器应因重复标识符而报错
        var exception = Assert.Throws<SyntaxError>(() => Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
        Assert.Contains("重复标识符", exception.Message);
    }

    [Fact]
    public void InvalidKeyword_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            if True { print(""test"") }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        
        // Assert - 编译器应因无效关键字而报错
        var exception = Assert.Throws<SyntaxError>(() => Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
        Assert.Contains("无效关键字", exception.Message);
    }
}