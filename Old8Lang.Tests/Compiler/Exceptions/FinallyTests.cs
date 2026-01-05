using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Exceptions;

/// <summary>
/// 编译器模式下的异常处理测试 - Finally 块
/// </summary>
public class FinallyTests
{
    private readonly ITestOutputHelper _output;

    public FinallyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void FinallyWithoutException_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- """"
            try {
                result <- result + ""try;""
            } finally {
                result <- result + ""finally;""
            }
            Assert.Equal(""try;finally;"", result)
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
    public void FinallyWithException_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- """"
            try {
                result <- result + ""try;""
                throw ""test error""
            } catch (e) {
                result <- result + ""catch;""
            } finally {
                result <- result + ""finally;""
            }
            Assert.Equal(""try;catch;finally;"", result)
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
    public void FinallyWithUncaughtException_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- """"
            try {
                result <- result + ""try;""
                throw ""uncaught""
            } finally {
                result <- result + ""finally;""
            }
            // 这行不应该执行，因为异常未被捕获
            result <- result + ""after;""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - finally 应该执行，但之后会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.NotNull(exception);
        // 验证 finally 执行了
        Assert.Contains("try;finally;", exception.Message);
    }

    [Fact]
    public void FinallyWithReturnInTry_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func testFunction() -> string {
                try {
                    return ""from try""
                } finally {
                    // finally 应该在 return 之后执行
                    result <- ""finally executed""
                }
                return ""unreachable""
            }
            
            result <- testFunction()
            Assert.Equal(""from try"", result)
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
    public void FinallyWithReturnInCatch_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func testFunction() -> string {
                try {
                    throw ""error""
                } catch (e) {
                    return ""from catch: "" + e
                } finally {
                    // finally 应该在 catch 中的 return 之后执行
                    // 注意：这个测试依赖于 finally 如何与 return 交互
                }
            }
            
            result <- testFunction()
            Assert.Equal(""from catch: error"", result)
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
    public void NestedFinallyBlocks_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- """"
            try {
                result <- result + ""outer try;""
                try {
                    result <- result + ""inner try;""
                } finally {
                    result <- result + ""inner finally;""
                }
            } finally {
                result <- result + ""outer finally;""
            }
            Assert.Equal(""outer try;inner try;inner finally;outer finally;"", result)
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
    public void FinallyWithExceptionInFinally_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- """"
            try {
                result <- result + ""try;""
                throw ""original error""
            } catch (e) {
                result <- result + ""catch;""
            } finally {
                result <- result + ""finally start;""
                // 在 finally 中抛出新异常会覆盖原始异常
                throw ""finally error""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - finally 中的异常应该抛出
        var exception = Record.Exception(() => compiledAction());
        Assert.NotNull(exception);
        Assert.Contains("finally error", exception.Message);
    }

    [Fact]
    public void FinallyWithResourceCleanup_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 模拟资源管理
            resource_opened <- false
            resource_closed <- false
            
            func openResource() -> void {
                resource_opened <- true
            }
            
            func closeResource() -> void {
                if resource_opened {
                    resource_closed <- true
                }
            }
            
            func processResource() -> string {
                try {
                    openResource()
                    // 模拟处理过程中可能出现的错误
                    if true {  // 模拟条件
                        throw ""processing failed""
                    }
                    return ""success""
                } catch (e) {
                    return ""error: "" + e
                } finally {
                    closeResource()  // 确保资源被释放
                }
            }
            
            result <- processResource()
            Assert.True(resource_opened)
            Assert.True(resource_closed)
            Assert.Equal(""error: processing failed"", result)
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
    public void FinallyWithLoop_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- """"
            counter <- 0
            
            try {
                while counter < 3 {
                    if counter == 2 {
                        throw ""loop error""
                    }
                    result <- result + ""iteration "" + counter.ToStr() + "";""
                    counter <- counter + 1
                }
            } catch (e) {
                result <- result + ""caught: "" + e
            } finally {
                result <- result + ""finally: counter="" + counter.ToStr()
            }
            
            Assert.Equal(""iteration 0;iteration 1;caught: loop errorfinally: counter=2"", result)
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
    public void FinallyWithVariableModification_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            value <- 0
            
            try {
                value <- 10
                // 模拟可能出错的操作
                temp <- 1 / 0  // 这会抛出异常
            } catch (e) {
                value <- 20
            } finally {
                value <- value + 5  // finally 中的修改总是会执行
            }
            
            Assert.Equal(25, value)
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
    public void FinallyWithFunctionCall_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cleanup_called <- false
            
            func cleanup() -> void {
                cleanup_called <- true
            }
            
            func riskyOperation() -> string {
                try {
                    return ""operation result""
                } finally {
                    cleanup()
                }
            }
            
            result <- riskyOperation()
            Assert.Equal(""operation result"", result)
            Assert.True(cleanup_called)
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