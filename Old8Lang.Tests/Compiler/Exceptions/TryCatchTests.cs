using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Exceptions;

/// <summary>
/// 编译器模式下的异常处理测试 - Try-Catch 语句
/// </summary>
public class TryCatchTests
{
    private readonly ITestOutputHelper _output;

    public TryCatchTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BasicTryCatch_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- ""not caught""
            try {
                // 正常执行，不抛出异常
                result <- ""success""
            } catch (e) {
                result <- ""caught: "" + e
            }
            Assert.Equal(""success"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TryCatchWithStringException_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- ""not caught""
            try {
                throw ""test exception""
                result <- ""should not reach""
            } catch (e) {
                result <- ""caught: "" + e
            }
            Assert.Equal(""caught: test exception"", result)
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
    public void TryCatchWithNumberException_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 0
            try {
                throw 42
                result <- -1
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
    public void TryCatchWithArrayException_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- {}
            try {
                throw [1, 2, 3]
                result <- {0}
            } catch (e) {
                result <- e
            }
            Assert.Equal([1, 2, 3], result)
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
    public void NestedTryCatch_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- """"
            try {
                try {
                    throw ""inner exception""
                } catch (inner) {
                    result <- ""inner caught: "" + inner
                    throw ""outer exception""
                }
            } catch (outer) {
                result <- result + ""; outer caught: "" + outer
            }
            Assert.Contains(""inner caught: inner exception"", result)
            Assert.Contains(""outer caught: outer exception"", result)
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
    public void TryCatchWithDivisionByZero_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 0
            try {
                result <- 10 / 0  // 这会在运行时抛出异常
            } catch (e) {
                result <- -1  // 异常被捕获
            }
            Assert.Equal(-1, result)
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
    public void TryCatchWithVariableAccess_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            caught_value <- """"
            try {
                // 尝试访问未定义的变量或执行会抛出异常的操作
                x <- null
                result <- x + 1  // null 操作会抛出异常
            } catch (e) {
                caught_value <- ""exception caught: "" + e.ToStr()
            }
            Assert.True(caught_value.Length() > 0)
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
    public void TryCatchWithoutExceptionParameter_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- ""not caught""
            try {
                throw ""test""
            } catch {
                result <- ""caught without parameter""
            }
            Assert.Equal(""caught without parameter"", result)
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
    public void MultipleCatchBlocks_CompilesAndExecutesCorrectly()
    {
        // Arrange - 注意：Old8Lang 当前不支持类型化异常捕获，这里测试多个 catch 块
        var code = @"
            result <- ""not caught""
            try {
                throw ""string exception""
            } catch (e) {
                result <- ""first catch: "" + e
            } catch (e2) {
                result <- ""second catch: "" + e2
            }
            Assert.Equal(""first catch: string exception"", result)
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
    public void TryCatchInFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func risky_operation(input:int) -> string {
                try {
                    if input == 0 {
                        throw ""cannot be zero""
                    }
                    return ""success: "" + (10 / input).ToStr()
                } catch (e) {
                    return ""error: "" + e
                }
            }
            
            result1 <- risky_operation(2)
            result2 <- risky_operation(0)
            
            Assert.Equal(""success: 5"", result1)
            Assert.Equal(""error: cannot be zero"", result2)
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