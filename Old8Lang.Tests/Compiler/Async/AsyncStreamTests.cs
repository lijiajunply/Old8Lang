using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// 编译器模式下的异步编程功能测试 - 异步流
/// </summary>
public class AsyncStreamTests
{
    private readonly ITestOutputHelper _output;

    public AsyncStreamTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BasicAsyncStream_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func generateNumbers(count:int) -> stream {
                i <- 0
                while i < count {
                    yield i
                    await Task.Delay(10)
                    i <- i + 1
                }
            }
            
            async func consumeStream() {
                stream <- generateNumbers(5)
                results <- {}
                
                for num in stream {
                    results.Add(num)
                }
                
                Assert.Equal(5, results.Count())
                Assert.Equal({0, 1, 2, 3, 4}, results)
            }
            
            consumeStream()
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AsyncStreamWithFilter_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func generateNumbers() -> stream {
                i <- 0
                while i < 10 {
                    yield i
                    await Task.Delay(5)
                    i <- i + 1
                }
            }
            
            async func filterEvenNumbers() {
                stream <- generateNumbers()
                results <- {}
                
                for num in stream {
                    if num % 2 == 0 {
                        results.Add(num)
                    }
                }
                
                Assert.Equal(5, results.Count())
                Assert.Equal({0, 2, 4, 6, 8}, results)
            }
            
            filterEvenNumbers()
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AsyncStreamWithTransform_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func generateNumbers(count:int) -> stream {
                i <- 0
                while i < count {
                    yield i
                    await Task.Delay(5)
                    i <- i + 1
                }
            }
            
            async func transformStream() {
                stream <- generateNumbers(5)
                results <- {}
                
                for num in stream {
                    transformed <- num * num
                    results.Add(transformed)
                }
                
                Assert.Equal(5, results.Count())
                Assert.Equal({0, 1, 4, 9, 16}, results)
            }
            
            transformStream()
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AsyncStreamWithAggregation_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func generateNumbers(count:int) -> stream {
                i <- 0
                while i < count {
                    yield i
                    await Task.Delay(5)
                    i <- i + 1
                }
            }
            
            async func aggregateStream() {
                stream <- generateNumbers(10)
                sum <- 0
                
                for num in stream {
                    sum <- sum + num
                }
                
                Assert.Equal(45, sum)
            }
            
            aggregateStream()
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AsyncStreamWithEarlyTermination_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func generateNumbers() -> stream {
                i <- 0
                while i < 100 {
                    yield i
                    await Task.Delay(5)
                    i <- i + 1
                }
            }
            
            async func earlyTermination() {
                stream <- generateNumbers()
                results <- {}
                count <- 0
                
                for num in stream {
                    results.Add(num)
                    count <- count + 1
                    if count >= 5 {
                        break
                    }
                }
                
                Assert.Equal(5, results.Count())
                Assert.Equal({0, 1, 2, 3, 4}, results)
            }
            
            earlyTermination()
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AsyncStreamWithErrorHandling_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func generateNumbersWithError(count:int) -> stream {
                i <- 0
                while i < count {
                    if i == 3 {
                        throw ""Error at index 3""
                    }
                    yield i
                    await Task.Delay(5)
                    i <- i + 1
                }
            }
            
            async func handleStreamError() {
                caughtError <- """"
                results <- {}
                
                try {
                    stream <- generateNumbersWithError(10)
                    for num in stream {
                        results.Add(num)
                    }
                } catch (e) {
                    caughtError <- e
                }
                
                Assert.Equal(""Error at index 3"", caughtError)
                Assert.Equal({0, 1, 2}, results)
            }
            
            handleStreamError()
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MultipleAsyncStreams_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func generateStream1(count:int) -> stream {
                i <- 0
                while i < count {
                    yield i
                    await Task.Delay(5)
                    i <- i + 1
                }
            }
            
            async func generateStream2(count:int) -> stream {
                i <- 0
                while i < count {
                    yield i * 10
                    await Task.Delay(5)
                    i <- i + 1
                }
            }
            
            async func combineStreams() {
                stream1 <- generateStream1(3)
                stream2 <- generateStream2(3)
                
                results1 <- {}
                for num in stream1 {
                    results1.Add(num)
                }
                
                results2 <- {}
                for num in stream2 {
                    results2.Add(num)
                }
                
                Assert.Equal({0, 1, 2}, results1)
                Assert.Equal({0, 10, 20}, results2)
            }
            
            combineStreams()
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AsyncStreamWithFinally_CompilesAndExecutesCorrectly()
    {
        var code = @"
            cleanupExecuted <- false
            
            async func generateWithCleanup(count:int) -> stream {
                try {
                    i <- 0
                    while i < count {
                        yield i
                        await Task.Delay(5)
                        i <- i + 1
                    }
                } finally {
                    cleanupExecuted <- true
                }
            }
            
            async func testFinallyInStream() {
                stream <- generateWithCleanup(5)
                results <- {}
                
                for num in stream {
                    results.Add(num)
                }
                
                Assert.Equal(5, results.Count())
                Assert.True(cleanupExecuted)
            }
            
            testFinallyInStream()
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
