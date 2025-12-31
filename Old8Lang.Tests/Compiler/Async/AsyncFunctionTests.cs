using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// 异步函数基础测试
/// </summary>
[Collection("Sequential")]
public class AsyncFunctionTests
{
    [Fact]
    public void BasicAsyncFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func fetchData() -> string {
                return ""Data from server""
            }
            
            result <- await fetchData()
            Assert.Equal(""Data from server"", result)
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
    public void AsyncFunctionWithParameters_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func delayedAdd(a:int, b:int, delay_ms:int) -> int {
                await Task.Delay(delay_ms)
                return a + b
            }
            
            result <- await delayedAdd(10, 20, 100)
            Assert.Equal(30, result)
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
    public void AsyncFunctionWithReturnTypes_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func processBoolean(flag:bool) -> string {
                await Task.Delay(50)
                if flag {
                    return ""Success""
                } else {
                    return ""Failure""
                }
            }
            
            async func processNumber(value:int) -> int {
                await Task.Delay(50)
                return value * 2
            }
            
            async func processDouble(value:double) -> double {
                await Task.Delay(50)
                return value / 2
            }
            
            result1 <- await processBoolean(true)
            result2 <- await processNumber(25)
            result3 <- await processDouble(10.0)
            
            Assert.Equal(""Success"", result1)
            Assert.Equal(50, result2)
            Assert.Equal(5.0, result3)
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
    public void AsyncFunctionInClass_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class AsyncCalculator {
                private value <- 0
                
                func init() {
                    this.value <- 0
                }
                
                async func addAsync(operand:int) -> int {
                    await Task.Delay(100)
                    this.value <- this.value + operand
                    return this.value
                }
                
                async func getValue() -> int {
                    return this.value
                }
            }
            
            calculator <- AsyncCalculator()
            result1 <- await calculator.addAsync(10)
            result2 <- await calculator.getValue()
            
            Assert.Equal(10, result1)
            Assert.Equal(10, result2)
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
    public void ChainedAsyncCalls_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func fetchUserId(id:int) -> string {
                await Task.Delay(50)
                return ""user_"" + id.ToStr()
            }
            
            async func fetchUserData(userId:string) -> string {
                await Task.Delay(50)
                return ""data_for_"" + userId
            }
            
            // Chain the async calls
            userId <- await fetchUserId(123)
            userData <- await fetchUserData(userId)
            
            Assert.Equal(""user_123"", userId)
            Assert.Equal(""data_for_user_123"", userData)
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
    public void AsyncErrorHandling_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func mightFail(should_fail:bool) -> string {
                await Task.Delay(50)
                if should_fail {
                    throw ""Something went wrong""
                } else {
                    return ""Success""
                }
            }
            
            error_caught <- false
            result_value <- """"
            
            try {
                result_value <- await mightFail(false)
            } catch (e) {
                result_value <- e
            }
            
            try {
                result_value <- await mightFail(true)
            } catch (e) {
                error_caught <- true
                result_value <- e
            }
            
            Assert.Equal(""Success"", result_value)
            Assert.Equal(""Something went wrong"", result_value)
            Assert.True(error_caught)
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
    public void AsyncLoop_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func processItems(items:List<int>) -> List<int> {
                results <- {}
                for item in items {
                    await Task.Delay(10)
                    processed <- item * 2
                    results <- results.Add(processed)
                }
                return results
            }
            
            input_items <- {1, 2, 3, 4, 5}
            processed_items <- await processItems(input_items)
            
            Assert.Equal({2, 4, 6, 8, 10}, processed_items)
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
    public void AsyncConditionalExecution_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func conditionalLoad(load_fast:bool) -> string {
                if load_fast {
                    await Task.Delay(10)
                    return ""Fast load complete""
                } else {
                    await Task.Delay(100)
                    return ""Slow load complete""
                }
            }
            
            result1 <- await conditionalLoad(true)
            result2 <- await conditionalLoad(false)
            
            Assert.Equal(""Fast load complete"", result1)
            Assert.Equal(""Slow load complete"", result2)
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
    public void AsyncWithTimeout_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func operationWithTimeout() -> string {
                timeout_task <- Task.Delay(50)
                operation_task <- Task.Delay(200)
                
                completed <- await Task.WhenAny([timeout_task, operation_task])
                
                if completed == timeout_task {
                    return ""Operation timed out""
                } else {
                    return ""Operation completed""
                }
            }
            
            result <- await operationWithTimeout()
            Assert.Equal(""Operation timed out"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}