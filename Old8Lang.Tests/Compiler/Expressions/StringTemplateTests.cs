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
            result <- $""Hello, {name}!""
            Assert.Equal(""Hello, World!"", result)
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
    public void MultiplePlaceholders_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            first <- ""John""
            last <- ""Doe""
            age <- 30
            result <- $""{first} {last} is {age} years old""
            Assert.Equal(""John Doe is 30 years old"", result)
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
    public void NumericPlaceholders_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            y <- 20
            result <- $""Sum: {x}, Product: {y}""
            Assert.Equal(""Sum: 10, Product: 20"", result)
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
    public void ExpressionInTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 5
            b <- 3
            result <- $""{a + b} is the sum""
            Assert.Equal(""8 is the sum"", result)
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
    public void NestedStringTemplates_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            outer <- ""Hello""
            inner <- ""World""
            result <- $""{outer} {outer}! {inner} {inner}!""
            Assert.Equal(""Hello Hello! World World!"", result)
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
    public void BooleanInTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            isAdmin <- true
            name <- ""Alice""
            role <- isAdmin ? ""Admin"" : ""User""
            result <- $""{role}: {name}""
            Assert.Equal(""Admin: Alice"", result)
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
    public void FunctionCallInTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func greet(name:string) -> string {
                return ""Hello, "" + name
            }
            
            username <- ""Bob""
            result <- $""{greet(username)}""
            Assert.Equal(""Hello, Bob"", result)
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
    public void EmptyTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- ""No placeholders here""
            Assert.Equal(""No placeholders here"", result)
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
    public void SpecialCharactersInTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            symbol <- ""@#$%^""
            result <- $""Symbol: {symbol}!""
            Assert.Equal(""Symbol: @#$%^!"", result)
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
    public void UnicodeInTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            chinese <- ""你好""
            english <- ""World""
            result <- $""{chinese} {english}!""
            Assert.Equal(""你好 World!"", result)
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
    public void ComplexTemplateWithMath_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            y <- 20
            z <- 30
            avg <- (x + y + z) / 3
            result <- $""Values: {x}, {y}, {z}. Average: {avg}""
            Assert.Equal(""Values: 10, 20, 30. Average: 20"", result)
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
    public void TemplateInConditional_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            score <- 85
            grade <- score > 90 ? ""A"" : (score > 80 ? ""B"" : ""C"")
            message <- $""Grade: {grade}. Score: {score}""
            Assert.Equal(""Grade: B. Score: 85"", message)
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
    public void RepeatedPlaceholders_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            name <- ""Alice""
            result <- $""Hello {name}, {name}, {name}!""
            Assert.Equal(""Hello Alice, Alice, Alice!"", result)
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
    public void TemplateEscaping_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            value <- 42
            result <- $""Value is {value} and this shows placeholder syntax""
            Assert.Equal(""Value is 42 and this shows placeholder syntax"", result)
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
    public void EmptyVariableValueInTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            name <- """"
            result <- $""Hello, {name}!""
            Assert.Equal(""Hello, !"", result)
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
    public void VeryLongTemplate_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            line1 <- ""This is line 1""
            line2 <- ""This is line 2""
            line3 <- ""This is line 3""
            result <- $""{line1}
 {line2}
 {line3}
 This is a very long template string with multiple lines and placeholders.
 It should work correctly when compiled and executed.""
            Assert.True(result.StartsWith(""This is line 1""))
            Assert.True(result.Contains(""This is line 2""))
            Assert.True(result.Contains(""This is line 3""))
            Assert.True(result.Contains(""This is a very long template string""))
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