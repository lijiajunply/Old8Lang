using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Statements;

/// <summary>
/// 跳转语句编译模式测试
/// 测试编译器模式下的 break、continue、return 语句的 IL 生成和执行
/// </summary>
[Collection("Sequential")]
public class JumpTests
{
    #region Break 语句测试

    [Fact]
    public void Break_InForLoop_CompilesAndExitsLoop()
    {
        // Arrange
        var code = @"
            func testBreak() -> int {
                sum <- 0
                for i <- 0; i < 10; i <- i + 1 {
                    if i == 5 {
                        break
                    }
                    sum <- sum + i
                }
                return sum
            }

            Assert.True(testBreak() == 10) // 0+1+2+3+4
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
    public void Break_InWhileLoop_CompilesAndExitsLoop()
    {
        // Arrange
        var code = @"
            func testWhileBreak() -> int {
                count <- 0
                while true {
                    count <- count + 1
                    if count >= 5 {
                        break
                    }
                }
                return count
            }

            Assert.True(testWhileBreak() == 5)
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
    public void Break_InNestedLoop_ExitsInnerLoopOnly()
    {
        // Arrange
        var code = @"
            func testNestedBreak() -> int {
                count <- 0
                for i <- 0; i < 3; i <- i + 1 {
                    for j <- 0; j < 5; j <- j + 1 {
                        count <- count + 1
                        if j == 2 {
                            break
                        }
                    }
                }
                return count
            }

            Assert.True(testNestedBreak() == 9) // 每次内循环执行3次，共3*3=9
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
    public void Break_InForInLoop_CompilesAndExitsLoop()
    {
        // Arrange
        var code = @"
            func testForInBreak() -> int {
                list <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
                sum <- 0
                for item in list {
                    if item > 5 {
                        break
                    }
                    sum <- sum + item
                }
                return sum
            }

            Assert.True(testForInBreak() == 15) // 1+2+3+4+5
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

    #region Continue 语句测试

    [Fact]
    public void Continue_InForLoop_SkipsRestOfIteration()
    {
        // Arrange
        var code = @"
            func testContinue() -> int {
                sum <- 0
                for i <- 0; i < 10; i <- i + 1 {
                    if i % 2 == 0 {
                        continue
                    }
                    sum <- sum + i
                }
                return sum
            }

            Assert.True(testContinue() == 25) // 1+3+5+7+9
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
    public void Continue_InWhileLoop_SkipsToNextIteration()
    {
        // Arrange
        var code = @"
            func testWhileContinue() -> int {
                i <- 0
                sum <- 0
                while i < 10 {
                    i <- i + 1
                    if i % 2 == 0 {
                        continue
                    }
                    sum <- sum + i
                }
                return sum
            }

            Assert.True(testWhileContinue() == 25) // 1+3+5+7+9
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
    public void Continue_InForInLoop_SkipsCurrentItem()
    {
        // Arrange
        var code = @"
            func testForInContinue() -> int {
                list <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
                sum <- 0
                for item in list {
                    if item % 2 == 0 {
                        continue
                    }
                    sum <- sum + item
                }
                return sum
            }

            Assert.True(testForInContinue() == 25) // 1+3+5+7+9
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
    public void Continue_InNestedLoop_AffectsInnerLoopOnly()
    {
        // Arrange
        var code = @"
            func testNestedContinue() -> int {
                count <- 0
                for i <- 0; i < 3; i <- i + 1 {
                    for j <- 0; j < 5; j <- j + 1 {
                        if j == 2 {
                            continue
                        }
                        count <- count + 1
                    }
                }
                return count
            }

            Assert.True(testNestedContinue() == 12) // 每次内循环执行4次，共3*4=12
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

    #region Return 语句测试

    [Fact]
    public void Return_InFunction_ReturnsValue()
    {
        // Arrange
        var code = @"
            func getValue() -> int {
                return 42
            }

            Assert.True(getValue() == 42)
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
    public void Return_EarlyReturn_ExitsFunctionImmediately()
    {
        // Arrange
        var code = @"
            func earlyReturn(x:int) -> string {
                if x < 0 {
                    return ""negative""
                }
                if x == 0 {
                    return ""zero""
                }
                return ""positive""
            }

            Assert.True(earlyReturn(-5) == ""negative"")
            Assert.True(earlyReturn(0) == ""zero"")
            Assert.True(earlyReturn(10) == ""positive"")
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
    public void Return_InLoop_ExitsFunctionNotJustLoop()
    {
        // Arrange
        var code = @"
            func findFirst(target:int) -> int {
                for i <- 0; i < 10; i <- i + 1 {
                    if i == target {
                        return i
                    }
                }
                return -1
            }

            Assert.True(findFirst(5) == 5)
            Assert.True(findFirst(15) == -1)
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
    public void Return_WithExpression_EvaluatesAndReturns()
    {
        // Arrange
        var code = @"
            func calculate(a:int, b:int) -> int {
                return a * 2 + b * 3
            }

            Assert.True(calculate(5, 10) == 40)
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
    public void Return_VoidFunction_ReturnsNothing()
    {
        // Arrange
        var code = @"
            func voidFunc() -> void {
                x <- 10
                return
            }

            voidFunc()
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

    #region 混合测试

    [Fact]
    public void BreakAndContinue_InSameLoop_WorkCorrectly()
    {
        // Arrange
        var code = @"
            func mixed() -> int {
                sum <- 0
                for i <- 0; i < 20; i <- i + 1 {
                    if i >= 10 {
                        break
                    }
                    if i % 2 == 0 {
                        continue
                    }
                    sum <- sum + i
                }
                return sum
            }

            Assert.True(mixed() == 25) // 1+3+5+7+9
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
    public void Return_WithBreakAndContinue_ReturnTakesPrecedence()
    {
        // Arrange
        var code = @"
            func precedence(x:int) -> int {
                for i <- 0; i < 10; i <- i + 1 {
                    if i == x {
                        return i * 10
                    }
                    if i % 2 == 0 {
                        continue
                    }
                }
                return -1
            }

            Assert.True(precedence(5) == 50)
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
    public void ComplexJumps_InNestedStructures_WorkCorrectly()
    {
        // Arrange
        var code = @"
            func complex(threshold:int) -> int {
                result <- 0
                for i <- 0; i < 5; i <- i + 1 {
                    for j <- 0; j < 5; j <- j + 1 {
                        value <- i * 10 + j

                        if value == threshold {
                            return value
                        }

                        if value % 3 == 0 {
                            continue
                        }

                        if value > 30 {
                            break
                        }

                        result <- result + value
                    }
                }
                return result
            }

            Assert.True(complex(23) == 23)
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
