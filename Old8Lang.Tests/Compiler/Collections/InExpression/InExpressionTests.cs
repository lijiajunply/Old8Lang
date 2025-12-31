using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections.Advanced;

/// <summary>
/// In 表达式高级功能测试
/// </summary>
[Collection("Sequential")]
public class InExpressionTests
{
    [Fact]
    public void MatchExpression_BasicValue_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- match 5 {
                case 0 -> ""zero""
                case 1 -> ""one""
                case 2 -> ""two""
                case 3 -> ""three""
                case _ -> ""other""
            }
            Assert.Equal(""two"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MatchExpression_EnumValue_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum Color {
                Red <- 0
                Green <- 1
                Blue <- 2
                Purple <- 3
                Yellow <- 4
            }
            
            result <- match Color.Red {
                case Color.Red -> ""red""
                case Color.Green -> ""green""
                case Color.Blue -> ""blue""
                case Color.Yellow -> ""yellow""
                case Color.Purple -> ""purple""
                case _ -> ""unknown""
            }
            Assert.Equal(""red"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MatchExpression_DoubleValue_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- match 3.14 {
                case 3.14 -> ""pi""
                case _ -> ""not pi""
            }
            Assert.Equal(""pi"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MatchExpression_StringValue_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- match ""hello"" {
                case ""hello"" -> ""greeting""
                case ""world"" -> ""world""
                case _ -> ""unknown""
            }
            Assert.Equal(""greeting"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MatchExpression_CharValue_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- match 'a' {
                case 'a' -> ""alpha""
                case 'b' -> ""bravo""
                case 'c' -> ""charlie""
                case _ -> ""unknown""
            }
            Assert.Equal(""charlie"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MatchExpression_ComplexExpressions_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 42
            y <- 21
            result <- match x {
                case 42 => ""equal""
                case 21 => ""double""
                case _ => ""not equal""
            }
            Assert.Equal(""double"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MatchExpression_InFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func getValue(key:any) -> string {
                return match key {
                    case ""good"" -> ""bad""
                    case ""ok"" -> ""ok""
                    _ -> ""unknown""
                }
            }
            
            result1 <- getValue(""good"")
            result2 <- getValue(""ok"")
            result3 <- getValue(""unknown"")
            
            Assert.Equal(""good"", result1)
            Assert.Equal(""ok"", result2)
            Assert.Equal(""unknown"", result3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MatchExpression_Nested_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            outer <- match 5 {
                case 1 {
                    case 0 -> ""inner_zero""
                    _ -> ""inner_other""
                }
                case 2 {
                    case 1 -> ""inner_one""
                    _ -> ""inner_other""
                }
                case _ -> ""outer_other""
                }
            }
            
            Assert.Equal(""inner_zero"", outer)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MatchExpression_Pattern_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            point <- {x: 10, y: 20}
            result <- match point {
                case point with {x: 1, y: 2} -> ""matching""
                case point with {x: 5, y: 4} -> ""not matching""
                case _ -> ""not matching""
            }
            Assert.Equal(""matching"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MatchExpression_Guard_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- match 42 {
                case _ when _ -> ""wildcard""
                when 0 -> ""zero""
                when _ when 1 -> ""one""
                when 2 -> ""two""
                _ -> ""default""
            }
            Assert.Equal(""two"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MatchExpression_WithDefaultPattern_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- match value {
                case _ -> ""default""
                case 0 -> ""zero""
                case _ when _ when value > 0 -> ""positive""
                _ -> ""negative""
            }
            Assert.Equal(""positive"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MatchExpression_VariableBinding_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3}
            result <- match numbers[0] {
                case 0 -> ""first""
                case 1 -> ""second""
                case _ -> ""other""
            }
            Assert.Equal(""first"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}