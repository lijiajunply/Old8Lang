using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// 编译器模式下的异步编程功能测试 - Task API
/// </summary>
public class TaskAPITests
{
    private readonly ITestOutputHelper _output;

    public TaskAPITests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TaskDelay_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func testDelay() {
                startTime <- Time.Now()
                await Task.Delay(100)
                endTime <- Time.Now()
                duration <- endTime - startTime
                
                Assert.True(duration >= 90)
            }
            
            testDelay()
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
    public void TaskRun_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func heavyComputation(value:int) -> int {
                await Task.Delay(50)
                return value * value
            }
            
            async func testRun() {
                result <- await Task.Run(heavyComputation, 10)
                Assert.Equal(100, result)
            }
            
            testRun()
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
    public void TaskWhenAll_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func task1() -> int {
                await Task.Delay(50)
                return 10
            }
            
            async func task2() -> int {
                await Task.Delay(60)
                return 20
            }
            
            async func task3() -> int {
                await Task.Delay(40)
                return 30
            }
            
            async func testWhenAll() {
                tasks <- {task1(), task2(), task3()}
                results <- await Task.WhenAll(tasks)
                
                Assert.Equal(3, results.Count())
                Assert.Equal(10, results[0])
                Assert.Equal(20, results[1])
                Assert.Equal(30, results[2])
            }
            
            testWhenAll()
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
    public void TaskWhenAny_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func fastTask() -> string {
                await Task.Delay(10)
                return ""fast""
            }
            
            async func slowTask() -> string {
                await Task.Delay(100)
                return ""slow""
            }
            
            async func testWhenAny() {
                tasks <- {fastTask(), slowTask()}
                result <- await Task.WhenAny(tasks)
                
                Assert.Equal(""fast"", result)
            }
            
            testWhenAny()
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
    public void TaskFromResult_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func testFromResult() {
                task <- Task.FromResult(42)
                result <- await task
                Assert.Equal(42, result)
            }
            
            testFromResult()
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
    public void TaskContinueWith_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func compute(value:int) -> int {
                await Task.Delay(30)
                return value * 2
            }
            
            async func testContinueWith() {
                task1 <- compute(5)
                task2 <- task1.ContinueWith(func(t) -> int {
                    return t.Result + 10
                })
                
                result1 <- await task1
                result2 <- await task2
                
                Assert.Equal(10, result1)
                Assert.Equal(20, result2)
            }
            
            testContinueWith()
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
    public void TaskWait_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func longRunning() -> string {
                await Task.Delay(100)
                return ""completed""
            }
            
            async func testWait() {
                task <- longRunning()
                task.Wait()
                result <- task.Result
                
                Assert.Equal(""completed"", result)
            }
            
            testWait()
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
    public void TaskWaitAll_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func task1() -> void {
                await Task.Delay(50)
            }
            
            async func task2() -> void {
                await Task.Delay(60)
            }
            
            async func task3() -> void {
                await Task.Delay(40)
            }
            
            async func testWaitAll() {
                tasks <- {task1(), task2(), task3()}
                Task.WaitAll(tasks)
                
                Assert.True(tasks[0].IsCompleted)
                Assert.True(tasks[1].IsCompleted)
                Assert.True(tasks[2].IsCompleted)
            }
            
            testWaitAll()
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
    public void TaskWaitAny_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func fastTask() -> void {
                await Task.Delay(20)
            }
            
            async func slowTask() -> void {
                await Task.Delay(100)
            }
            
            async func testWaitAny() {
                tasks <- {fastTask(), slowTask()}
                index <- Task.WaitAny(tasks)
                
                Assert.Equal(0, index)
            }
            
            testWaitAny()
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
    public void TaskCompleted_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func testCompleted() {
                task <- Task.Completed()
                Assert.True(task.IsCompleted)
            }
            
            testCompleted()
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
    public void TaskCancellation_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func longRunningTask(cts) -> string {
                i <- 0
                while i < 100 {
                    if cts.IsCancellationRequested {
                        throw ""Task cancelled""
                    }
                    await Task.Delay(10)
                    i <- i + 1
                }
                return ""completed""
            }
            
            async func testCancellation() {
                cts <- CreateCancellationTokenSource()
                task <- longRunningTask(cts.Token)
                
                await Task.Delay(50)
                cts.Cancel()
                
                error <- """"
                try {
                    result <- await task
                } catch (e) {
                    error <- e
                }
                
                Assert.Equal(""Task cancelled"", error)
            }
            
            testCancellation()
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
    public void TaskExceptionHandling_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func failingTask() -> int {
                await Task.Delay(20)
                throw ""Task failed""
            }
            
            async func testExceptionHandling() {
                caughtError <- """"
                
                try {
                    task <- failingTask()
                    result <- await task
                } catch (e) {
                    caughtError <- e
                }
                
                Assert.Equal(""Task failed"", caughtError)
            }
            
            testExceptionHandling()
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
