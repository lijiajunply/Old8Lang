using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Functions;

/// <summary>
/// Lambda表达式解释模式测试
/// </summary>
public class LambdaTests
{
    [Fact]
    public void Lambda_SimpleAddition_WorksCorrectly()
    {
        // Arrange
        var code = @"
            add <- (x, y) -> x + y
            result <- add(5, 3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(8, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Lambda_WithBlockBody_WorksCorrectly()
    {
        // Arrange
        var code = @"
            calculate <- (x, y) -> {
                temp <- x * 2
                return temp + y
            }
            result <- calculate(5, 3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(13, ((IntLangValue)result).Value); // (5 * 2) + 3 = 10 + 3 = 13
    }

    [Fact]
    public void Lambda_NoParameters_WorksCorrectly()
    {
        // Arrange
        var code = @"
            getConstant <- () -> 42
            result <- getConstant()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Lambda_SingleParameter_WorksCorrectly()
    {
        // Arrange
        var code = @"
            square <- (x) -> x * x
            result <- square(7)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(49, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Lambda_WithExplicitTypeAnnotations_WorksCorrectly()
    {
        // Arrange
        var code = @"
            multiply <- (x:int, y:int) -> int => x * y
            result <- multiply(6, 7)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Lambda_ReturningString_WorksCorrectly()
    {
        // Arrange
        var code = @"
            greet <- (name:string) -> ""Hello, "" + name
            result <- greet(""Alice"")
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
    public void Lambda_ClosureOverVariables_CapturesCorrectly()
    {
        // Arrange
        var code = @"
            multiplier <- 3
            createMultiplier <- (factor) -> (x) -> x * factor
            triple <- createMultiplier(multiplier)
            result <- triple(10)
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
    public void Lambda_HigherOrderFunction_WorksCorrectly()
    {
        // Arrange
        var code = @"
            applyOperation <- (x, y, operation) -> operation(x, y)
            add <- (a, b) -> a + b
            result <- applyOperation(15, 25, add)
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
    public void Lambda_InlineLambda_WorksCorrectly()
    {
        // Arrange
        var code = @"
            result <- ((x, y) -> x + y)(10, 20)
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
    public void Lambda_WithConditionalLogic_WorksCorrectly()
    {
        // Arrange
        var code = @"
            absoluteValue <- (x) -> if x >= 0 then x else -x
            result1 <- absoluteValue(10)
            result2 <- absoluteValue(-10)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as IntLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as IntLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(10, result1.Value);
        Assert.Equal(10, result2.Value);
    }

    [Fact]
    public void Lambda_WithArrayOperations_WorksCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            sum <- 0
            forEach <- (arr, action) -> {
                for i <- 0, i < 5, i++ {
                    action(arr[i])
                }
            }
            addToSum <- (x) ->  sum + x
            forEach(numbers, addToSum)
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
        Assert.Equal(15, ((IntLangValue)result).Value); // 1 + 2 + 3 + 4 + 5 = 15
    }

    [Fact]
    public void Lambda_RecursiveLambda_WorksCorrectly()
    {
        // Arrange
        var code = @"
            factorial <- null
            factorial <- (n) ->  n <= 1 ? 1 : n * factorial(n - 1)
            result <- factorial(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(120, ((IntLangValue)result).Value); // 5! = 120
    }

    [Fact]
    public void Lambda_WithComparisonOperations_WorksCorrectly()
    {
        // Arrange
        var code = @"
            isGreaterThan <- (threshold) -> (value) -> value > threshold
            isGreaterThan10 <- isGreaterThan(10)
            result1 <- isGreaterThan10(15)
            result2 <- isGreaterThan10(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as BoolLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as BoolLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(true, result1.Value);  // 15 > 10
        Assert.Equal(false, result2.Value); // 5 > 10
    }

    [Fact]
    public void Lambda_WithMultipleReturnPaths_WorksCorrectly()
    {
        // Arrange
        var code = @"
            categorize <- (score) -> {
                if score >= 90 {
                    return ""A""
                }
                if score >= 80 {
                    return ""B""
                }
                if score >= 70 {
                    return ""C""
                }
                return ""F""
            }
            result1 <- categorize(95)
            result2 <- categorize(85)
            result3 <- categorize(65)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as StringLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as StringLangValue;
        var result3 = interpreter.Manager.GetValue(new LangId("result3")) as StringLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.Equal("A", result1.Value);
        Assert.Equal("B", result2.Value);
        Assert.Equal("F", result3.Value);
    }

    [Fact]
    public void Lambda_ClosureWithLoop_CapturesLoopVariableCorrectly()
    {
        // Arrange
        var code = @"
            functions <- {}
            for i <- 0, i < 3, i++ {
                // 创建捕获循环变量的 lambda
                createFunc <- (index) -> () -> index
                functions.Add(createFunc(i))
            }
            result1 <- functions[0]()
            result2 <- functions[1]()
            result3 <- functions[2]()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as IntLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as IntLangValue;
        var result3 = interpreter.Manager.GetValue(new LangId("result3")) as IntLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.Equal(0, result1.Value);
        Assert.Equal(1, result2.Value);
        Assert.Equal(2, result3.Value);
    }

    [Fact]
    public void Lambda_WithBooleanOperations_WorksCorrectly()
    {
        // Arrange
        var code = @"
            andOperation <- (a, b) -> a and b
            orOperation <- (a, b) -> a or b
            notOperation <- (a) -> not a
            result1 <- andOperation(true, false)
            result2 <- orOperation(true, false)
            result3 <- notOperation(true)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as BoolLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as BoolLangValue;
        var result3 = interpreter.Manager.GetValue(new LangId("result3")) as BoolLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.Equal(false, result1.Value); // true and false = false
        Assert.Equal(true, result2.Value);  // true or false = true
        Assert.Equal(false, result3.Value); // not true = false
    }

    [Fact]
    public void Lambda_WithMixedTypeOperations_WorksCorrectly()
    {
        // Arrange
        var code = @"
            formatNumber <- (num, prefix:string, suffix:string) -> prefix + num + suffix
            result <- formatNumber(42, ""Number: "", ""!"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Number: 42!", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Lambda_ChainedOperations_WorksCorrectly()
    {
        // Arrange
        var code = @"
            compose <- (f, g) -> (x) -> f(g(x))
            double <- (x) -> x * 2
            addTen <- (x) -> x + 10
            combined <- compose(double, addTen)
            result <- combined(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value); // double(addTen(5)) = double(15) = 30
    }

    [Fact]
    public void Lambda_ImmediateExecution_WorksCorrectly()
    {
        // Arrange
        var code = @"
            result <- ((x) -> x * x + 2 * x + 1)(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(36, ((IntLangValue)result).Value); // 5*5 + 2*5 + 1 = 25 + 10 + 1 = 36
    }

    [Fact]
    public void Lambda_EmptyLambda_WorksCorrectly()
    {
        // Arrange
        var code = @"
            doNothing <- () -> void
            counter <- 0
            doNothing()
            result <- counter
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(0, ((IntLangValue)result).Value);
    }
}