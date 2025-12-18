using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Expressions;

/// <summary>
/// 字符串模板解释模式测试
/// </summary>
public class StringTemplateTests
{
    [Fact]
    public void StringTemplate_SimpleVariable_InterpolatesCorrectly()
    {
        // Arrange
        var code = @"
            name <- ""Alice""
            result <- $""Hello, {name}!""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello, Alice!", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_MultipleVariables_InterpolatesCorrectly()
    {
        // Arrange
        var code = @"
            name <- ""Bob""
            age <- 25
            result <- $""{name} is {age} years old""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Bob is 25 years old", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_WithExpression_EvaluatesAndInterpolates()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            result <- $""The sum of {a} and {b} is {a + b}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("The sum of 10 and 20 is 30", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_WithFunctionCall_InterpolatesResult()
    {
        // Arrange
        var code = @"
            func getName() {
                return ""Charlie""
            }
            func getAge() {
                return 30
            }
            result <- $""{getName()} is {getAge()} years old""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Charlie is 30 years old", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_EscapedBrackets_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            name <- ""David""
            result <- $""Hello {{name}}, your score is {len(name.ToStr())}!""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello {name}, your score is 5!", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_NestedTemplates_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            outer <- ""World""
            inner <- $""Hello, {outer}""
            result <- $""Message: {inner}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Message: Hello, World", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_WithBooleanValue_ConvertsToString()
    {
        // Arrange
        var code = @"
            isReady <- true
            result <- $""Status: {isReady}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Status: true", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_WithArrayAccess_InterpolatesElement()
    {
        // Arrange
        var code = @"
            colors <- [""red"", ""green"", ""blue""]
            result <- $""The first color is {colors[0]} and the second is {colors[1]}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("The first color is red and the second is green", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_WithDictionaryAccess_InterpolatesValue()
    {
        // Arrange
        var code = """
                   person <- {"name": "Eve", "age": 28}
                   a <- person["name"]
                   b <- person["age"]
                   result <- $"{a} is {b} years old"
                   """;
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Eve is 28 years old", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_ComplexExpression_EvaluatesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 5
            y <- 3
            result <- $""{x} squared is {x * x}, and {y} cubed is {y * y * y}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("5 squared is 25, and 3 cubed is 27", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_WithLambda_EvaluatesLambda()
    {
        // Arrange
        var code = @"
            calculate <- (a, b) -> a + b
            result <- $""The result is {calculate(10, 20)}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("The result is 30", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_WithCharacterValue_ConvertsToString()
    {
        // Arrange
        var code = @"
            grade <- 'A'
            result <- $""Your grade is {grade}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Your grade is A", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_WithDoubleValue_IncludesDecimalPoint()
    {
        // Arrange
        var code = @"
            pi <- 3.14159
            result <- $""The value of pi is approximately {pi}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("The value of pi is approximately 3.14159", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_MultipleLines_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            line1 <- ""First line""
            line2 <- ""Second line""
            line3 <- ""Third line""
            result <- $""{line1}\n{line2}\n{line3}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("First line\nSecond line\nThird line", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_WithQuotes_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            message <- ""Hello, World!""
            result <- $""She said: {message}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("She said: Hello, World!", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_WithUnicode_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            chinese <- ""你好""
            emoji <- ""👋""
            result <- $""{chinese} World {emoji}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("你好 World 👋", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_WithComplexLogic_EvaluatesCorrectly()
    {
        // Arrange
        var code = @"
            score <- 85
            result <- $""Your score is {score}, which is {if score >= 90 then 'A' else if score >= 80 then 'B' else 'C'}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Your score is 85, which is B", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_WithMathOperations_PerformsCalculations()
    {
        // Arrange
        var code = @"
            base <- 100
            taxRate <- 0.08
            tax <- base * taxRate
            total <- base + tax
            result <- $""Price: ${base}, Tax: ${tax}, Total: ${total}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Price: $100, Tax: $8, Total: $108", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_NestedInCondition_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            isAdmin <- true
            name <- ""Frank""
            greeting <- """"  // 在作用域外预定义变量
            if isAdmin {
                greeting <- $""Welcome, Administrator {name}!""
            } else {
                greeting <- $""Hello, {name}!""
            }
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
        Assert.Equal("Welcome, Administrator Frank!", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_WithMemberAccess_InterpolatesMemberValue()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""Grace""
                public age <- 32
            }
            person <- Person()
            result <- $""{person.name} is {person.age} years old""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Grace is 32 years old", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_WithComparisonOperation_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            result <- $""Is {a} greater than {b}? {a > b}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Is 10 greater than 20? false", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_VeryComplex_HandlesMultipleOperations()
    {
        // Arrange
        var code = @"
            products <- [
                {""name"": ""Book"", ""price"": 29.99},
                {""name"": ""Pen"", ""price"": 1.99},
                {""name"": ""Notebook"", ""price"": 4.99}
            ]
            total <- 0
            count <- 0
            for product in products {
                total <- total + product[""price""]
                count <- count + 1
            }
            average <- total / count
            result <- $""You bought {count} items for ${total}, average price: ${average}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("You bought 3 items for $36.97, average price: $12.323333333333332", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringTemplate_SimpleTemplate_WorksCorrectly()
    {
        // Arrange
        var code = @"
            result <- $""Hello, World!""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello, World!", ((StringLangValue)result).Value);
    }
}