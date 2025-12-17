using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Async;

/// <summary>
/// 异步等待表达式测试
/// </summary>
public class AwaitTests
{
    [Fact]
    public void Await_SimpleAsyncFunction_WaitsForResult()
    {
        // Arrange
        var code = @"
            async func fetchData() -> string {
                await Task.Delay(100)
                return ""data loaded""
            }
            result <- await fetchData()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("data loaded", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Await_WithIntegerResult_ReturnsCorrectInt()
    {
        // Arrange
        var code = @"
            async func calculateSum(a:int, b:int) -> int {
                await Task.Delay(50)
                return a + b
            }
            result <- await calculateSum(10, 20)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Await_WithBooleanResult_ReturnsCorrectBool()
    {
        // Arrange
        var code = @"
            async func checkCondition(value:int) -> bool {
                await Task.Delay(30)
                return value > 50
            }
            result <- await checkCondition(75)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void Await_WithDoubleResult_ReturnsCorrectDouble()
    {
        // Arrange
        var code = @"
            async func performCalculation() -> double {
                await Task.Delay(40)
                return 3.14159 * 2.0
            }
            result <- await performCalculation()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(6.28318, ((DoubleLangValue)result).Value, 5);
    }

    [Fact]
    public void Await_ChainedAsyncCalls_HandlesSequentialExecution()
    {
        // Arrange
        var code = @"
            async func step1() -> string {
                await Task.Delay(50)
                return ""step1""
            }
            async func step2(input:string) -> string {
                await Task.Delay(50)
                return input + "" -> step2""
            }
            async func step3(input:string) -> string {
                await Task.Delay(50)
                return input + "" -> step3""
            }
            result <- await step3(await step2(await step1()))
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("step1 -> step2 -> step3", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Await_WithParameters_PassesParametersCorrectly()
    {
        // Arrange
        var code = @"
            async func processUserData(name:string, age:int) -> string {
                await Task.Delay(60)
                return name + "" is "" + age.ToStr() + "" years old""
            }
            result <- await processUserData(""Alice"", 30)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Alice is 30 years old", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Await_WithConditionalExecution_WaitsBasedOnCondition()
    {
        // Arrange
        var code = @"
            async func loadConfig(shouldLoad:bool) -> string {
                if shouldLoad {
                    await Task.Delay(80)
                    return ""config loaded""
                } else {
                    return ""default config""
                }
            }
            result1 <- await loadConfig(true)
            result2 <- await loadConfig(false)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("config loaded", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("default config", ((StringLangValue)result2).Value);
    }

    [Fact]
    public void Await_WithLoop_ProcessesAsyncOperationsInLoop()
    {
        // Arrange
        var code = @"
            async func processItem(item:int) -> string {
                await Task.Delay(30)
                return ""item "" + item.ToStr() + "" processed""
            }
            results <- {""""}
            for i in 1..3 {
                result <- await processItem(i)
                results.Add(result)
            }
            finalResult <- results.Join("" | "")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var finalResult = interpreter.Manager.GetValue(new LangId("finalResult"));
        Assert.NotNull(finalResult);
        Assert.IsType<StringLangValue>(finalResult);
        Assert.Equal("item 1 processed | item 2 processed | item 3 processed", ((StringLangValue)finalResult).Value);
    }

    [Fact]
    public void Await_WithArrayOperations_ProcessesAsyncArrays()
    {
        // Arrange
        var code = @"
            async func fetchItem(index:int) -> int {
                await Task.Delay(40)
                return index * 2
            }
            items <- [0, 0, 0]
            for i in 0..2 {
                items[i] <- await fetchItem(i)
            }
            result <- items
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ArrayLangValue>(result);
        var array = (ArrayLangValue)result;
        Assert.Equal(3, array.Value.Count);
        Assert.Equal(0, ((IntLangValue)array.Value[0]).Value);
        Assert.Equal(2, ((IntLangValue)array.Value[1]).Value);
        Assert.Equal(4, ((IntLangValue)array.Value[2]).Value);
    }

    [Fact]
    public void Await_WithListOperations_ProcessesAsyncLists()
    {
        // Arrange
        var code = @"
            async func generateNumber(value:int) -> int {
                await Task.Delay(25)
                return value * value
            }
            numbers <- {1, 2, 3, 4, 5}
            results <- {}
            for num in numbers {
                result <- await generateNumber(num)
                results.Add(result)
            }
            sum <- 0
            for r in results {
                sum <- sum + r
            }
            finalResult <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var finalResult = interpreter.Manager.GetValue(new LangId("finalResult"));
        Assert.NotNull(finalResult);
        Assert.IsType<IntLangValue>(finalResult);
        Assert.Equal(55, ((IntLangValue)finalResult).Value); // 1+4+9+16+25
    }

    [Fact]
    public void Await_WithDictionaryOperations_ProcessesAsyncDictionaries()
    {
        // Arrange
        var code = @"
            async func lookupUser(id:int) -> string {
                await Task.Delay(50)
                if id == 1 {
                    return ""Alice""
                } else if id == 2 {
                    return ""Bob""
                } else {
                    return ""Unknown""
                }
            }
            userIds <- {""user1"": 1, ""user2"": 2, ""user3"": 3}
            userNames <- {}
            for key in userIds.Keys {
                id <- userIds[key]
                name <- await lookupUser(id)
                userNames[key] <- name
            }
            result <- userNames[""user1""]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Alice", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Await_WithErrorHandling_HandlesAsyncExceptions()
    {
        // Arrange
        var code = @"
            async func riskyOperation(shouldFail:bool) -> string {
                await Task.Delay(40)
                if shouldFail {
                    throw ""Operation failed""
                }
                return ""Operation succeeded""
            }
            try {
                result <- await riskyOperation(false)
            } catch {
                result <- ""Caught exception: "" + exception
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
        Assert.Equal("Operation succeeded", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Await_WithTryCatch_HandlesAsyncExceptionsCorrectly()
    {
        // Arrange
        var code = @"
            async func failingOperation() -> string {
                await Task.Delay(30)
                throw ""Async error occurred""
            }
            try {
                result <- await failingOperation()
            } catch {
                result <- ""Error handled: "" + exception
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
        Assert.Equal("Error handled: Async error occurred", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Await_WithCharacterResult_ReturnsCorrectChar()
    {
        // Arrange
        var code = @"
            async func getGrade(score:int) -> char {
                await Task.Delay(35)
                if score >= 90 {
                    return 'A'
                } else if score >= 80 {
                    return 'B'
                } else {
                    return 'C'
                }
            }
            result <- await getGrade(85)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<CharLangValue>(result);
        Assert.Equal('B', ((CharLangValue)result).Value);
    }

    [Fact]
    public void Await_MultipleParallelWaits_HandlesConcurrentExecution()
    {
        // Arrange
        var code = @"
            async func computeValue(id:int) -> string {
                await Task.Delay(60)
                return ""result-"" + id.ToStr()
            }
            task1 <- computeValue(1)
            task2 <- computeValue(2)
            task3 <- computeValue(3)
            result1 <- await task1
            result2 <- await task2
            result3 <- await task3
            combinedResult <- result1 + "", "" + result2 + "", "" + result3
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var combinedResult = interpreter.Manager.GetValue(new LangId("combinedResult"));
        Assert.NotNull(combinedResult);
        Assert.IsType<StringLangValue>(combinedResult);
        Assert.Equal("result-1, result-2, result-3", ((StringLangValue)combinedResult).Value);
    }

    [Fact]
    public void Await_WithNestedAsyncFunctions_HandlesNestedCalls()
    {
        // Arrange
        var code = @"
            async func innerAsync(value:int) -> int {
                await Task.Delay(40)
                return value * 3
            }
            async func outerAsync(base:int) -> string {
                result <- await innerAsync(base)
                await Task.Delay(30)
                return ""final: "" + result.ToStr()
            }
            result <- await outerAsync(10)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("final: 30", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Await_WithComplexReturnTypes_HandlesObjects()
    {
        // Arrange
        var code = @"
            class DataItem {
                public value:string
                public timestamp:int
                func Init(v:string) {
                    value <- v
                    timestamp <- 123456
                }
            }
            async func createDataItem(content:string) -> DataItem {
                await Task.Delay(45)
                item <- DataItem(content)
                return item
            }
            result <- await createDataItem(""test data"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<AnyLangValue>(result);
    }

    [Fact]
    public void Await_WithVariableCaptures_CapturesCorrectlyInAsync()
    {
        // Arrange
        var code = @"
            multiplier <- 5
            async func createMultiplier() -> func {
                return async (x:int) -> x * multiplier
            }
            multiply <- await createMultiplier()
            result <- multiply(8)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(40, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Await_WithRecursion_HandlesAsyncRecursion()
    {
        // Arrange
        var code = @"
            async func fibonacci(n:int) -> int {
                await Task.Delay(20)
                if n <= 1 {
                    return n
                } else {
                    a <- await fibonacci(n - 1)
                    b <- await fibonacci(n - 2)
                    return a + b
                }
            }
            result <- await fibonacci(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5, ((IntLangValue)result).Value); // fibonacci(5) = 5
    }

    [Fact]
    public void Await_WithComplexExpressions_EvaluatesAfterAwait()
    {
        // Arrange
        var code = @"
            async func getValue() -> int {
                await Task.Delay(40)
                return 100
            }
            async func getMultiplier() -> int {
                await Task.Delay(30)
                return 2
            }
            result <- (await getValue()) * (await getMultiplier()) + 50
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(250, ((IntLangValue)result).Value); // 100 * 2 + 50
    }

    [Fact]
    public void Await_WithStringOperations_ProcessesAsyncStrings()
    {
        // Arrange
        var code = @"
            async func processText(text:string) -> string {
                await Task.Delay(35)
                return text.ToUppercase() + ""-PROCESSED""
            }
            async func concatenateAsync(a:string, b:string) -> string {
                processedA <- await processText(a)
                processedB <- await processText(b)
                await Task.Delay(25)
                return processedA + "" + "" + processedB
            }
            result <- await concatenateAsync(""hello"", ""world"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("HELLO-PROCESSED WORLD-PROCESSED", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Await_WithMathematicalOperations_PerformsAsyncMath()
    {
        // Arrange
        var code = @"
            async func squareRoot(x:double) -> double {
                await Task.Delay(50)
                return x * x
            }
            async func calculateDistance(x1:double, y1:double, x2:double, y2:double) -> double {
                dxSquared <- await squareRoot(x2 - x1)
                dySquared <- await squareRoot(y2 - y1)
                await Task.Delay(30)
                return dxSquared + dySquared
            }
            result <- await calculateDistance(0.0, 0.0, 3.0, 4.0)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(25.0, ((DoubleLangValue)result).Value, 5); // 3² + 4² = 9 + 16 = 25
    }

    [Fact]
    public void Await_WithTaskChaining_ChainsMultipleAwaits()
    {
        // Arrange
        var code = @"
            async func stepA() -> string {
                await Task.Delay(30)
                return ""A""
            }
            async func stepB(input:string) -> string {
                await Task.Delay(30)
                return input + ""B""
            }
            async func stepC(input:string) -> string {
                await Task.Delay(30)
                return input + ""C""
            }
            async func pipeline() -> string {
                result1 <- await stepA()
                result2 <- await stepB(result1)
                result3 <- await stepC(result2)
                return result3
            }
            result <- await pipeline()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("ABC", ((StringLangValue)result).Value);
    }
}