using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Expressions;

/// <summary>
/// 字符串模板编译模式测试
/// </summary>
[Collection("Sequential")]
public class StringTemplateTests
{
    [Fact]
    public void SimpleStringTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            name <- ""World""
            result <- ""Hello, {{name}}!""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello, World!", ((StringLangValue)result).Value);
    }

    [Fact]
    public void MultiplePlaceholders_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            first <- ""John""
            last <- ""Doe""
            age <- 30
            result <- ""{{first}} {{last}} is {{age}} years old""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("John Doe is 30 years old", ((StringLangValue)result).Value);
    }

    [Fact]
    public void NumericPlaceholders_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            y <- 20
            result <- ""Sum: {{x}}, Product: {{y}}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Sum: 10, Product: 20", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ExpressionInTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 5
            b <- 3
            result <- ""{{a + b}} is the sum""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("8 is the sum", ((StringLangValue)result).Value);
    }

    [Fact]
    public void NestedStringTemplates_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            outer <- ""Hello""
            inner <- ""World""
            result <- ""{{outer}} {{outer}}! {{inner}} {{inner}}!""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello Hello! World World!", ((StringLangValue)result).Value);
    }

    [Fact]
    public void BooleanInTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            isAdmin <- true
            name <- ""Alice""
            result <- ""{{isAdmin ? ""Admin"" : ""User""}}: {{name}}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Admin: Alice", ((StringLangValue)result).Value);
    }

    [Fact]
    public void FunctionCallInTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func greet(name:string):string {
                return ""Hello, "" + name
            }
            
            username <- ""Bob""
            result <- ""{{greet(username)}}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello, Bob", ((StringLangValue)result).Value);
    }

    [Fact]
    public void EmptyTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- ""No placeholders here""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("No placeholders here", ((StringLangValue)result).Value);
    }

    [Fact]
    public void SpecialCharactersInTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            symbol <- ""@#$%^""
            result <- ""Symbol: {{symbol}}!""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Symbol: @#$%^!", ((StringLangValue)result).Value);
    }

    [Fact]
    public void UnicodeInTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            chinese <- ""你好""
            english <- ""World""
            result <- ""{{chinese}} {{english}}!""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("你好 World!", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ComplexTemplateWithMath_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            y <- 20
            z <- 30
            avg <- (x + y + z) / 3
            result <- ""Values: {{x}}, {{y}}, {{z}}. Average: {{avg}}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Values: 10, 20, 30. Average: 20.0", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TemplateInConditional_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            score <- 85
            grade <- ""C""
            result <- score > 90 ? ""A"" : (score > 80 ? ""B"" : ""C"")
            message <- ""Grade: {{result}}. Score: {{score}}""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var message = interpreter.Manager.GetValue(new LangId("message"));
        Assert.NotNull(message);
        Assert.IsType<StringLangValue>(message);
        Assert.Equal("Grade: B. Score: 85", ((StringLangValue)message).Value);
    }

    [Fact]
    public void RepeatedPlaceholders_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            name <- ""Alice""
            result <- ""Hello {{name}}, {{name}}, {{name}}!""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello Alice, Alice, Alice!", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TemplateEscaping_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            value <- 42
            result <- ""Value is {{value}} and this shows placeholder syntax""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Value is 42 and this shows placeholder syntax", ((StringLangValue)result).Value);
    }

    [Fact]
    public void EmptyVariableValueInTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            name <- """"
            result <- ""Hello, {{name}}!""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello, !", ((StringLangValue)result).Value);
    }

    [Fact]
    public void VeryLongTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            line1 <- ""This is line 1""
            line2 <- ""This is line 2""
            line3 <- ""This is line 3""
            result <- ""{{line1}}
{{line2}}
{{line3}}
This is a very long template string with multiple lines and placeholders.
It should work correctly when compiled and executed.""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        var resultValue = ((StringLangValue)result).Value;
        Assert.True(resultValue.StartsWith("This is line 1"));
        Assert.True(resultValue.Contains("This is line 2"));
        Assert.True(resultValue.Contains("This is line 3"));
        Assert.True(resultValue.Contains("This is a very long template string"));
    }
}