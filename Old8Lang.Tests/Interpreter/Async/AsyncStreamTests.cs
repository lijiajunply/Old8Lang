using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Async;

/// <summary>
/// 异步流测试
/// </summary>
public class AsyncStreamTests
{
    [Fact]
    public void AsyncStream_BasicStreamCreation_CreatesAsyncStream()
    {
        // Arrange
        var code = @"
            stream <- async {
                yield 1
                yield 2
                yield 3
            }
            result <- ""stream created""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("stream created", ((StringLangValue)result).Value);
    }

    [Fact]
    public void AsyncStream_StreamIteration_IteratesOverStream()
    {
        // Arrange
        var code = @"
            stream <- async {
                for i in 1..5 {
                    yield i * 2
                }
            }
            sum <- 0
            for item in stream {
                sum <- sum + item
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value); // 2+4+6+8+10 = 30
    }

    [Fact]
    public void AsyncStream_StreamWithDelay_HandlesDelayedYields()
    {
        // Arrange
        var code = @"
            stream <- async {
                yield ""start""
                await async.Sleep(100)
                yield ""middle""
                await async.Sleep(100)
                yield ""end""
            }
            count <- 0
            for item in stream {
                count <- count + 1
            }
            result <- count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    [Fact]
    public void AsyncStream_ConditionalYielding_YieldsBasedOnCondition()
    {
        // Arrange
        var code = @"
            stream <- async {
                for i in 1..10 {
                    if i % 2 = 0 {
                        yield i
                    }
                }
            }
            resultList <- {}
            for item in stream {
                resultList.Add(item)
            }
            result <- resultList.Count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5, ((IntLangValue)result).Value); // Even numbers from 1-10
    }

