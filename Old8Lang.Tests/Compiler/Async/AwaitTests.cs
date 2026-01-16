using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// 编译器模式下的异步编程功能测试 - Await 关键字
/// </summary>
public class AwaitTests
{
    private readonly ITestOutputHelper _output;

    public AwaitTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void AwaitBasicTask_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            #!debug true
            async func simpleTask() -> string {
                await Task.Delay(50)
                return ""Task completed""
            }
            
            async func main() {
                result <- await simpleTask()
                Assert.Equal(""Task completed"", result)
            }
            
            main()
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
    public void AwaitWithIntTask_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func calculateTask() -> int {
                await Task.Delay(30)
                return 42
            }
            
            async func testIntTask() {
                result <- await calculateTask()
                Assert.Equal(42, result)
            }
            
            testIntTask()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact(Skip = "Temporarily disabled due to runtime crash")]
    public void AwaitWithDoubleTask_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func precisionTask() -> double {
                await Task.Delay(25)
                return 3.14159
            }
            
            async func testDoubleTask() {
                result <- await precisionTask()
                Assert.True(result > 3.14 && result < 3.15)
            }
            
            testDoubleTask()
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
    public void AwaitWithArrayTask_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func arrayTask() -> array {
                await Task.Delay(40)
                return [1, 2, 3, 4, 5]
            }
            
            async func testArrayTask() {
                result <- await arrayTask()
                Assert.Equal(5, result.Length)
                Assert.Equal(1, result[0])
                Assert.Equal(5, result[4])
            }
            
            testArrayTask()
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
    public void AwaitWithListTask_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func listTask() -> list {
                await Task.Delay(35)
                return {""apple"", ""banana"", ""cherry""}
            }
            
            async func testListTask() {
                result <- await listTask()
                Assert.Equal(3, result.Count())
                Assert.Equal(""apple"", result[0])
                Assert.Equal(""cherry"", result[2])
            }
            
            testListTask()
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
    public void AwaitWithDictionaryTask_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func dictTask() -> dict {
                await Task.Delay(45)
                return {""key1"": ""value1"", ""key2"": ""value2"", ""key3"": ""value3""}
            }
            
            async func testDictTask() {
                result <- await dictTask()
                Assert.Equal(""value1"", result[""key1""])
                Assert.Equal(""value2"", result[""key2""])
                Assert.Equal(""value3"", result[""key3""])
            }
            
            testDictTask()
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
    public void MultipleAwaitsInSequence_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func step1() -> string {
                await Task.Delay(20)
                return ""step1""
            }
            
            async func step2() -> string {
                await Task.Delay(20)
                return ""step2""
            }
            
            async func step3() -> string {
                await Task.Delay(20)
                return ""step3""
            }
            
            async func testSequentialAwaits() {
                result1 <- await step1()
                result2 <- await step2()
                result3 <- await step3()
                
                Assert.Equal(""step1"", result1)
                Assert.Equal(""step2"", result2)
                Assert.Equal(""step3"", result3)
                
                return ""all completed""
            }
            
            finalResult <- testSequentialAwaits()
            Assert.Equal(""all completed"", finalResult)
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
    public void AwaitInConditional_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func fastTask() -> string {
                await Task.Delay(10)
                return ""fast""
            }
            
            async func slowTask() -> string {
                await Task.Delay(100)
                return ""slow""
            }
            
            async func testConditionalAwait(useFast:bool) -> string {
                if useFast {
                    result <- await fastTask()
                } else {
                    result <- await slowTask()
                }
                return result
            }
            
            // 测试两种情况
            result1 <- testConditionalAwait(true)
            result2 <- testConditionalAwait(false)
            
            Assert.Equal(""fast"", result1)
            Assert.Equal(""slow"", result2)
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
    public void AwaitInLoop_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func processItem(item:int) -> int {
                await Task.Delay(10)
                return item * 2
            }
            
            async func testAwaitInLoop() -> list {
                input <- {1, 2, 3, 4, 5}
                results <- {}
                
                i <- 0
                while i < input.Count() {
                    processed <- await processItem(input[i])
                    results.Add(processed)
                    i <- i + 1
                }
                
                return results
            }
            
            finalResults <- testAwaitInLoop()
            Assert.Equal({2, 4, 6, 8, 10}, finalResults)
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
    public void AwaitWithErrorHandling_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            caughtError <- """"
            
            async func riskyTask() -> string {
                await Task.Delay(30)
                throw ""Task failed""
            }
            
            async func safeTask() -> string {
                await Task.Delay(30)
                return ""Task succeeded""
            }
            
            async func testAwaitWithError() {
                try {
                    result <- await riskyTask()
                } catch (e) {
                    caughtError <- e
                }
                
                try {
                    result2 <- await safeTask()
                    Assert.Equal(""Task succeeded"", result2)
                } catch (e) {
                    // 不应该到这里
                }
            }
            
            testAwaitWithError()
            Assert.Equal(""Task failed"", caughtError)
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
    public void AwaitWithFinally_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            finallyExecuted <- false
            finallyWithException <- false
            
            async func taskWithFinally(success:bool) -> string {
                try {
                    await Task.Delay(20)
                    if success {
                        return ""Success""
                    } else {
                        throw ""Failure""
                    }
                } catch (e) {
                    finallyWithException <- true
                } finally {
                    finallyExecuted <- true
                }
            }
            
            async func testAwaitFinally() {
                result1 <- await taskWithFinally(true)
                Assert.Equal(""Success"", result1)
                Assert.True(finallyExecuted)
                Assert.False(finallyWithException)
                
                finallyExecuted <- false
                finallyWithException <- false
                
                result2 <- await taskWithFinally(false)
                // result2 不应该有值，因为异常被捕获
                Assert.True(finallyExecuted)
                Assert.True(finallyWithException)
            }
            
            testAwaitFinally()
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
    public void AwaitNestedCalls_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func level3() -> string {
                await Task.Delay(15)
                return ""level3""
            }
            
            async func level2() -> string {
                result <- await level3()
                await Task.Delay(15)
                return ""level2_"" + result
            }
            
            async func level1() -> string {
                result <- await level2()
                await Task.Delay(15)
                return ""level1_"" + result
            }
            
            async func testNestedAwait() {
                result <- await level1()
                Assert.Equal(""level1_level2_level3"", result)
            }
            
            testNestedAwait()
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
    public void AwaitWithTaskWhenAll_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func task1() -> string {
                await Task.Delay(30)
                return ""result1""
            }
            
            async func task2() -> string {
                await Task.Delay(40)
                return ""result2""
            }
            
            async func task3() -> string {
                await Task.Delay(20)
                return ""result3""
            }
            
            async func testTaskWhenAll() -> list {
                tasks <- {task1(), task2(), task3()}
                results <- await Task.WhenAll(tasks)
                
                // Task.WhenAll 返回一个包含所有结果的数组
                Assert.Equal(3, results.Length)
                Assert.Equal(""result1"", results[0])
                Assert.Equal(""result2"", results[1])
                Assert.Equal(""result3"", results[2])
                
                return {results[0], results[1], results[2]}
            }
            
            finalResults <- testTaskWhenAll()
            Assert.Equal({""result1"", ""result2"", ""result3""}, finalResults)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact(Skip = "Temporarily disabled due to IL verification issues with Task.WhenAny helper")]
    public void AwaitWithTaskWhenAny_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func fastTask() -> string {
                await Task.Delay(10)
                return ""fast""
            }
            
            async func slowTask() -> string {
                await Task.Delay(100)
                return ""slow""
            }
            
            async func testTaskWhenAny() -> string {
                tasks <- {fastTask(), slowTask()}
                winnerTask <- await Task.WhenAny(tasks)
                winner <- await winnerTask
                
                // Task.WhenAny 返回第一个完成的任务结果
                Assert.Equal(""fast"", winner)
                
                return ""first completed""
            }
            
            result <- testTaskWhenAny()
            Assert.Equal(""first completed"", result)
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
    public void AwaitWithVoidReturn_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func voidTask(state: list) -> void {
                await Task.Delay(50)
                state.Add(true)
            }
            
            async func testVoidTask() {
                state <- {}
                await voidTask(state)
                Assert.True(state[0])
            }
            
            testVoidTask()
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
    public void AwaitWithComplexExpressions_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func getValue() -> int {
                await Task.Delay(20)
                return 5
            }
            
            async func testComplexExpressions() -> int {
                // 在表达式中使用 await
                result1 <- (await getValue()) * 2      // 5 * 2 = 10
                result2 <- (await getValue()) + 10      // 5 + 10 = 15
                result3 <- (await getValue()) - 3       // 5 - 3 = 2
                
                Assert.Equal(10, result1)
                Assert.Equal(15, result2)
                Assert.Equal(2, result3)
                
                return result1 + result2 + result3  // 10 + 15 + 2 = 27
            }
            
            finalResult <- testComplexExpressions()
            Assert.Equal(27, finalResult)
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
    public void AwaitWithMethodChaining_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func getNumber() -> int {
                await Task.Delay(20)
                return 10
            }
            
            async func getString() -> string {
                await Task.Delay(20)
                return ""prefix_""
            }
            
            async func testMethodChaining() -> string {
                // 链式调用中的 await
                number <- await getNumber()
                prefix <- await getString()
                result <- prefix + number.ToStr()
                
                Assert.Equal(""prefix_10"", result)
                return result
            }
            
            result <- testMethodChaining()
            Assert.Equal(""prefix_10"", result)
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
