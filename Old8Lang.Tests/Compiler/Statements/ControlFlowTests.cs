using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Statements;

/// <summary>
/// 控制流编译模式测试
/// 测试编译器模式下的条件语句、循环语句和复杂控制流的 IL 生成和执行
/// </summary>
[Collection("Sequential")]
public class ControlFlowTests
{
    #region If 语句测试

    [Fact]
    public void ControlFlow_SimpleIf_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            result <- 0
            if x > 5 {
                result <- 100
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ControlFlow_IfElse_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 3
            result <- """"
            if x > 5 {
                result <- ""greater""
            } else {
                result <- ""less or equal""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ControlFlow_IfElifElse_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            score <- 75
            grade <- """"
            if score >= 90 {
                grade <- ""A""
            } elif score >= 80 {
                grade <- ""B""
            } elif score >= 70 {
                grade <- ""C""
            } else {
                grade <- ""F""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ControlFlow_NestedIf_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 15
            y <- 20
            result <- """"
            if x > 10 {
                if y > 15 {
                    result <- ""both greater""
                } else {
                    result <- ""x greater only""
                }
            } else {
                result <- ""x not greater""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region For 循环测试

    [Fact]
    public void ControlFlow_SimpleForLoop_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i <- 1, i <= 5, i++ {
                sum <- sum + i
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ControlFlow_ForLoopWithStep_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i <- 0, i <= 10, i <- i + 2 {
                sum <- sum + i
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ControlFlow_NestedForLoop_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 0
            for i <- 1, i <= 3, i++ {
                for j <- 1, j <= 3, j++ {
                    result <- result + (i * j)
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region While 循环测试

    [Fact]
    public void ControlFlow_SimpleWhileLoop_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            count <- 0
            i <- 1
            while i <= 5 {
                count <- count + i
                i <- i + 1
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ControlFlow_WhileLoopWithCondition_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            current <- 1
            while current < 100 {
                sum <- sum + current
                current <- current * 2
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ControlFlow_NestedWhileLoop_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 0
            i <- 1
            while i <= 3 {
                j <- 1
                while j <= 3 {
                    result <- result + 1
                    j <- j + 1
                }
                i <- i + 1
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region Switch 语句测试

    [Fact]
    public void ControlFlow_SimpleSwitch_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            day <- 3
            dayName <- """"
            switch day {
                case 1 {
                    dayName <- ""Monday""
                }
                case 2 {
                    dayName <- ""Tuesday""
                }
                case 3 {
                    dayName <- ""Wednesday""
                }
                default {
                    dayName <- ""Unknown""
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ControlFlow_SwitchWithDefault_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            value <- 99
            result <- """"
            switch value {
                case 1 {
                    result <- ""one""
                }
                case 2 {
                    result <- ""two""
                }
                default {
                    result <- ""other""
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region Break 和 Continue 测试

    [Fact]
    public void ControlFlow_ForLoopWithBreak_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i <- 1, i <= 10, i++ {
                if i > 5 {
                    break
                }
                sum <- sum + i
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ControlFlow_ForLoopWithContinue_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i <- 1, i <= 10, i++ {
                if i % 2 == 0 {
                    continue
                }
                sum <- sum + i
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ControlFlow_WhileLoopWithBreak_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            counter <- 0
            i <- 1
            while true {
                if i > 5 {
                    break
                }
                counter <- counter + i
                i <- i + 1
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 复杂控制流组合测试

    [Fact]
    public void ControlFlow_IfForCombination_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i <- 1, i <= 10, i++ {
                if i % 2 == 0 {
                    sum <- sum + i
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ControlFlow_WhileIfCombination_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            counter <- 0
            sum <- 0
            while counter < 50 {
                counter <- counter + 1
                if counter % 5 != 0 {
                    sum <- sum + counter
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ControlFlow_NestedLoopsWithConditions_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 0
            for i <- 1, i <= 5, i++ {
                for j <- 1, j <= 5, j++ {
                    if i == j {
                        result <- result + (i * j)
                    }
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ControlFlow_ComplexFactorial_CompilesCorrectly()
    {
        // Arrange - 使用循环实现阶乘
        var code = @"
            n <- 5
            result <- 1
            i <- 1
            while i <= n {
                result <- result * i
                i <- i + 1
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion
}
