using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// 异步生成器（Async Generator）测试
/// </summary>
[Collection("Sequential")]
public class AsyncGeneratorTests
{
    [Fact]
    public void BasicAsyncGenerator_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func countToN(n:int) {
                i <- 1
                while i <= n {
                    await Task.Delay(10)
                    yield i
                    i <- i + 1
                }
            }
            
            count <- 0
            async for num in countToN(5) {
                count <- count + 1
            }
            
            Assert.Equal(5, count)
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
    public void AsyncGeneratorWithFilter_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func evenNumbersUpTo(n:int) {
                i <- 1
                while i <= n {
                    if i % 2 == 0 {
                        yield i
                    }
                    i <- i + 1
                    await Task.Delay(5)
                }
            }
            
            evens_list <- {}
            async for num in evenNumbersUpTo(10) {
                evens_list <- evens_list.Add(num)
            }
            
            Assert.Equal({2, 4, 6, 8, 10}, evens_list)
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
    public void AsyncGeneratorWithTransform_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func squaresUpTo(n:int) {
                i <- 1
                while i <= n {
                    yield i * i
                    i <- i + 1
                    await Task.Delay(5)
                }
            }
            
            squares_sum <- 0
            async for square in squaresUpTo(5) {
                squares_sum <- squares_sum + square
            }
            
            Assert.Equal(55, squares_sum)  // 1 + 4 + 9 + 16 + 25
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
    public void AsyncGeneratorInClass_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class AsyncSequence {
                private start <- 0
                private end <- 0
                
                func init(start_val:int, end_val:int) -> void {
                    this.start <- start_val
                    this.end <- end_val
                }
                
                async func generate() {
                    value <- this.start
                    while value <= this.end {
                        yield value
                        value <- value + 1
                        await Task.Delay(10)
                    }
                }
            }
            
            seq <- AsyncSequence(1, 5)
            sum <- 0
            async for num in seq.generate() {
                sum <- sum + num
            }
            
            Assert.Equal(21, sum)  // 1 + 2 + 3 + 4 + 5
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
    public void AsyncGeneratorWithException_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func generatorWithError(should_fail:bool) {
                i <- 1
                while i <= 5 {
                    if should_fail and i == 3 {
                        throw ""Generated error at iteration "" + i
                    }
                    yield i
                    i <- i + 1
                    await Task.Delay(5)
                }
            }
            
            error_caught <- false
            results <- {}
            
            try {
                async for num in generatorWithError(true) {
                    results <- results.Add(num)
                }
            } catch (e) {
                error_caught <- true
            }
            
            Assert.True(error_caught)
            Assert.Equal({1, 2}, results)
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
    public void NestedAsyncGenerator_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func nestedGenerator() {
                // Outer generator
                i <- 1
                while i <= 3 {
                    // Inner generator for each outer iteration
                    j <- 1
                    while j <= 2 {
                        yield i * 10 + j  // Combine i and j to create unique numbers
                        j <- j + 1
                        await Task.Delay(5)
                    }
                    i <- i + 1
                    await Task.Delay(10)
                }
            }
            
            results <- {}
            async for num in nestedGenerator() {
                results <- results.Add(num)
            }
            
            expected <- {11, 12, 21, 22, 31, 32}  // (1*10+1, 1*10+2, 2*10+1, 2*10+2, 3*10+1, 3*10+2)
            Assert.Equal(expected, results)
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
    public void AsyncGeneratorWithEarlyReturn_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func findFirstMatch(items:list, target:int) -> int? {
                for item in items {
                    await Task.Delay(5)
                    if item == target {
                        return item
                    }
                }
                return null
            }
            
            numbers <- {5, 10, 15, 20, 25}
            result1 <- await FindFirstMatch(numbers, 15)
            result2 <- await FindFirstMatch(numbers, 99)
            
            Assert.Equal(15, result1)
            Assert.Equal(null, result2)
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
    public void AsyncGeneratorWithAccumulator_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func runningSum() {
                sum <- 0
                i <- 1
                while i <= 5 {
                    sum <- sum + i
                    yield sum
                    i <- i + 1
                    await Task.Delay(5)
                }
            }
            
            partial_sums <- {}
            async for partial_sum in runningSum() {
                partial_sums <- partial_sums.Add(partial_sum)
            }
            
            Assert.Equal({1, 3, 6, 10, 15}, partial_sums)
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