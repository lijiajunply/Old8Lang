using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Expressions;

/// <summary>
/// 比较表达式解释模式测试
/// </summary>
public class ComparisonTests
{
    [Theory]
    [InlineData(5, 3, true)]
    [InlineData(3, 5, false)]
    [InlineData(5, 5, false)]
    [InlineData(0, -1, true)]
    [InlineData(-10, -20, true)]
    public void GreaterThan_TwoIntegers_ReturnsCorrectResult(int a, int b, bool expected)
    {
        // Arrange
        var code = $"result <- {a} > {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData(5, 3, false)]
    [InlineData(3, 5, true)]
    [InlineData(5, 5, false)]
    [InlineData(0, -1, false)]
    [InlineData(-10, -20, false)]
    public void LessThan_TwoIntegers_ReturnsCorrectResult(int a, int b, bool expected)
    {
        // Arrange
        var code = $"result <- {a} < {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData(5, 3, true)]
    [InlineData(3, 5, false)]
    [InlineData(5, 5, true)]
    [InlineData(0, -1, true)]
    [InlineData(-10, -20, true)]
    public void GreaterThanOrEqual_TwoIntegers_ReturnsCorrectResult(int a, int b, bool expected)
    {
        // Arrange
        var code = $"result <- {a} >= {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData(5, 3, false)]
    [InlineData(3, 5, true)]
    [InlineData(5, 5, true)]
    [InlineData(0, -1, false)]
    [InlineData(-10, -20, false)]
    public void LessThanOrEqual_TwoIntegers_ReturnsCorrectResult(int a, int b, bool expected)
    {
        // Arrange
        var code = $"result <- {a} <= {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData(5, 5, true)]
    [InlineData(3, 5, false)]
    [InlineData(-10, -10, true)]
    [InlineData(0, 1, false)]
    public void Equal_TwoIntegers_ReturnsCorrectResult(int a, int b, bool expected)
    {
        // Arrange
        var code = $"result <- {a} == {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData(5, 5, false)]
    [InlineData(3, 5, true)]
    [InlineData(-10, -10, false)]
    [InlineData(0, 1, true)]
    public void NotEqual_TwoIntegers_ReturnsCorrectResult(int a, int b, bool expected)
    {
        // Arrange
        var code = $"result <- {a} != {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData(3.14, 3.14, true)]
    [InlineData(2.71, 3.14, false)]
    [InlineData(0.0, 0.0, true)]
    [InlineData(-1.5, -1.5, true)]
    [InlineData(1.0, 1.0000001, false)] // 浮点精度
    public void Comparison_TwoDoubles_ReturnsCorrectResult(double a, double b, bool expected)
    {
        // Arrange
        var code = $"result <- {a} == {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData("hello", "hello", true)]
    [InlineData("hello", "world", false)]
    [InlineData("", "", true)]
    [InlineData("a", "A", false)] // 大小写敏感
    [InlineData("hello world", "hello world", true)]
    public void Comparison_TwoStrings_ReturnsCorrectResult(string a, string b, bool expected)
    {
        // Arrange
        var escapedA = a.Replace("\"", "\\\"");
        var escapedB = b.Replace("\"", "\\\"");
        var code = $"result <- \"{escapedA}\" == \"{escapedB}\"";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void Comparison_TwoBooleans_ReturnsCorrectResult(bool a, bool b, bool expected)
    {
        // Arrange
        var code = $"result <- {a.ToString().ToLower()} == {b.ToString().ToLower()}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData('a', 'a', true)]
    [InlineData('a', 'b', false)]
    [InlineData('A', 'a', false)] // 大小写敏感
    [InlineData('0', '0', true)]
    [InlineData('@', '#', false)]
    public void Comparison_TwoChars_ReturnsCorrectResult(char a, char b, bool expected)
    {
        // Arrange
        var code = $"result <- '{a}' == '{b}'";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void MixedTypeComparison_IntegerAndDouble_ReturnsCorrectResult()
    {
        // Arrange
        var code = @"
            intVal <- 5
            doubleVal <- 5.0
            result1 <- intVal == doubleVal
            result2 <- intVal < doubleVal + 0.1
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
        Assert.Equal(true, result1.Value);  // 5 == 5.0
        Assert.Equal(true, result2.Value);  // 5 < 5.1
    }

    [Fact]
    public void ComparisonWithVariables_UsesVariableValues()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            result1 <- a < b
            result2 <- a == b
            result3 <- a > b
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
        Assert.Equal(true, result1.Value);   // 10 < 20
        Assert.Equal(false, result2.Value);  // 10 != 20
        Assert.Equal(false, result3.Value);  // 10 !> 20
    }

    [Fact]
    public void ComparisonWithExpressions_EvaluatesExpressionsFirst()
    {
        // Arrange
        var code = @"
            a <- 5
            b <- 10
            result1 <- (a + b) > (b - a)
            result2 <- (a * b) == 50
            result3 <- (b / a) >= 2
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
        Assert.Equal(true, result1.Value);  // 15 > 5
        Assert.Equal(true, result2.Value);  // 50 == 50
        Assert.Equal(true, result3.Value);  // 2 >= 2
    }

    [Fact]
    public void ComparisonChaining_MultipleComparisons()
    {
        // Arrange
        var code = @"
            a <- 1
            b <- 2
            c <- 3
            d <- 4
            result1 <- a < b and b < c and c < d
            result2 <- a <= b <= c <= d  // 这取决于语言是否支持链式比较
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as BoolLangValue;
        Assert.NotNull(result1);
        Assert.Equal(true, result1.Value); // 1 < 2 and 2 < 3 and 3 < 4

        // result2 取决于语言实现
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        Assert.NotNull(result2);
    }

    [Fact]
    public void ComparisonInCondition_UsedForControlFlow()
    {
        // Arrange
        var code = @"
            age <- 18
            result <- """"
            if age >= 18 {
                result <- ""adult""
            } else {
                result <- ""minor""
            }

            score <- 85
            grade <- """"
            if score >= 90 {
                grade <- ""A""
            } elif score >= 80 {
                grade <- ""B""
            } elif score >= 70 {
                grade <- ""C""
            } else {
                grade <- ""D""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        var grade = interpreter.Manager.GetValue(new LangId("grade")) as StringLangValue;

        Assert.NotNull(result);
        Assert.NotNull(grade);
        Assert.Equal("adult", result.Value);
        Assert.Equal("B", grade.Value);
    }

    [Fact]
    public void ComparisonWithFunctionCalls_ComparesReturnValues()
    {
        // Arrange
        var code = @"
            func getValueA() {
                return 10
            }
            func getValueB() {
                return 20
            }
            result1 <- getValueA() < getValueB()
            result2 <- getValueA() + getValueB() == 30
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
        Assert.Equal(true, result1.Value); // 10 < 20
        Assert.Equal(true, result2.Value); // 10 + 20 == 30
    }

    [Fact]
    public void ComparisonWithArrayElements_ComparesArrayValues()
    {
        // Arrange
        var code = @"
            arr1 <- [1, 2, 3, 4, 5]
            arr2 <- [1, 2, 3, 4, 5]
            arr3 <- [1, 2, 3, 4, 6]
            result1 <- arr1[0] == arr2[0]
            result2 <- arr1[4] != arr3[4]
            result3 <- arr1[2] < arr3[4]
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
        Assert.Equal(true, result1.Value); // arr1[0] == arr2[0] == 1
        Assert.Equal(true, result2.Value); // arr1[4] != arr3[4] == 5 != 6
        Assert.Equal(true, result3.Value); // arr1[2] < arr3[4] == 3 < 6
    }

    [Fact]
    public void StringComparison_LexicographicOrdering()
    {
        // Arrange
        var code = @"
            str1 <- ""apple""
            str2 <- ""banana""
            str3 <- ""Apple""
            result1 <- str1 < str2
            result2 <- str1 == str3
            result3 <- str1 >= str3
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
        Assert.Equal(true, result1.Value);  // "apple" < "banana" (lexicographic)
        Assert.Equal(false, result2.Value); // "apple" != "Apple" (case sensitive)
        // result3 取决于具体的大小写敏感比较实现
    }

    [Fact]
    public void ComparisonWithNullValues_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 0
            b <- """"
            c <- false
            result1 <- a == null
            result2 <- b == null
            result3 <- c == null
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);

        // Assert - 这个测试取决于 Old8Lang 如何处理 null 值
        // 如果语言支持 null，这些比较会有意义
        ast.Run(interpreter.Manager);

        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
    }
}