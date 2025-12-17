using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Exceptions;

/// <summary>
/// Throw语句测试
/// </summary>
public class ThrowTests
{
    [Fact]
    public void Throw_SimpleStringException_ThrowsCorrectly()
    {
        // Arrange
        var code = @"
            throw ""This is an error""
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.ThrowsAny<Old8Exception>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void Throw_WithTryCatch_CatchesExceptionCorrectly()
    {
        // Arrange
        var code = @"
            try {
                throw ""Test exception""
            } catch {
                errorMessage <- ""Caught: "" + exception
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("errorMessage"));
        Assert.NotNull(result);

        // result 应该是 ErrorLangValue，我们需要获取其 FriendlyMessage
        if (result is ErrorLangValue errorValue)
        {
            Assert.Equal("Caught: Test exception", errorValue.FriendlyMessage);
        }
        else
        {
            // 如果不是 ErrorLangValue，尝试转换为字符串
            var stringResult = result as StringLangValue;
            Assert.NotNull(stringResult);
            Assert.Equal("Caught: Test exception", stringResult.Value);
        }
    }

    [Fact]
    public void Throw_WithNumberException_ThrowsIntegerException()
    {
        // Arrange
        var code = @"
            try {
                throw 404
            } catch {
                errorCode <- ""Error code: "" + exception.ToStr()
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("errorCode"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Error code: 404", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Throw_WithBooleanException_ThrowsBooleanValue()
    {
        // Arrange
        var code = @"
            try {
                throw true
            } catch {
                status <- ""Status: "" + exception.ToStr()
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("status"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Status: True", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Throw_WithDoubleException_ThrowsDoubleValue()
    {
        // Arrange
        var code = @"
            try {
                throw 3.14159
            } catch {
                value <- ""Value: "" + exception.ToStr()
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("value"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Value: 3.14159", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Throw_WithCharacterException_ThrowsCharValue()
    {
        // Arrange
        var code = @"
            try {
                throw 'X'
            } catch {
                symbol <- ""Symbol: "" + exception.ToStr()
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("symbol"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Symbol: X", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Throw_WithNullException_ThrowsNullValue()
    {
        // Arrange
        var code = @"
            try {
                throw null
            } catch {
                result <- ""Exception is null: "" + (exception == null).ToStr()
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
        Assert.Equal("Exception is null: true", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Throw_InsideFunction_PropagatesException()
    {
        // Arrange
        var code = @"
            func riskyFunction(shouldThrow:bool) -> string {
                if shouldThrow {
                    throw ""Function failed""
                }
                return ""Function succeeded""
            }
            try {
                result <- riskyFunction(true)
            } catch {
                result <- ""Caught from function: "" + exception
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
        Assert.Equal("Caught from function: Function failed", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Throw_InsideLoop_ExitsLoopAndCatchesException()
    {
        // Arrange
        var code = @"
            counter <- 0
            try {
                for i in [1~5] {
                    counter <- counter + 1
                    if i == 3 {
                        throw ""Loop interrupted at "" + i.ToStr()
                    }
                }
            } catch {
                loopResult <- ""Loop stopped at iteration "" + counter.ToStr()
                exceptionMessage <- ""Exception: "" + exception
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var loopResult = interpreter.Manager.GetValue(new LangId("loopResult"));
        var exceptionMessage = interpreter.Manager.GetValue(new LangId("exceptionMessage"));

        Assert.NotNull(loopResult);
        Assert.IsType<StringLangValue>(loopResult);
        Assert.Equal("Loop stopped at iteration 3", ((StringLangValue)loopResult).Value);

        Assert.NotNull(exceptionMessage);
        Assert.IsType<StringLangValue>(exceptionMessage);
        Assert.Equal("Exception: Loop interrupted at 3", ((StringLangValue)exceptionMessage).Value);
    }

    [Fact]
    public void Throw_InConditional_ThrowsBasedOnCondition()
    {
        // Arrange
        var code = @"
            age <- 15
            try {
                if age < 18 {
                    throw ""Access denied: Underage""
                }
                accessMessage <- ""Access granted""
            } catch {
                accessMessage <- exception
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("accessMessage"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Access denied: Underage", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Throw_NestedTryCatch_HandlesNestedExceptions()
    {
        // Arrange
        var code = @"
            try {
                try {
                    throw ""Inner exception""
                } catch {
                    innerMessage <- ""Inner caught: "" + exception
                    throw ""Outer exception""
                }
            } catch {
                outerMessage <- ""Outer caught: "" + exception
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var innerResult = interpreter.Manager.GetValue(new LangId("innerMessage"));
        var outerResult = interpreter.Manager.GetValue(new LangId("outerMessage"));

        Assert.NotNull(innerResult);
        Assert.IsType<StringLangValue>(innerResult);
        Assert.Equal("Inner caught: Inner exception", ((StringLangValue)innerResult).Value);

        Assert.NotNull(outerResult);
        Assert.IsType<StringLangValue>(outerResult);
        Assert.Equal("Outer caught: Outer exception", ((StringLangValue)outerResult).Value);
    }

    [Fact]
    public void Throw_WithArrayOperation_ThrowsInArrayContext()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            i <- 0
            try {
                for i in {0, 1, 2, 3, 4, 5} {
                    if i >= 5 {
                        throw ""Index out of bounds: "" + i.ToStr()
                    }
                    value <- numbers[i]
                }
                result <- ""All values processed""
            } catch {
                result <- exception
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);

        // result 应该是 ErrorLangValue，我们需要获取其 FriendlyMessage
        if (result is ErrorLangValue errorValue)
        {
            Assert.Equal("Index out of bounds: 5", errorValue.FriendlyMessage);
        }
        else
        {
            // 如果不是 ErrorLangValue，尝试转换为字符串
            var stringResult = result as StringLangValue;
            Assert.NotNull(stringResult);
            Assert.Equal("Index out of bounds: 5", stringResult.Value);
        }
    }

    [Fact]
    public void Throw_WithListOperation_ThrowsInListContext()
    {
        // Arrange
        var code = @"
            items <- {""apple"", ""banana"", ""cherry""}
            try {
                for item in items {
                    if item == ""banana"" {
                        throw ""Found forbidden item: "" + item
                    }
                }
                result <- ""All items OK""
            } catch {
                result <- exception
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);

        // result 应该是 ErrorLangValue，我们需要获取其 FriendlyMessage
        if (result is ErrorLangValue errorValue)
        {
            Assert.Equal("Found forbidden item: banana", errorValue.FriendlyMessage);
        }
        else
        {
            // 如果不是 ErrorLangValue，尝试转换为字符串
            var stringResult = result as StringLangValue;
            Assert.NotNull(stringResult);
            Assert.Equal("Found forbidden item: banana", stringResult.Value);
        }
    }

    [Fact]
    public void Throw_WithDictionaryOperation_ThrowsInDictionaryContext()
    {
        // Arrange
        var code = @"
            config <- {""timeout"": 30, ""retries"": 3}
            try {
                if config.ContainsKey(""timeout"") {
                    if config[""timeout""] > 60 {
                        throw ""Timeout value too high: "" + config[""timeout""].ToStr()
                    }
                }
                result <- ""Configuration is valid""
            } catch {
                result <- exception
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
        Assert.Equal("Configuration is valid", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Throw_WithObjectCreation_ThrowsDuringConstruction()
    {
        // Arrange
        var code = @"
            class UserProfile {
                public name:string
                public age:int
                func Init(userName:string, userAge:int) {
                    if userAge < 0 {
                        throw ""Invalid age: "" + userAge.ToStr()
                    }
                    name <- userName
                    age <- userAge
                }
            }
            try {
                user <- UserProfile(""Alice"", -5)
                result <- ""User created successfully""
            } catch {
                result <- ""User creation failed: "" + exception
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
        Assert.Equal("User creation failed: Invalid age: -5", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Throw_WithMethodCall_ThrowsFromMethod()
    {
        // Arrange
        var code = @"
            class Calculator {
                func Divide(a:double, b:double) -> double {
                    if b == 0.0 {
                        throw ""Division by zero""
                    }
                    return a / b
                }
            }
            calc <- Calculator()
            try {
                result <- calc.Divide(10.0, 0.0)
                message <- ""Calculation successful""
            } catch {
                message <- exception
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("message"));
        Assert.NotNull(result);

        // result 应该是 ErrorLangValue，我们需要获取其 FriendlyMessage
        if (result is ErrorLangValue errorValue)
        {
            Assert.Equal("Division by zero", errorValue.FriendlyMessage);
        }
        else
        {
            // 如果不是 ErrorLangValue，尝试转换为字符串
            var stringResult = result as StringLangValue;
            Assert.NotNull(stringResult);
            Assert.Equal("Division by zero", stringResult.Value);
        }
    }

    [Fact]
    public void Throw_WithComplexExpression_ThrowsCalculatedValue()
    {
        // Arrange
        var code = @"
            x <- 10
            y <- 0
            try {
                if y == 0 {
                    throw ""Cannot divide "" + x.ToStr() + "" by zero""
                }
                result <- x / y
            } catch {
                errorMessage <- exception
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("errorMessage"));
        Assert.NotNull(result);

        // result 应该是 ErrorLangValue，我们需要获取其 FriendlyMessage
        if (result is ErrorLangValue errorValue)
        {
            Assert.Equal("Cannot divide 10 by zero", errorValue.FriendlyMessage);
        }
        else
        {
            // 如果不是 ErrorLangValue，尝试转换为字符串
            var stringResult = result as StringLangValue;
            Assert.NotNull(stringResult);
            Assert.Equal("Cannot divide 10 by zero", stringResult.Value);
        }
    }

    [Fact]
    public void Throw_WithVariableMessage_ThrowsVariableContent()
    {
        // Arrange
        var code = @"
            errorMessage <- ""Dynamic error message with value: 42""
            try {
                throw errorMessage
            } catch {
                caughtMessage <- exception
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("caughtMessage"));
        Assert.NotNull(result);

        // result 应该是 ErrorLangValue，我们需要获取其 FriendlyMessage
        if (result is ErrorLangValue errorValue)
        {
            Assert.Equal("Dynamic error message with value: 42", errorValue.FriendlyMessage);
        }
        else
        {
            // 如果不是 ErrorLangValue，尝试转换为字符串
            var stringResult = result as StringLangValue;
            Assert.NotNull(stringResult);
            Assert.Equal("Dynamic error message with value: 42", stringResult.Value);
        }
    }

    [Fact]
    public void Throw_InLambda_ThrowsFromLambda()
    {
        // Arrange
        var code = @"
            riskyOperation <- (x:int) -> {
                if x < 0 {
                    throw ""Negative value not allowed: "" + x.ToStr()
                }
                return x * 2
            }
            try {
                result <- riskyOperation(-5)
            } catch {
                result <- ""Lambda error: "" + exception
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
        Assert.Equal("Lambda error: Negative value not allowed: -5", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Throw_WithMultipleExceptionTypes_HandlesDifferentTypes()
    {
        // Arrange
        var code = @"
            func testException(type:string) {
                try {
                    if type == ""string"" {
                        throw ""String exception""
                    } else if type == ""number"" {
                        throw 500
                    } else if type == ""boolean"" {
                        throw false
                    }
                } catch {
                    result <- type + "" exception caught: "" + exception.ToStr()
                }
            }
            testException(""string"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("string exception caught: String exception", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Throw_WithWhileLoop_ExitsWhileLoopOnException()
    {
        // Arrange
        var code = @"
            counter <- 0
            try {
                while counter < 10 {
                    counter <- counter + 1
                    if counter == 5 {
                        throw ""While loop stopped at "" + counter.ToStr()
                    }
                }
            } catch {
                result <- ""Loop terminated at counter: "" + counter.ToStr()
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
        Assert.Equal("Loop terminated at counter: 5", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Throw_WithForInLoop_ExitsForInLoopOnException()
    {
        // Arrange
        var code = @"
            items <- {""first"", ""second"", ""third"", ""problematic"", ""fourth""}
            processedCount <- 0
            try {
                for item in items {
                    processedCount <- processedCount + 1
                    if item == ""problematic"" {
                        throw ""Problem encountered at: "" + item
                    }
                }
            } catch {
                result <- ""Processed "" + processedCount.ToStr() + "" items before error: "" + exception
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
        Assert.Equal("Processed 3 items before error: Problem encountered at: problematic", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Throw_WithRecursiveFunction_PropagatesThroughRecursion()
    {
        // Arrange
        var code = @"
            func recursiveFunction(depth:int, maxDepth:int) -> string {
                if depth > maxDepth {
                    throw ""Maximum depth exceeded: "" + depth.ToStr()
                }
                if depth == maxDepth {
                    return ""Reached maximum depth""
                }
                return recursiveFunction(depth + 1, maxDepth)
            }
            try {
                result <- recursiveFunction(0, 5)
            } catch {
                result <- ""Error in recursion: "" + exception
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
        Assert.Equal("Error in recursion: Maximum depth exceeded: 6", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Throw_WithSwitchStatement_HandlesExceptionsInCases()
    {
        // Arrange
        var code = @"
            operation <- ""divide""
            a <- 10
            b <- 0
            try {
                switch operation {
                    case ""add""{
                        result <- a + b
                    }
                    case ""subtract""
                        {result <- a - b}
                    case ""divide"" {
                        if b == 0 {
                            throw ""Division by zero in switch case""
                        }
                        result <- a / b
                    }
                    default
                        result <- 0
                }
            } catch {
                result <- exception
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
        Assert.Equal("Division by zero in switch case", ((StringLangValue)result).Value);
    }
}