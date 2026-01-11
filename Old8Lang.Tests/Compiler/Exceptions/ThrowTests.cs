using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Exceptions;

/// <summary>
/// 编译器模式下的异常处理测试 - Throw 语句
/// </summary>
public class ThrowTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void ThrowString_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- """"
            try {
                throw ""test exception message""
            } catch (e) {
                result <- e
            }
            Assert.Equal(""test exception message"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ThrowInteger_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 0
            try {
                throw 42
            } catch (e) {
                result <- e
            }
            Assert.Equal(42, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ThrowDouble_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 0.0
            try {
                throw 3.14
            } catch (e) {
                result <- e
            }
            Assert.Equal(3.14, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ThrowBoolean_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- false
            try {
                throw true
            } catch (e) {
                result <- e
            }
            Assert.Equal(true, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ThrowNull_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- ""not null""
            try {
                throw null
            } catch (e) {
                result <- ""caught null""
            }
            Assert.Equal(""caught null"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ThrowArray_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- {}
            try {
                throw [1, 2, 3, ""error""]
            } catch (e) {
                result <- e
            }
            Assert.Equal([1, 2, 3, ""error""], result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ThrowList_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- {}
            try {
                throw {1, 2, 3, ""list error""}
            } catch (e) {
                result <- e
            }
            Assert.Equal({1, 2, 3, ""list error""}, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ThrowDictionary_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- {}
            try {
                throw {""error"": ""division by zero"", ""code"": 500}
            } catch (e) {
                result <- e
            }
            Assert.Equal({""error"": ""division by zero"", ""code"": 500}, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ThrowExpression_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            y <- 0
            result <- """"
            try {
                throw ""Error: "" + (x / y).ToStr() + "" is invalid""
            } catch (e) {
                result <- e
            }
            Assert.Contains(""Error:"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ThrowInFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func validateAge(age:int) -> void {
                if age < 0 {
                    throw ""Age cannot be negative: "" + age.ToStr()
                }
                if age > 150 {
                    throw ""Age seems unrealistic: "" + age.ToStr()
                }
            }
            
            result1 <- """"
            result2 <- """"
            result3 <- """"
            
            try {
                validateAge(25)
                result1 <- ""valid""
            } catch (e) {
                result1 <- e
            }
            
            try {
                validateAge(-5)
                result2 <- ""valid""
            } catch (e) {
                result2 <- e
            }
            
            try {
                validateAge(200)
                result3 <- ""valid""
            } catch (e) {
                result3 <- e
            }
            
            Assert.Equal(""valid"", result1)
            Assert.Equal(""Age cannot be negative: -5"", result2)
            Assert.Equal(""Age seems unrealistic: 200"", result3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ThrowInLoop_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- """"
            try {
                i <- 0
                while i < 10 {
                    if i == 5 {
                        throw ""Loop stopped at "" + i.ToStr()
                    }
                    i <- i + 1
                }
            } catch (e) {
                result <- e
            }
            Assert.Equal(""Loop stopped at 5"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ThrowWithoutCatch_CompilesAndExecutesCorrectly()
    {
        // Arrange - 测试未被捕获的异常会传播
        var code = @"
            func throwException() -> void {
                throw ""uncaught exception""
            }
            
            caught <- false
            try {
                try {
                    throwException()
                } finally {
                    caught <- true  // finally 块应该执行
                }
            } catch (e) {
                // 外层捕获异常
                Assert.Equal(""uncaught exception"", e)
            }
            Assert.True(caught)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ThrowComplexObject_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 创建一个复杂对象作为异常
            error_obj <- {
                ""type"": ""ValidationError"",
                ""message"": ""Invalid input"",
                ""code"": 400,
                ""details"": {""field"": ""email"", ""issue"": ""invalid format""}
            }
            
            result <- {}
            try {
                throw error_obj
            } catch (e) {
                result <- e
            }
            
            Assert.Equal(""ValidationError"", result[""type""])
            Assert.Equal(""Invalid input"", result[""message""])
            Assert.Equal(400, result[""code""])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}