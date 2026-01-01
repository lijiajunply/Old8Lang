using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Functions;

/// <summary>
/// 命名参数语法解析测试
/// </summary>
[Collection("Sequential")]
public class NamedArgumentsParserTests
{
    #region 基础语法解析测试

    [Fact]
    public void ParseProgram_NamedArguments_Basic_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            func greet(name:string, age:int, message:string) -> void {
                PrintLine(message + "", "" + name + ""! Age: "" + age.ToStr())
            }
            greet(name: ""Alice"", age: 25, message: ""Hello"")
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert - 应该成功解析，不抛出异常
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_NamedArguments_MixedWithPositional_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            func test(a:int, b:int, c:int) -> int {
                return a + b + c
            }
            result <- test(1, b: 2, c: 3)
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_NamedArguments_OutOfOrder_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            func calculate(x:int, y:int, operation:string) -> int {
                if operation == ""add"" {
                    return x + y
                }
                return x * y
            }
            result <- calculate(operation: ""mul"", y: 3, x: 7)
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_NamedArguments_WithDefaultParameters_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            func display(title:string, width: 800, height: 600) -> void {
                PrintLine(title)
            }
            display(title: ""Window"", height: 1080)
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_NamedArguments_ComplexExpressions_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            func test(a:int, b:int, c:int) -> int {
                return a + b + c
            }
            x <- 10
            y <- 20
            result <- test(a: x + 5, b: y * 2, c: 100)
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_NamedArguments_NestedFunctionCalls_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }
            func multiply(x:int, y:int) -> int {
                return x * y
            }
            result <- add(a: multiply(x: 2, y: 3), b: 5)
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误语法测试

    [Fact]
    public void ParseProgram_PositionalAfterNamed_ThrowsError()
    {
        // Arrange
        var code = @"
            func test(a:int, b:int, c:int) -> int {
                return a + b + c
            }
            result <- test(a: 1, 2, 3)
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var exception = Assert.Throws<SyntaxError>(() => parser.ParseProgram());
        Assert.Contains("位置参数必须出现在所有命名参数之前", exception.Message);
    }

    [Fact]
    public void ParseProgram_NamedArguments_InvalidSyntax_ThrowsError()
    {
        // Arrange
        var code = @"
            func test(a:int, b:int) -> int {
                return a + b
            }
            result <- test(a: , b: 2)
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.Throws<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 边界测试

    [Fact]
    public void ParseProgram_NamedArguments_SingleParameter_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            func single(x:int) -> int {
                return x * 2
            }
            result <- single(x: 5)
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_NamedArguments_ManyParameters_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            func many(a:int, b:int, c:int, d:int, e:int) -> int {
                return a + b + c + d + e
            }
            result <- many(e: 5, d: 4, c: 3, b: 2, a: 1)
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_NamedArguments_WithStringLiterals_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            func format(template:string, name:string, value:int) -> string {
                return template + name + value.ToStr()
            }
            result <- format(name: ""Test"", value: 42, template: ""Result: "")
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_NamedArguments_WithBooleanLiterals_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            func configure(enabled:bool, verbose:bool) -> void {
                PrintLine(enabled.ToStr())
            }
            configure(verbose: true, enabled: false)
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region Lambda 和方法调用测试

    [Fact]
    public void ParseProgram_NamedArguments_LambdaCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            add <- (x:int, y:int) -> x + y
            result <- add(y: 5, x: 3)
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_NamedArguments_MethodChain_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            class Calculator {
                func calculate(x:int, y:int, op:string) -> int {
                    return x + y
                }
            }
            calc <- Calculator()
            result <- calc.calculate(op: ""add"", x: 10, y: 20)
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion
}
