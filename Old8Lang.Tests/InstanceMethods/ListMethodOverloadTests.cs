using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;
using Xunit;

namespace Old8Lang.Tests.InstanceMethods;

/// <summary>
/// List 方法重载测试
/// </summary>
public class ListMethodOverloadTests
{
    [Fact]
    public void ListFirst_NoParameter_ShouldReturnFirstElement()
    {
        // Arrange
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result <- list.First()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ListFirst_WithPredicate_ShouldReturnFirstMatchingElement()
    {
        // Arrange
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result <- list.First((x:int) -> x > 2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ListFirst_OverloadResolution_ShouldSelectCorrectMethod()
    {
        // Arrange - 测试重载解析是否正确选择方法
        var code = @"
            list <- {10, 20, 30, 40, 50}
            result1 <- list.First()                    // 无参数版本
            result2 <- list.First((x:int) -> x > 25)   // 带谓词版本
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(10, ((IntLangValue)result1).Value);
        Assert.Equal(30, ((IntLangValue)result2).Value);
    }

    [Fact]
    public void ListLast_NoParameter_ShouldReturnLastElement()
    {
        // Arrange
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result <- list.Last()
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
    public void ListLast_WithPredicate_ShouldReturnLastMatchingElement()
    {
        // Arrange
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result <- list.Last((x:int) -> x < 4)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ListLast_OverloadResolution_ShouldSelectCorrectMethod()
    {
        // Arrange - 测试重载解析是否正确选择方法
        var code = @"
            list <- {10, 20, 30, 40, 50}
            result1 <- list.Last()                     // 无参数版本
            result2 <- list.Last((x:int) -> x < 35)    // 带谓词版本
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(50, ((IntLangValue)result1).Value);
        Assert.Equal(30, ((IntLangValue)result2).Value);
    }

    [Fact]
    public void ListAggregate_WithoutSeed_ShouldAggregateFromFirstElement()
    {
        // Arrange
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result <- list.Aggregate((acc:int, x:int) -> acc + x)
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
    public void ListAggregate_WithSeed_ShouldAggregateFromSeed()
    {
        // Arrange
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result <- list.Aggregate((acc:int, x:int) -> acc + x, 10)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(25, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ListAggregate_OverloadResolution_ShouldSelectCorrectMethod()
    {
        // Arrange - 测试重载解析是否正确选择方法
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result1 <- list.Aggregate((acc:int, x:int) -> acc * x)        // 无初始值版本
            result2 <- list.Aggregate((acc:int, x:int) -> acc * x, 2)     // 带初始值版本
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(120, ((IntLangValue)result1).Value);  // 1*2*3*4*5 = 120
        Assert.Equal(240, ((IntLangValue)result2).Value);  // 2*1*2*3*4*5 = 240
    }

    [Fact]
    public void ListSum_NoParameter_ShouldSumAllElements()
    {
        // Arrange
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result <- list.Sum()
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
    public void ListSum_WithSelector_ShouldSumSelectedValues()
    {
        // Arrange
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result <- list.Sum((x:int) -> x * 2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value);  // (1+2+3+4+5)*2 = 30
    }

    [Fact]
    public void ListSum_OverloadResolution_ShouldSelectCorrectMethod()
    {
        // Arrange - 测试重载解析是否正确选择方法
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result1 <- list.Sum()                      // 无参数版本
            result2 <- list.Sum((x:int) -> x * x)      // 带选择器版本
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(15, ((IntLangValue)result1).Value);   // 1+2+3+4+5 = 15
        Assert.Equal(55, ((IntLangValue)result2).Value);   // 1+4+9+16+25 = 55
    }

    [Fact]
    public void ListMin_NoParameter_ShouldReturnMinValue()
    {
        // Arrange
        var code = @"
            list <- {5, 2, 8, 1, 9}
            result <- list.Min()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ListMin_WithSelector_ShouldReturnMinSelectedValue()
    {
        // Arrange
        var code = @"
            list <- {-5, -2, -8, -1, -9}
            result <- list.Min((x:int) -> x * x)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value);  // min of (25, 4, 64, 1, 81) = 1
    }

    [Fact]
    public void ListMin_OverloadResolution_ShouldSelectCorrectMethod()
    {
        // Arrange - 测试重载解析是否正确选择方法
        var code = @"
            list <- {5, 2, 8, 1, 9}
            result1 <- list.Min()                      // 无参数版本
            result2 <- list.Min((x:int) -> -x)         // 带选择器版本（取负数的最小值）
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(1, ((IntLangValue)result1).Value);    // min(5,2,8,1,9) = 1
        Assert.Equal(-9, ((IntLangValue)result2).Value);   // min(-5,-2,-8,-1,-9) = -9
    }

    [Fact]
    public void ListMax_NoParameter_ShouldReturnMaxValue()
    {
        // Arrange
        var code = @"
            list <- {5, 2, 8, 1, 9}
            result <- list.Max()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(9, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ListMax_WithSelector_ShouldReturnMaxSelectedValue()
    {
        // Arrange
        var code = @"
            list <- {-5, -2, -8, -1, -9}
            result <- list.Max((x:int) -> x * x)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(81, ((IntLangValue)result).Value);  // max of (25, 4, 64, 1, 81) = 81
    }

    [Fact]
    public void ListMax_OverloadResolution_ShouldSelectCorrectMethod()
    {
        // Arrange - 测试重载解析是否正确选择方法
        var code = @"
            list <- {5, 2, 8, 1, 9}
            result1 <- list.Max()                      // 无参数版本
            result2 <- list.Max((x:int) -> -x)         // 带选择器版本（取负数的最大值）
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(9, ((IntLangValue)result1).Value);    // max(5,2,8,1,9) = 9
        Assert.Equal(-1, ((IntLangValue)result2).Value);   // max(-5,-2,-8,-1,-9) = -1
    }
}
