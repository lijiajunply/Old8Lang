using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Functions;

/// <summary>
/// 闭包解释模式测试
/// </summary>
public class ClosureTests
{
    [Fact]
    public void Closure_BasicVariableCapture_CapturesOuterVariable()
    {
        // Arrange
        var code = @"
            multiplier <- 3
            func createMultiplier() -> function {
                return (x:int) -> x * multiplier
            }
            triple <- createMultiplier()
            result <- triple(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value); // 5 * 3
    }

    [Fact]
    public void Closure_MultipleVariablesCapture_CapturesMultipleOuterVariables()
    {
        // Arrange
        var code = @"
            base <- 10
            offset <- 5
            func createCalculator() -> function {
                return (x:int) -> x * base + offset
            }
            calculator <- createCalculator()
            result <- calculator(3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(35, ((IntLangValue)result).Value); // 3 * 10 + 5
    }

    [Fact]
    public void Closure_StringVariableCapture_CapturesStringVariables()
    {
        // Arrange
        var code = @"
            prefix <- ""Hello""
            func createGreeter() -> function {
                return (name:string) -> prefix + "", "" + name
            }
            greeter <- createGreeter()
            result <- greeter(""Alice"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello, Alice", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Closure_MultipleClosures_ShareAndModifyVariables()
    {
        // Arrange
        var code = @"
            counter <- 0
            func createIncrementer() -> function {
                return () -> {
                    counter <- counter + 1
                    return counter
                }
            }
            func createGetter() -> function {
                return () -> counter
            }
            incrementer <- createIncrementer()
            getter <- createGetter()

            result1 <- getter()
            result2 <- incrementer()
            result3 <- incrementer()
            result4 <- getter()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(0, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(1, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(2, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(2, ((IntLangValue)result4).Value);
    }

    [Fact]
    public void Closure_ClosureParameter_ClosureAsParameter()
    {
        // Arrange
        var code = @"
            func applyTwice(operation:func, value:int) -> int {
                return operation(operation(value))
            }
            multiplier <- 2
            doubler <- (x:int) -> x * multiplier
            result <- applyTwice(doubler, 5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(20, ((IntLangValue)result).Value); // ((5 * 2) * 2)
    }

    [Fact]
    public void Closure_ClosureReturningClosure_ClosureFactory()
    {
        // Arrange
        var code = @"
            func createPowerFunction(power:int) -> function {
                return (base:int) -> base ^ power
            }
            square <- createPowerFunction(2)
            cube <- createPowerFunction(3)

            result1 <- square(4)
            result2 <- cube(3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(16, ((IntLangValue)result1).Value); // 4^2

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(27, ((IntLangValue)result2).Value); // 3^3
    }

    [Fact]
    public void Closure_WithArrayCapture_CapturesArrayVariables()
    {
        // Arrange
        var code = @"
            factors <- [2, 3, 5]
            func createArrayMultiplier() -> function {
                return (x:int) -> {
                    result <- x
                    for factor in factors {
                        result <- result * factor
                    }
                    return result
                }
            }
            multiplier <- createArrayMultiplier()
            result <- multiplier(1)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value); // 1 * 2 * 3 * 5
    }

    [Fact]
    public void Closure_ClosureModification_ModifiesCapturedVariables()
    {
        // Arrange
        var code = @"
            accumulator <- 0
            func createAccumulator() -> function {
                return (value:int) -> {
                    accumulator <- accumulator + value
                    return accumulator
                }
            }
            acc <- createAccumulator()
            result1 <- acc(10)
            result2 <- acc(5)
            result3 <- acc(3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(10, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(15, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(18, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void Closure_NestedClosures_ClosureInsideClosure()
    {
        // Arrange
        var code = @"
            outer <- 10
            func createOuter() -> function {
                middle <- 5
                return () -> {
                    inner <- 2
                    return () -> outer + middle + inner
                }
            }
            nested <- createOuter()
            innerFunc <- nested()
            result <- innerFunc()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(17, ((IntLangValue)result).Value); // 10 + 5 + 2
    }

    [Fact]
    public void Closure_WithBooleanCapture_CapturesBooleanValues()
    {
        // Arrange
        var code = @"
            debugMode <- true
            func createLogger() -> function {
                return (message:string) -> {
                    if debugMode {
                        return ""[DEBUG] "" + message
                    } else {
                        return message
                    }
                }
            }
            logger <- createLogger()
            result <- logger(""Test message"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("[DEBUG] Test message", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Closure_WithDictionaryCapture_CapturesDictionaryVariables()
    {
        // Arrange
        var code = @"
            config <- {""prefix"": ""Mr"", ""suffix"": ""Jr""}
            func createNameFormatter() -> function {
                return (firstName:string, lastName:string) -> {
                    return config[""prefix""] + "". "" + firstName + "" "" + lastName + "" "" + config[""suffix""]
                }
            }
            formatter <- createNameFormatter()
            result <- formatter(""John"", ""Doe"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Mr. John Doe Jr", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Closure_ClosureWithLoops_ClosureInLoop()
    {
        // Arrange
        var code = @"
            multipliers <- {}
            for i <- 1, i <= 3, i++ {
                multiplier <- (x:int) -> x * i
                multipliers.Add(multiplier)
            }
            result1 <- multipliers[0](10)
            result2 <- multipliers[1](10)
            result3 <- multipliers[2](10)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(10, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(20, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(30, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void Closure_WithFunctionCallInClosure_CallsCapturedFunction()
    {
        // Arrange
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }
            func createAdder() -> function {
                return (x:int) -> add(x, 10)
            }
            adder <- createAdder()
            result <- adder(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value); // 5 + 10
    }

    [Fact]
    public void Closure_WithConditionals_UsesCapturedVariablesInConditionals()
    {
        // Arrange
        var code = @"
            threshold <- 50
            func createThresholdChecker() -> function {
                return (value:int) -> {
                    if value > threshold {
                        return ""above threshold""
                    } else if value == threshold {
                        return ""at threshold""
                    } else {
                        return ""below threshold""
                    }
                }
            }
            checker <- createThresholdChecker()
            result1 <- checker(60)
            result2 <- checker(50)
            result3 <- checker(40)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(result1);
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("above threshold", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("at threshold", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("below threshold", ((StringLangValue)result3).Value);
    }

    [Fact]
    public void Closure_WithRecursion_ClosureCallingItthis()
    {
        // Arrange
        var code = @"
            func createRecursiveCounter() -> function {
                counter <- 0
                func recursive() -> int {
                    if counter < 5 {
                        counter <- counter + 1
                        return recursive()
                    } else {
                        return counter
                    }
                }
                return recursive
            }
            recursiveCounter <- createRecursiveCounter()
            result <- recursiveCounter()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Closure_WithComplexDataStructures_CapturesComplexTypes()
    {
        // Arrange
        var code = @"
            person <- {
                ""name"": ""Alice"",
                ""age"": 25,
                ""hobbies"": {""reading"", ""coding""}
            }
            func createPersonInfo() -> function {
                return () -> {
                    return person[""name""] + "" is "" + person[""age""].ToStr() + "" years old""
                }
            }
            getInfo <- createPersonInfo()
            result <- getInfo()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Alice is 25 years old", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Closure_WithSideEffects_ModifiesExternalState()
    {
        // Arrange
        var code = @"
            logMessages <- {}
            func createLogger() -> function {
                return (message:string) -> {
                    logMessages.Push(message)
                    return ""Logged: "" + message
                }
            }
            logger <- createLogger()
            result1 <- logger(""First message"")
            result2 <- logger(""Second message"")
            logCount <- len(logMessages)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var logCount = interpreter.Manager.GetValue(new LangId("logCount"));

        Assert.NotNull(result1);
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("Logged: First message", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("Logged: Second message", ((StringLangValue)result2).Value);

        Assert.NotNull(logCount);
        Assert.IsType<IntLangValue>(logCount);
        Assert.Equal(2, ((IntLangValue)logCount).Value);
    }

    [Fact]
    public void Closure_WithMutableCapturedVariables_ModifiesCapturedVariables()
    {
        // Arrange
        var code = @"
            state <- 0
            func createStateModifier() -> tuple {
                func increment() -> int {
                    state <- state + 1
                    return state
                }
                func decrement() -> int {
                    state <- state - 1
                    return state
                }
                return (increment, decrement)
            }
            operations <- createStateModifier()
            incrementer <- operations[0]
            decrementer <- operations[1]

            result1 <- incrementer()
            result2 <- incrementer()
            result3 <- decrementer()
            result4 <- incrementer()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(1, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(2, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(1, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(2, ((IntLangValue)result4).Value);
    }

    [Fact]
    public void Closure_WithMultipleLevels_MultipleClosureLevels()
    {
        // Arrange
        var code = @"
            level1 <- 1
            func createLevel1() -> function {
                level2 <- 10
                return () -> {
                    func createLevel2() -> function {
                        level3 <- 100
                        return () -> level1 + level2 + level3
                    }
                    return createLevel2()
                }
            }
            level1Func <- createLevel1()
            level2Func <- level1Func()
            result <- level2Func()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(111, ((IntLangValue)result).Value); // 1 + 10 + 100
    }
}