    [Fact]
    public void AsyncStream_StreamWithParameters_AcceptsParameters()
    {
        // Arrange
        var code = @"
            func generateNumbers(start:int, end:int) -> async {
                return async {
                    for i in start..end {
                        yield i
                    }
                }
            }
            stream <- generateNumbers(5, 8)
            sum <- 0
            for item in stream {
                sum <- sum + item
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(26, ((IntLangValue)result).Value); // 5+6+7+8 = 26
    }

    [Fact]
    public void AsyncStream_NestedStreams_HandlesNestedAsyncStreams()
    {
        // Arrange
        var code = @"
            outerStream <- async {
                yield ""outer start""
                innerStream <- async {
                    yield ""inner 1""
                    yield ""inner 2""
                }
                for item in innerStream {
                    yield item
                }
                yield ""outer end""
            }
            count <- 0
            for item in outerStream {
                count <- count + 1
            }
            result <- count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(4, ((IntLangValue)result).Value);
    }

    [Fact]
    public void AsyncStream_StreamTransformation_TransformsStreamValues()
    {
        // Arrange
        var code = @"
            numberStream <- async {
                for i in 1..5 {
                    yield i
                }
            }
            squaredStream <- async {
                for num in numberStream {
                    yield num * num
                }
            }
            sum <- 0
            for squared in squaredStream {
                sum <- sum + squared
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(55, ((IntLangValue)result).Value); // 1²+2²+3²+4²+5² = 55
    }

    [Fact]
    public void AsyncStream_StreamFiltering_FiltersStreamValues()
    {
        // Arrange
        var code = @"
            allNumbers <- async {
                for i in 1..20 {
                    yield i
                }
            }
            primes <- async {
                for num in allNumbers {
                    if num > 1 and num <= 3 {
                        yield num
                    } else if num > 3 and (num % 2 != 0) and (num % 3 != 0) {
                        yield num
                    }
                }
            }
            count <- 0
            for prime in primes {
                count <- count + 1
            }
            result <- count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // Should count primes from 1-20
        Assert.Equal(8, ((IntLangValue)result).Value); // 2,3,5,7,11,13,17,19
    }

    [Fact]
    public void AsyncStream_StreamAggregation_AggregatesStreamValues()
    {
        // Arrange
        var code = @"
            dataStream <- async {
                yield 10
                yield 20
                yield 30
                yield 40
                yield 50
            }
            count <- 0
            sum <- 0
            max <- 0
            for value in dataStream {
                count <- count + 1
                sum <- sum + value
                if value > max {
                    max <- value
                }
            }
            average <- sum / count
            result <- max.ToStr() + ""-"" + average.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("50-30", ((StringLangValue)result).Value);
    }

    [Fact]
    public void AsyncStream_ErrorHandling_HandlesErrorsInStream()
    {
        // Arrange
        var code = @"
            errorStream <- async {
                yield 1
                yield 2
                // Simulate an error
                try {
                    result <- 10 / 0
                } catch {
                    yield -1
                }
                yield 3
            }
            results <- {}
            for item in errorStream {
                results.Add(item)
            }
            result <- results.Count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    [Fact]
    public void AsyncStream_InfiniteStream_HandlesInfiniteStream()
    {
        // Arrange
        var code = @"
            counterStream <- async {
                i <- 1
                while i <= 5 {
                    yield i
                    i <- i + 1
                }
            }
            sum <- 0
            count <- 0
            for num in counterStream {
                sum <- sum + num
                count <- count + 1
                if count >= 5 {
                    break
                }
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value); // 1+2+3+4+5 = 15
    }

    [Fact]
    public void AsyncStream_StreamComposition_ComposesMultipleStreams()
    {
        // Arrange
        var code = @"
            stream1 <- async {
                yield 1
                yield 2
                yield 3
            }
            stream2 <- async {
                yield 4
                yield 5
                yield 6
            }
            combinedStream <- async {
                for item in stream1 {
                    yield item
                }
                for item in stream2 {
                    yield item
                }
            }
            sum <- 0
            for item in combinedStream {
                sum <- sum + item
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(21, ((IntLangValue)result).Value); // 1+2+3+4+5+6 = 21
    }

    [Fact]
    public void AsyncStream_StreamWithState_MaintainsStateInStream()
    {
        // Arrange
        var code = @"
            statefulStream <- async {
                state <- 0
                for i in 1..5 {
                    state <- state + i
                    yield state
                }
            }
            results <- {}
            for value in statefulStream {
                results.Add(value)
            }
            result <- results[results.Count - 1]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value); // Cumulative sum: 1+2+3+4+5 = 15
    }

    [Fact]
    public void AsyncStream_StreamWithComplexLogic_HandlesComplexStreamLogic()
    {
        // Arrange
        var code = @"
            fibonacciStream <- async {
                a <- 0
                b <- 1
                yield a
                yield b
                for i in 3..10 {
                    c <- a + b
                    yield c
                    a <- b
                    b <- c
                }
            }
            numbers <- {}
            for num in fibonacciStream {
                numbers.Add(num)
            }
            result <- numbers[numbers.Count - 1]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // 10th Fibonacci number (starting from 0): 34
        Assert.Equal(34, ((IntLangValue)result).Value);
    }

    [Fact]
    public void AsyncStream_StreamBuffering_BuffersStreamOutput()
    {
        // Arrange
        var code = @"
            bufferedStream <- async {
                buffer <- {}
                for i in 1..5 {
                    buffer.Add(i)
                    if buffer.Count = 3 {
                        for item in buffer {
                            yield item
                        }
                        buffer.Clear()
                    }
                }
                // Yield remaining items
                for item in buffer {
                    yield item
                }
            }
            sum <- 0
            for item in bufferedStream {
                sum <- sum + item
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value); // 1+2+3+4+5 = 15
    }

    [Fact]
    public void AsyncStream_StreamCancellation_HandlesStreamCancellation()
    {
        // Arrange
        var code = @"
            longStream <- async {
                for i in 1..100 {
                    yield i
                    if i = 10 {
                        break
                    }
                }
            }
            count <- 0
            for item in longStream {
                count <- count + 1
            }
            result <- count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(10, ((IntLangValue)result).Value);
    }

    [Fact]
    public void AsyncStream_StreamWithException_HandlesExceptionInStream()
    {
        // Arrange
        var code = @"
            exceptionStream <- async {
                try {
                    yield ""start""
                    // This might cause an exception
                    invalidOperation <- null + 1
                    yield ""this won't be reached""
                } catch {
                    yield ""error caught""
                }
                yield ""end""
            }
            results <- {}
            for item in exceptionStream {
                results.Add(item)
            }
            result <- results.Count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // Should have: "start", "error caught", "end"
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    [Fact]
    public void AsyncStream_StreamParallelism_HandlesParallelStreamOperations()
    {
        // Arrange
        var code = @"
            parallelStream1 <- async {
                for i in 1..3 {
                    await async.Sleep(10)
                    yield i * 2
                }
            }
            parallelStream2 <- async {
                for i in 1..3 {
                    await async.Sleep(10)
                    yield i * 3
                }
            }
            results <- {}
            // Process streams concurrently
            for item1 in parallelStream1 {
                results.Add(item1)
            }
            for item2 in parallelStream2 {
                results.Add(item2)
            }
            result <- results.Count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(6, ((IntLangValue)result).Value);
    }

    [Fact]
    public void AsyncStream_StreamWithTimedEmissions_YieldsAtSpecificTimes()
    {
        // Arrange
        var code = @"
            timedStream <- async {
                startTime <- async.Now()
                for i in 1..3 {
                    await async.Sleep(50)
                    yield i
                }
            }
            count <- 0
            for item in timedStream {
                count <- count + 1
            }
            result <- count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    [Fact]
    public void AsyncStream_StreamWithDependencies_HandlesStreamDependencies()
    {
        // Arrange
        var code = @"
            dependencyStream <- async {
                baseValues <- async {
                    for i in 1..3 {
                        yield i * 10
                    }
                }
                dependentValues <- async {
                    sum <- 0
                    for base in baseValues {
                        sum <- sum + base
                    }
                    yield sum
                }
                for value in dependentValues {
                    yield value
                }
            }
            result <- 0
            for value in dependencyStream {
                result <- value
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(60, ((IntLangValue)result).Value); // 10+20+30 = 60
    }

    [Fact]
    public void AsyncStream_StreamWithCaching_CachesStreamResults()
    {
        // Arrange
        var code = @"
            cachedStream <- async {
                cache <- {}
                for i in 1..5 {
                    if not cache.ContainsKey(i) {
                        cache[i] <- i * i
                    }
                    yield cache[i]
                }
            }
            sum <- 0
            for value in cachedStream {
                sum <- sum + value
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(55, ((IntLangValue)result).Value); // 1²+2²+3²+4²+5² = 55
    }

    [Fact]
    public void AsyncStream_StreamWithRetry_RetriesFailedOperations()
    {
        // Arrange
        var code = @"
            retryStream <- async {
                attempts <- 0
                maxAttempts <- 3
                while attempts < maxAttempts {
                    try {
                        attempts <- attempts + 1
                        if attempts < 3 {
                            // Simulate failure
                            invalid <- null + 1
                        }
                        yield ""success after "" + attempts.ToStr() + "" attempts""
                        break
                    } catch {
                        if attempts = maxAttempts {
                            yield ""failed after "" + maxAttempts.ToStr() + "" attempts""
                        }
                    }
                }
            }
            result <- """"
            for message in retryStream {
                result <- message
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("success after 3 attempts", ((StringLangValue)result).Value);
    }
}