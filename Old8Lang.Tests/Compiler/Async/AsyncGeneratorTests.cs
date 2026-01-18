using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// 编译器模式下的异步编程功能测试 - 异步生成器
/// </summary>
public class AsyncGeneratorTests
{
    private readonly ITestOutputHelper _output;

    public AsyncGeneratorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BasicAsyncGenerator_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func countNumbers() {
                i <- 1
                while i <= 5 {
                    await Task.Delay(10)
                    yield i
                    i <- i + 1
                }
            }
            
            async func consumeGenerator() {
                sum <- 0
                count <- 0
                async for num in countNumbers() {
                    sum <- sum + num
                    count <- count + 1
                }
                Assert.Equal(15, sum)  // 1+2+3+4+5 = 15
                Assert.Equal(5, count)
            }
            
            consumeGenerator()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;

        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AsyncGeneratorWithStrings_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func generateStrings() {
                items <- {""apple"", ""banana"", ""cherry""}
                i <- 0
                while i < items.Count() {
                    await Task.Delay(5)
                    yield items[i]
                    i <- i + 1
                }
            }
            
            async func consumeStringGenerator() {
                results <- {}
                count <- 0
                async for fruit in generateStrings() {
                    results.Add(fruit)
                    count <- count + 1
                }
                Assert.Equal(3, count)
                Assert.Equal({""apple"", ""banana"", ""cherry""}, results)
            }
            
            consumeStringGenerator()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;

        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AsyncGeneratorWithCondition_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func generateEvenNumbers(max:int) {
                i <- 0
                while i <= max {
                    if i % 2 == 0 {
                        await Task.Delay(5)
                        yield i
                    }
                    i <- i + 1
                }
            }
            
            async func consumeConditionalGenerator() {
                results <- {}
                async for num in generateEvenNumbers(10) {
                    results.Add(num)
                }
                Assert.Equal({0, 2, 4, 6, 8, 10}, results)
            }
            
            consumeConditionalGenerator()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;

        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AsyncGeneratorWithEarlyExit_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func generateNumbersWithLimit(limit:int) {
                i <- 1
                while true {  // 无限循环
                    await Task.Delay(10)
                    if i > limit {
                        break
                    }
                    yield i
                    i <- i + 1
                }
            }
            
            async func consumeLimitedGenerator() {
                sum <- 0
                count <- 0
                async for num in generateNumbersWithLimit(5) {
                    sum <- sum + num
                    count <- count + 1
                }
                Assert.Equal(15, sum)  // 1+2+3+4+5 = 15
                Assert.Equal(5, count)
            }
            
            consumeLimitedGenerator()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;

        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void NestedAsyncGenerator_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func innerGenerator(start:int, count:int) {
                i <- 0
                while i < count {
                    await Task.Delay(5)
                    yield start + i
                    i <- i + 1
                }
            }
            
            async func outerGenerator() {
                base <- 10
                async for num in innerGenerator(base, 3) {
                    await Task.Delay(5)
                    yield num * 2
                }
            }
            
            async func consumeNestedGenerator() {
                results <- {}
                async for num in outerGenerator() {
                    results.Add(num)
                }
                Assert.Equal({20, 22, 24}, results)  // (10+0)*2, (10+1)*2, (10+2)*2
            }
            
