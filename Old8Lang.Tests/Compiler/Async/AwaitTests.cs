using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// Await 关键字测试
/// </summary>
[Collection("Sequential")]
public class AwaitTests
{
    [Fact]
    public void BasicAwait_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func getValue() -> int {
                await Task.Delay(50)
                return 42
            }
            
            result <- await getValue()
            Assert.Equal(42, result)
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
    public void AwaitInFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func processData(input:int) -> int {
                delay_result <- await Task.Delay(100)
                return input * 2
            }
            
            result <- await processData(10)
            Assert.Equal(20, result)
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
    public void AwaitMultipleSequential_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func fetchData() -> string {
                await Task.Delay(50)
                return ""Data1""
            }
            
            async func fetchMoreData() -> string {
                await Task.Delay(50)
                return ""Data2""
            }
            
            async func getAllData() -> string {
                data1 <- await fetchData()
                data2 <- await fetchMoreData()
                return data1 + ""-"" + data2
            }
            
            result <- await getAllData()
            Assert.Equal(""Data1-Data2"", result)
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
    public void AwaitInLoop_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func processItems(items:list) -> list {
                results <- {}
                for item in items {
                    await Task.Delay(10)
                    processed <- item * 2
                    results <- results.Add(processed)
                }
                return results
            }
            
            input <- {1, 2, 3, 4, 5}
            results <- await processItems(input)
            
            Assert.Equal({2, 4, 6, 8, 10}, results)
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
    public void AwaitInCondition_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func checkCondition(value:int) -> string {
                await Task.Delay(50)
                if value > 10 {
                    return ""Large""
                } else {
                    return ""Small""
                }
            }
            
            result1 <- await checkCondition(15)
            result2 <- await checkCondition(5)
            
            Assert.Equal(""Large"", result1)
            Assert.Equal(""Small"", result2)
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
    public void AwaitInTryCatch_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func riskyOperation() -> string {
                try {
                    await Task.Delay(50)
                    throw ""Operation failed""
                } catch (e) {
                    return ""Error caught: "" + e
                }
            }
            
            result <- await riskyOperation()
            Assert.True(result.StartsWith(""Error caught:""))
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
    public void AwaitInFinally_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cleanup_executed <- false
            
            async func operation() -> int {
                try {
                    await Task.Delay(50)
                    return 42
                } finally {
                    cleanup_executed <- true
                }
            }
            
            result <- await operation()
            Assert.Equal(42, result)
            Assert.True(cleanup_executed)
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
    public void AwaitTaskAPI_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func testTaskAPI() -> string {
                // Test Task.WhenAll
                task1 <- Task.Delay(50, ""First"")
                task2 <- Task.Delay(100, ""Second"")
                task3 <- Task.Delay(25, ""Third"")
                
                results <- await Task.WhenAll([task1, task2, task3])
                return results[0] + ""-"" + results[1] + ""-"" + results[2]
            }
            
            async func testWhenAny() -> string {
                task1 <- Task.Delay(200, ""Slow"")
                task2 <- Task.Delay(50, ""Fast"")
                
                result <- await Task.WhenAny([task1, task2])
                return result
            }
            
            all_result <- await testTaskAPI()
            any_result <- await testWhenAny()
            
            Assert.Equal(""First-Second-Third"", all_result)
            Assert.Equal(""Fast"", any_result)
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
    public void AwaitComplexScenario_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func fetchUserData(userId:int) -> string {
                await Task.Delay(50)
                return ""User_"" + userId.ToStr()
            }
            
            async func fetchUserPosts(userId:int) -> string {
                user_data <- await fetchUserData(userId)
                await Task.Delay(50)
                return user_data + ""_posts""
            }
            
            async func getCompleteUser(userId:int) -> string {
                // Parallel fetch user data and posts
                user_task <- fetchUserData(userId)
                posts_task <- fetchUserPosts(userId)
                
                user_data <- await user_task
                posts_data <- await posts_task
                
                return user_data + "" | "" + posts_data
            }
            
            result <- await getCompleteUser(123)
            Assert.Equal(""User_123|User_123_posts"", result)
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
    public void AwaitWithTimeout_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func operationWithTimeout() -> string {
                // Short operation that should complete
                short_task <- Task.Delay(50, ""Complete"")
                
                // Long operation that should timeout
                long_task <- Task.Delay(200, ""Timeout"")
                
                completed <- await Task.WhenAny([short_task, long_task])
                
                return completed
            }
            
            result <- await operationWithTimeout()
            Assert.Equal(""Complete"", result)
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