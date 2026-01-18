using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// 编译器模式下的异步编程功能测试 - 异步函数
/// </summary>
public class AsyncFunctionTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void BasicAsyncFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func fetchData() -> string {
                await Task.Delay(100)
                return ""Data from server""
            }
            
            async func main() {
                data <- await fetchData()
                Assert.Equal(""Data from server"", data)
            }
            
            result <- main()
            Assert.True(result != null)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        
        // Enable debug output to see generated IL code
        Old8Lang.Compiler.Compiler.DebugOutputEnabled = true;
        // Disable IL verification to get more detailed error information
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;
        Old8Lang.Compiler.Compiler.DebugOutputEnabled = false;

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AsyncFunctionWithParameters_CompilesAndExecutesCorrectly()
    {
        // Arrange
        Old8Lang.Compiler.Compiler.DebugOutputEnabled = true;
        var code = @"
            async func calculateSum(a:int, b:int) -> int {
                await Task.Delay(50)
                return a + b
            }
            
            async func testCalculation() {
                result1 <- await calculateSum(10, 20)
                result2 <- await calculateSum(5, 15)
                
                Assert.Equal(30, result1)
                Assert.Equal(20, result2)
            }
            
            testCalculation()
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
    public void AsyncFunctionReturningVoid_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            executed <- false
            
            async func processData() -> void {
                await Task.Delay(100)
                executed <- true
            }
            
            async func main() {
                await processData()
                Assert.True(executed)
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
    public void MultipleAsyncCalls_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func getValue(id:int) -> string {
                await Task.Delay(50)
                return ""value_"" + id.ToStr()
            }
            
            async func fetchMultipleValues() {
                value1 <- await getValue(1)
                value2 <- await getValue(2)
                value3 <- await getValue(3)
                
                Assert.Equal(""value_1"", value1)
                Assert.Equal(""value_2"", value2)
                Assert.Equal(""value_3"", value3)
                
                return ""all fetched""
            }
            
            result <- fetchMultipleValues()
            Assert.Equal(""all fetched"", result)
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
    public void AsyncFunctionWithError_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            caughtError <- """"
            
            async func riskyOperation() -> string {
                await Task.Delay(50)
                throw ""Something went wrong""
            }
            
            async func handleAsyncError() {
                try {
                    result <- await riskyOperation()
                } catch (e) {
                    caughtError <- e
                }
            }
            
            handleAsyncError()
            Assert.Equal(""Something went wrong"", caughtError)
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
    public void AsyncFunctionWithLoops_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func processItems(items:list) -> list {
                results <- {}
                i <- 0
                while i < items.Count() {
                    item <- items[i]
                    processed <- item * 2
                    await Task.Delay(10)
                    results.Add(processed)
                    i <- i + 1
                }
                return results
            }
            
            async func testAsyncLoop() {
                input <- {1, 2, 3, 4, 5}
                results <- await processItems(input)
                
                Assert.Equal(5, results.Count())
                Assert.Equal({2, 4, 6, 8, 10}, results)
            }
            
            testAsyncLoop()
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
    public void AsyncFunctionChaining_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func step1(data:int) -> int {
                await Task.Delay(30)
                return data * 2
            }
            
            async func step2(data:int) -> int {
                await Task.Delay(30)
                return data + 10
            }
            
            async func step3(data:int) -> string {
                await Task.Delay(30)
                return ""result_"" + data.ToStr()
            }
            
            async func chainAsyncOperations() {
                result1 <- await step1(5)   // 10
                result2 <- await step2(result1)  // 20
                result3 <- await step3(result2)  // ""result_20""
                
                Assert.Equal(10, result1)
                Assert.Equal(20, result2)
                Assert.Equal(""result_20"", result3)
            }
            
            chainAsyncOperations()
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
    public void AsyncFunctionWithConditional_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func conditionalOperation(useFast:bool) -> string {
                if useFast {
                    await Task.Delay(10)
                    return ""fast operation""
                } else {
                    await Task.Delay(100)
                    return ""slow operation""
                }
            }
            
            async func testConditional() {
                result1 <- await conditionalOperation(true)
                result2 <- await conditionalOperation(false)
                
                Assert.Equal(""fast operation"", result1)
                Assert.Equal(""slow operation"", result2)
            }
            
            testConditional()
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
    public void AsyncFunctionWithArray_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func processArray(data:array) -> array {
                results <- []
                i <- 0
                while i < data.Length {
                    value <- data[i]
                    processed <- value + 100
                    await Task.Delay(20)
                    results.Add(processed)
                    i <- i + 1
                }
                return results
            }
            
            async func testArrayProcessing() {
                input <- [1, 2, 3, 4, 5]
                results <- await processArray(input)
                
                Assert.Equal(5, results.Length)
                Assert.Equal([101, 102, 103, 104, 105], results)
            }
            
            testArrayProcessing()
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
    public void AsyncFunctionWithDictionary_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func processDictionary(data:dict) -> dict {
                results <- {}
                
                // 假设字典有迭代方法
                keys <- {""key1"", ""key2"", ""key3""}
                i <- 0
                while i < keys.Count() {
                    key <- keys[i]
                    value <- data[key]
                    processed <- value * 10
                    await Task.Delay(30)
                    results[key] <- processed
                    i <- i + 1
                }
                
                return results
            }
            
            async func testDictionaryProcessing() {
                input <- {""key1"": 5, ""key2"": 10, ""key3"": 15}
                results <- await processDictionary(input)
                
                Assert.Equal(50, results[""key1""])
                Assert.Equal(100, results[""key2""])
                Assert.Equal(150, results[""key3""])
            }
            
            testDictionaryProcessing()
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
    public void AsyncFunctionWithFinally_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cleanupExecuted <- false
            
            async func operationWithCleanup() -> string {
                try {
                    await Task.Delay(50)
                    return ""operation completed""
                } catch (e) {
                    throw e
                } finally {
                    await Task.Delay(20)
                    cleanupExecuted <- true
                }
            }
            
            async func testFinally() {
                result <- await operationWithCleanup()
                Assert.Equal(""operation completed"", result)
                Assert.True(cleanupExecuted)
            }
            
            testFinally()
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
    public void AsyncFunctionWithNestedCalls_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func innerAsync(value:int) -> int {
                await Task.Delay(30)
                return value * 3
            }
            
            async func middleAsync(value:int) -> int {
                innerResult <- await innerAsync(value)
                await Task.Delay(20)
                return innerResult + 5
            }
            
            async func outerAsync(value:int) -> string {
                middleResult <- await middleAsync(value)
                await Task.Delay(10)
                return ""final_"" + middleResult.ToStr()
            }
            
            async func testNested() {
                result <- await outerAsync(10)  // ((10 * 3) + 5) = 35 -> ""final_35""
                Assert.Equal(""final_35"", result)
            }
            
            testNested()
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