            consumeNestedGenerator()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;

        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AsyncGeneratorWithError_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            caughtError <- """"
            
            async func generatorWithError() {
                i <- 1
                while i <= 5 {
                    await Task.Delay(10)
                    if i == 3 {
                        throw ""Error at iteration "" + i.ToStr()
                    }
                    yield i
                    i <- i + 1
                }
            }
            
            async func consumeGeneratorWithError() {
                try {
                    sum <- 0
                    async for num in generatorWithError() {
                        sum <- sum + num
                    }
                } catch (e) {
                    caughtError <- e
                }
            }
            
            consumeGeneratorWithError()
            Assert.Equal(""Error at iteration 3"", caughtError)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;

        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AsyncGeneratorWithComplexLogic_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func fibonacciGenerator(count:int) {
                if count <= 0 {
                    return
                }
                
                a <- 0
                b <- 1
                i <- 0
                
                while i < count {
                    await Task.Delay(5)
                    yield a
                    
                    next <- a + b
                    a <- b
                    b <- next
                    i <- i + 1
                }
            }
            
            async func consumeFibonacciGenerator() {
                results <- {}
                async for num in fibonacciGenerator(10) {
                    results.Add(num)
                }
                
                expected <- {0, 1, 1, 2, 3, 5, 8, 13, 21, 34}
                Assert.Equal(expected, results)
            }
            
            consumeFibonacciGenerator()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;

        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AsyncGeneratorWithFiltering_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func numberGenerator() {
                i <- 1
                while i <= 20 {
                    await Task.Delay(2)
                    yield i
                    i <- i + 1
                }
            }
            
            async func filterPrimes() {
                primes <- {}
                
                async for num in numberGenerator() {
                    isPrime <- true
                    if num < 2 {
                        isPrime <- false
                    } else {
                        divisor <- 2
                        while divisor * divisor <= num {
                            if num % divisor == 0 {
                                isPrime <- false
                                break
                            }
                            divisor <- divisor + 1
                        }
                    }
                    
                    if isPrime {
                        await Task.Delay(5)
                        yield num
                    }
                }
            }
            
            async func consumeFilteredPrimes() {
                results <- {}
                async for prime in filterPrimes() {
                    results.Add(prime)
                    if results.Count() >= 5 {
                        break  // 只取前5个质数
                    }
                }
                
                expected <- {2, 3, 5, 7, 11}
                Assert.Equal(expected, results)
            }
            
            consumeFilteredPrimes()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;

        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AsyncGeneratorWithState_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func statefulGenerator(start:int) {
                state <- start
                iteration <- 0
                
                while iteration < 5 {
                    await Task.Delay(10)
                    
                    // 根据状态产生不同的值
                    if state % 2 == 0 {
                        yield state * 2
                    } else {
                        yield state + 10
                    }
                    
                    state <- state + state  // 状态变化
                    iteration <- iteration + 1
                }
            }
            
            async func consumeStatefulGenerator() {
                results <- {}
                async for value in statefulGenerator(1) {
                    results.Add(value)
                }
                
                // 手动验证前几次迭代的值
                // start=1: 1+10=11, state=2
                // state=2: 2*2=4, state=4  
                // state=4: 4*2=8, state=8
                // state=8: 8*2=16, state=16
                // state=16: 16*2=32, state=32
                
                expected <- {11, 4, 8, 16, 32}
                Assert.Equal(expected, results)
            }
            
            consumeStatefulGenerator()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;

        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AsyncGeneratorWithResourceCleanup_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            resourceOpened <- false
            resourceClosed <- false
            
            async func openResource() -> void {
                resourceOpened <- true
            }
            
            async func closeResource() -> void {
                resourceClosed <- true
            }
            
            async func resourceGenerator() {
                await openResource()
                try {
                    i <- 1
                    while i <= 3 {
                        await Task.Delay(10)
                        yield ""resource_value_"" + i.ToStr()
                        i <- i + 1
                    }
                } finally {
                    await closeResource()
                }
            }
            
            async func consumeResourceGenerator() {
                results <- {}
                async for value in resourceGenerator() {
                    results.Add(value)
                }
                
                Assert.Equal({""resource_value_1"", ""resource_value_2"", ""resource_value_3""}, results)
                Assert.True(resourceOpened)
                Assert.True(resourceClosed)
            }
            
            consumeResourceGenerator()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;

        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AsyncGeneratorWithParallelProcessing_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func processDataItem(item:int) -> string {
                await Task.Delay(20)
                return ""processed_"" + item.ToStr()
            }
            
            async func processingGenerator(items:list) {
                i <- 0
                while i < items.Count() {
                    item <- items[i]
                    result <- await processDataItem(item)
                    await Task.Delay(5)
                    yield result
                    i <- i + 1
                }
            }
            
            async func consumeProcessingGenerator() {
                results <- {}
                async for processed in processingGenerator({1, 2, 3}) {
                    results.Add(processed)
                }
                
                expected <- {""processed_1"", ""processed_2"", ""processed_3""}
                Assert.Equal(expected, results)
            }
            
            consumeProcessingGenerator()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;

        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}