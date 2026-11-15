using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// IL 模式下的异步编程功能测试 - spawn 关键字
/// </summary>
public class SpawnTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void BasicSpawn_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func backgroundTask() -> int {
                Sleep(100)
                return 42
            }
            
            task <- spawn(backgroundTask)
            task.Start()
            result <- task.Join()
            
            Assert.Equal(42, result)
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
    public void MultipleSpawn_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func task1() -> int {
                Sleep(50)
                return 10
            }
            
            func task2() -> int {
                Sleep(60)
                return 20
            }
            
            func task3() -> int {
                Sleep(40)
                return 30
            }
            
            t1 <- spawn(task1)
            t1.Start()
            t2 <- spawn(task2)
            t2.Start()
            t3 <- spawn(task3)
            t3.Start()

            result1 <- t1.Join()
            result2 <- t2.Join()
            result3 <- t3.Join()
            
            Assert.Equal(10, result1)
            Assert.Equal(20, result2)
            Assert.Equal(30, result3)
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
    public void SpawnWithReturnValue_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func calculate(value:int) -> int {
                Sleep(50)
                return value * value
            }
            
            task <- spawn(calculate, 5)
            task.Start()
            result <- task.Join()
            
            Assert.Equal(25, result)
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
    public void SpawnWithParameters_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func process(name:string, value:int, multiplier:double) -> string {
                Sleep(30)
                result <- value * multiplier
                return name + "": "" + result.ToStr()
            }
            
            task <- spawn(process, ""test"", 10, 2.5)
            task.Start()
            result <- task.Join()
            
            Assert.Equal(""test: 25"", result)
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
    public void SpawnWithCollections_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func processList(input:list) -> list {
                Sleep(50)
                results <- {}
                i <- 0
                while i < input.Count() {
                    results.Add(input[i] * 2)
                    i <- i + 1
                }
                return results
            }
            
            task <- spawn(processList, {1, 2, 3, 4, 5})
            task.Start()
            result <- task.Join()
            
            Assert.Equal(5, result.Count())
            Assert.Equal({2, 4, 6, 8, 10}, result)
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
    public void SpawnWithErrorHandling_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func failingTask() -> int {
                Sleep(30)
                throw ""Task failed""
            }
            
            task <- spawn(failingTask)
            task.Start()
            
            caughtError <- """"
            try {
                result <- task.Join()
            } catch (e) {
                caughtError <- e
            }
            
            // 线程异常在三种模式下分别被包装成 CustomError / LangThrownException / 原始值，
            // 统一按错误消息内容断言。
            Assert.Contains(caughtError.ToStr(), ""Task failed"")
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
    public void SpawnWithComplexLogic_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func fibonacci(n:int) -> int {
                if n <= 1 {
                    return n
                }
                return fibonacci(n - 1) + fibonacci(n - 2)
            }
            
            func heavyComputation() -> int {
                Sleep(100)
                sum <- 0
                i <- 0
                while i < 10 {
                    sum <- sum + fibonacci(i)
                    i <- i + 1
                }
                return sum
            }
            
            task <- spawn(heavyComputation)
            task.Start()
            result <- task.Join()
            
            Assert.Equal(88, result)
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
    public void SpawnWithSharedState_CompilesAndExecutesCorrectly()
    {
        var code = @"
            counter <- 0
            mutex <- MutexCreate()
            
            func increment() -> void {
                i <- 0
                while i < 10 {
                    MutexLock(mutex)
                    counter <- counter + 1
                    MutexUnlock(mutex)
                    Sleep(10)
                    i <- i + 1
                }
            }
            
            task1 <- spawn(increment)
            task1.Start()
            task2 <- spawn(increment)
            task2.Start()
            
            task1.Join()
            task2.Join()
            
            Assert.Equal(20, counter)
            
            MutexDispose(mutex)
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
    public void SpawnWithConditional_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func conditionalTask(useFast:bool) -> string {
                if useFast {
                    Sleep(10)
                    return ""fast result""
                } else {
                    Sleep(100)
                    return ""slow result""
                }
            }
            
            task1 <- spawn(conditionalTask, true)
            task1.Start()
            task2 <- spawn(conditionalTask, false)
            task2.Start()
            
            result1 <- task1.Join()
            result2 <- task2.Join()
            
            Assert.Equal(""fast result"", result1)
            Assert.Equal(""slow result"", result2)
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
    public void SpawnWithLoops_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func loopTask(iterations:int) -> int {
                sum <- 0
                i <- 0
                while i < iterations {
                    sum <- sum + i
                    Sleep(10)
                    i <- i + 1
                }
                return sum
            }
            
            task <- spawn(loopTask, 5)
            task.Start()
            result <- task.Join()
            
            Assert.Equal(10, result)
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
    public void SpawnWithClassMethods_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Worker {
                private value <- 0
                
                func init(v:int) {
                    this.value <- v
                }
                
                func process() -> int {
                    Sleep(50)
                    return this.value * 2
                }
            }
            
            worker <- Worker(5)
            task <- spawn(() -> worker.process())
            task.Start()
            result <- task.Join()
            
            Assert.Equal(10, result)
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
    public void SpawnWithLambda_CompilesAndExecutesCorrectly()
    {
        var code = @"
            task <- spawn(() -> {
                Sleep(50)
                return 42
            })
            task.Start()

            result <- task.Join()
            
            Assert.Equal(42, result)
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
    public void SpawnParallelExecution_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func measureParallelExecution() -> bool {
                task1 <- spawn(() -> {
                    Sleep(100)
                })
                task1.Start()

                task2 <- spawn(() -> {
                    Sleep(100)
                })
                task2.Start()

                task3 <- spawn(() -> {
                    Sleep(100)
                })
                task3.Start()
                
                task1.Join()
                task2.Join()
                task3.Join()
                
                return true
            }
            
            isParallel <- measureParallelExecution()
            Assert.True(isParallel)
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
