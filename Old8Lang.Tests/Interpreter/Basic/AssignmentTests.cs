using Old8Lang.AST.Expression;
using Xunit;
using Xunit.Abstractions;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;

namespace Old8Lang.Tests.Interpreter.Basic;

/// <summary>
/// 基础赋值语句解释模式测试
/// </summary>
public class AssignmentTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper Output = output;

    [Fact]
    public void SimpleVariableAssignment_CreatesCorrectValue()
    {
        // Arrange
        var code = "a <- 42";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("a"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(123456789)]
    [InlineData(-987654321)]
    public void IntegerAssignment_EdgeCases_CreateCorrectValues(int value)
    {
        // Arrange
        var code = $"a <- {value}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("a"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(value, ((IntLangValue)result).Value);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(3.14159)]
    [InlineData(-2.71828)]
    [InlineData(1e10)]
    [InlineData(-1e-10)]
    public void DoubleAssignment_EdgeCases_CreateCorrectValues(double value)
    {
        // Arrange
        var code = $"a <- {value}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("a"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(value, ((DoubleLangValue)result).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("Hello, World!")]
    [InlineData("中文测试")]
    [InlineData("Special chars: !@#$%^&*()")]
    public void StringAssignment_VariousStrings_CreateCorrectValues(string str)
    {
        // Arrange
        // 转义双引号用于字符串字面量
        var escapedStr = str.Replace("\"", "\\\"");
        var code = $"a <- \"{escapedStr}\"";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("a"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal(str, ((StringLangValue)result).Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BooleanAssignment_CreatesCorrectValues(bool value)
    {
        // Arrange
        var code = $"a <- {value.ToString().ToLower()}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("a"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(value, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData('a')]
    [InlineData('Z')]
    [InlineData('0')]
    [InlineData('@')]
    public void CharAssignment_CreatesCorrectValues(char value)
    {
        // Arrange
        var code = $"a <- '{value}'";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("a"));
        Assert.NotNull(result);
        Assert.IsType<CharLangValue>(result);
        Assert.Equal(value, ((CharLangValue)result).Value);
    }

    [Fact]
    public void VariableReassignment_UpdatesValue()
    {
        // Arrange
        var code = @"
            a <- 10
            a <- 20
            a <- 30
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("a"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value);
    }

    [Fact]
    public void MultipleVariableAssignments_CreatesAllValues()
    {
        // Arrange
        var code = @"
            a <- 1
            b <- 2.5
            c <- ""hello""
            d <- true
            e <- 'x'
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var a = interpreter.Manager.GetValue(new LangId("a")) as IntLangValue;
        var b = interpreter.Manager.GetValue(new LangId("b")) as DoubleLangValue;
        var c = interpreter.Manager.GetValue(new LangId("c")) as StringLangValue;
        var d = interpreter.Manager.GetValue(new LangId("d")) as BoolLangValue;
        var e = interpreter.Manager.GetValue(new LangId("e")) as CharLangValue;

        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.NotNull(c);
        Assert.NotNull(d);
        Assert.NotNull(e);

        Assert.Equal(1, a.Value);
        Assert.Equal(2.5, b.Value);
        Assert.Equal("hello", c.Value);
        Assert.True(d.Value);
        Assert.Equal('x', e.Value);
    }

    [Fact]
    public void AssignmentWithExpression_CalculatesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            c <- a + b * 2
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("c"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(50, ((IntLangValue)result).Value); // 10 + 20 * 2 = 10 + 40 = 50
    }

    [Fact]
    public void AssignmentWithFunctionCall_CreatesCorrectValue()
    {
        // Arrange
        var code = @"
            func getValue() {
                return 42
            }
            result <- getValue()
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
    public void AssignmentWithIdentifierConflict_UsesLatestAssignment()
    {
        // Arrange
        var code = @"
            a <- 1
            b <- 2
            a <- b + 10
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var a = interpreter.Manager.GetValue(new LangId("a")) as IntLangValue;
        var b = interpreter.Manager.GetValue(new LangId("b")) as IntLangValue;

        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.Equal(12, a.Value); // a = b + 10 = 2 + 10 = 12
        Assert.Equal(2, b.Value);
    }

    [Fact]
    public void EmptyAssignment_OnlyParsesNoExecution()
    {
        // Arrange
        var code = "";
        var interpreter = new LangInterpreter();

        // Act & Assert - 应该能够解析空代码而不抛出异常
        var ast = interpreter.Build(code);
        Assert.NotNull(ast);
        Assert.Equal(0, ast.Count);

        // 执行也不应该抛出异常
        ast.Run(interpreter.Manager);
    }

    [Fact]
    public void AssignmentWithUnicode_HandlesUnicodeCorrectly()
    {
        // Arrange
        var code = @"
            中文变量名 <- 100
            another <- 中文变量名 + 50
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var var1 = interpreter.Manager.GetValue(new LangId("中文变量名")) as IntLangValue;
        var var2 = interpreter.Manager.GetValue(new LangId("another")) as IntLangValue;

        Assert.NotNull(var1);
        Assert.NotNull(var2);
        Assert.Equal(100, var1.Value);
        Assert.Equal(150, var2.Value);
    }
}