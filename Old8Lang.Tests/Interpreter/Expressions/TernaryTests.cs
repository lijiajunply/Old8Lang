using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Expressions;

/// <summary>
/// 三元表达式解释模式测试
/// </summary>
public class TernaryTests
{
    [Fact]
    public void Ternary_SimpleCondition_ReturnsCorrectValue()
    {
        // Arrange
        var code = @"
            x <- 10
            result <- if x > 5 then ""greater"" else ""less""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("greater", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Ternary_FalseCondition_ReturnsElseValue()
    {
        // Arrange
        var code = @"
            x <- 3
            result <- if x > 5 then ""greater"" else ""less""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("less", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Ternary_WithNumbers_ReturnsCorrectNumber()
    {
        // Arrange
        var code = @"
            a <- 15
            b <- 10
            result <- if a > b then a else b
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Ternary_WithBooleanValues_ReturnsBoolean()
    {
        // Arrange
        var code = @"
            isRaining <- true
            result <- if isRaining then true else false
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    [Fact]
    public void Ternary_WithDoubleValues_ReturnsCorrectDouble()
    {
        // Arrange
        var code = @"
            price <- 100.50
            discount <- 0.1
            result <- if price > 100 then price * (1 - discount) else price
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(90.45, ((DoubleLangValue)result).Value, 5);
    }

    [Fact]
    public void Ternary_WithCharacterValues_ReturnsCorrectChar()
    {
        // Arrange
        var code = @"
            grade <- 85
            result <- if grade >= 90 then 'A' else 'B'
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
    public void Ternary_WithComplexCondition_EvaluatesCondition()
    {
        // Arrange
        var code = @"
            age <- 25
            hasLicense <- true
            result <- if age >= 18 and hasLicense then ""can drive"" else ""cannot drive""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("can drive", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Ternary_NestedTernary_HandlesNestedConditions()
    {
        // Arrange
        var code = @"
            score <- 75
            result <- if score >= 90 then 'A' else if score >= 80 then 'B' else if score >= 70 then 'C' else 'F'
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<CharLangValue>(result);
        Assert.Equal('C', ((CharLangValue)result).Value);
    }

    [Fact]
    public void Ternary_WithFunctionCalls_EvaluatesFunctions()
    {
        // Arrange
        var code = @"
            func isEven(n:int) -> bool {
                return n % 2 == 0
            }
            func getEvenMessage() -> string {
                return ""number is even""
            }
            func getOddMessage() -> string {
                return ""number is odd""
            }
            number <- 8
            result <- if isEven(number) then getEvenMessage() else getOddMessage()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("number is even", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Ternary_WithArrayAccess_ConditionsOnArrays()
    {
        // Arrange
        var code = @"
            numbers <- [5, 10, 15, 20]
            result <- if numbers[2] > 12 then numbers[2] else numbers[0]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Ternary_WithListOperations_ConditionsOnLists()
    {
        // Arrange
        var code = @"
            items <- {1, 2, 3, 4, 5}
            result <- if items.Contains(3) then ""found"" else ""not found""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("found", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Ternary_WithDictionaryOperations_ConditionsOnDictionaries()
    {
        // Arrange
        var code = """
                   config <- {"theme": "dark", "notifications": true}
                   result <- if config.ContainsKey("theme") then config["theme"] else "light"
                   """;
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("dark", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Ternary_WithVariableAssignment_UsedInAssignment()
    {
        // Arrange
        var code = @"
            x <- 20
            y <- 30
            max <- if x > y then x else y
            result <- max
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
    public void Ternary_WithStringComparison_ComparesStrings()
    {
        // Arrange
        var code = @"
            name <- ""Alice""
            result <- if name == ""Alice"" then ""welcome Alice"" else ""welcome guest""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("welcome Alice", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Ternary_WithArithmeticExpressions_ComplexMath()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 5
            c <- 3
            result <- if (a + b) * c > 40 then (a + b) * c else a * b + c
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(45, ((IntLangValue)result).Value); // (10+5)*3 = 45 > 40, so return 45
    }

    [Fact]
    public void Ternary_WithLogicalOperations_CombinedConditions()
    {
        // Arrange
        var code = @"
            age <- 25
            student <- true
            employed <- false
            result <- if age < 30 and (student or employed) then ""eligible"" else ""not eligible""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("eligible", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Ternary_WithNotOperator_NegatedCondition()
    {
        // Arrange
        var code = @"
            isDisabled <- false
            result <- if not isDisabled then ""enabled"" else ""disabled""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("enabled", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Ternary_WithNullChecks_NullHandling()
    {
        // Arrange
        var code = @"
            optionalValue <- null
            result <- if optionalValue != null then optionalValue else ""default value""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("default value", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Ternary_InVariableDeclaration_UsedInVarDeclaration()
    {
        // Arrange
        var code = @"
            hour <- 14
            greeting <- if hour < 12 then ""Good morning"" else if hour < 18 then ""Good afternoon"" else ""Good evening""
            result <- greeting
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Good afternoon", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Ternary_WithLambdas_ReturnsLambdaFunctions()
    {
        // Arrange
        var code = @"
            shouldDouble <- true
            operation <- if shouldDouble then (x:int) -> x * 2 else (x:int) -> x + 1
            result <- operation(10)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(20, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Ternary_WithComplexExpressions_MultipleOperations()
    {
        // Arrange
        var code = @"
            basePrice <- 100
            quantity <- 5
            isPremium <- true
            discount <- if isPremium then 0.15 else 0.05
            total <- basePrice * quantity * (1 - discount)
            result <- total
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(425.0, ((DoubleLangValue)result).Value, 5); // 100 * 5 * (1 - 0.15) = 425
    }

    [Fact]
    public void Ternary_WithBooleanLiterals_DirectBooleanValues()
    {
        // Arrange
        var code = """
                   result1 <- if true then "yes" else "no"
                   result2 <- if false then "yes" else "no"
                   """;
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("yes", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("no", ((StringLangValue)result2).Value);
    }

    [Fact]
    public void Ternary_WithRangeConditions_CheckRanges()
    {
        // Arrange
        var code = @"
            score <- 85
            grade <- if score >= 90 then 'A' else if score >= 80 then 'B' else if score >= 70 then 'C' else if score >= 60 then 'D' else 'F'
            result <- grade
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
    public void Ternary_WithMethodCallConditions_CallsMethodsInCondition()
    {
        // Arrange
        var code = @"
            func isValidAge(age:int) -> bool {
                return age >= 0 and age <= 150
            }
            func getStatus(age:int) -> string {
                if isValidAge(age) {
                    return ""valid age: "" + age.ToStr()
                } else {
                    return ""invalid age""
                }
            }
            result1 <- getStatus(25)
            result2 <- getStatus(-5)
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
        Assert.Equal("valid age: 25", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("invalid age", ((StringLangValue)result2).Value);
    }

    [Fact]
    public void Ternary_InComplexLogic_MultipleTernaryExpressions()
    {
        // Arrange
        var code = @"
            x <- 10
            y <- 20
            z <- 30
            result <- if x > y and x > z then ""x is max"" else if y > x and y > z then ""y is max"" else ""z is max""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("z is max", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Ternary_WithComparisonOperators_AllComparisonTypes()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 5
            result1 <- if a > b then ""greater"" else ""not greater""
            result2 <- if a < b then ""less"" else ""not less""
            result3 <- if a >= b then ""greater or equal"" else ""not greater or equal""
            result4 <- if a <= b then ""less or equal"" else ""not less or equal""
            result5 <- if a == b then ""equal"" else ""not equal""
            result6 <- if a != b then ""not equal"" else ""equal""
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
        var result5 = interpreter.Manager.GetValue(new LangId("result5"));
        var result6 = interpreter.Manager.GetValue(new LangId("result6"));

        Assert.NotNull(result1);
        Assert.Equal("greater", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.Equal("not less", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.Equal("greater or equal", ((StringLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.Equal("not less or equal", ((StringLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.Equal("not equal", ((StringLangValue)result5).Value);

        Assert.NotNull(result6);
        Assert.Equal("not equal", ((StringLangValue)result6).Value);
    }
}