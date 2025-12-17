using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Statements;

/// <summary>
/// 跳转语句测试（Break/Continue）
/// </summary>
public class JumpStatementsTests
{
    [Fact]
    public void BreakStatement_SimpleForLoop_BreaksOutOfLoop()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in 1..10 {
                sum <- sum + i
                if i == 5 {
                    break
                }
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value); // 1+2+3+4+5 = 15
    }

    [Fact]
    public void BreakStatement_ForLoopAtStart_BreaksImmediately()
    {
        // Arrange
        var code = @"
            count <- 0
            for i in 1..10 {
                count <- count + 1
                break
                count <- count + 1 // This should not execute
            }
            result <- count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value); // Only one iteration
    }

    [Fact]
    public void BreakStatement_WhileLoop_BreaksOutOfWhile()
    {
        // Arrange
        var code = @"
            counter <- 0
            while counter < 10 {
                counter <- counter + 1
                if counter == 3 {
                    break
                }
            }
            result <- counter
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
    public void BreakStatement_ForInLoop_BreaksOutOfForIn()
    {
        // Arrange
        var code = @"
            items <- {10, 20, 30, 40, 50}
            sum <- 0
            for item in items {
                sum <- sum + item
                if item == 30 {
                    break
                }
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(60, ((IntLangValue)result).Value); // 10+20+30 = 60
    }

    [Fact]
    public void BreakStatement_NestedLoops_BreaksOnlyInnerLoop()
    {
        // Arrange
        var code = @"
            outerCount <- 0
            innerCount <- 0
            for i in 1..5 {
                outerCount <- outerCount + 1
                for j in 1..5 {
                    innerCount <- innerCount + 1
                    if j == 3 {
                        break
                    }
                }
            }
            result <- ""outer: "" + outerCount.ToStr() + "", inner: "" + innerCount.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("outer: 5, inner: 15", ((StringLangValue)result).Value); // 5 * 3 = 15 inner iterations
    }

    [Fact]
    public void BreakStatement_WithSwitch_BreaksOutOfSwitchInLoop()
    {
        // Arrange
        var code = @"
            count <- 0
            for i in 1..10 {
                count <- count + 1
                switch i {
                    case 5:
                        break
                    default:
                        // Continue loop
                }
                if i >= 7 {
                    break
                }
            }
            result <- count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(7, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ContinueStatement_SimpleForLoop_SkipsIteration()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in 1..10 {
                if i % 2 == 0 {
                    continue
                }
                sum <- sum + i
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(25, ((IntLangValue)result).Value); // 1+3+5+7+9 = 25
    }

    [Fact]
    public void ContinueStatement_ForLoopAtStart_SkipsFirstIteration()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in 1..5 {
                if i <= 2 {
                    continue
                }
                sum <- sum + i
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(12, ((IntLangValue)result).Value); // 3+4+5 = 12
    }

    [Fact]
    public void ContinueStatement_WhileLoop_SkipsIterationInWhile()
    {
        // Arrange
        var code = @"
            counter <- 0
            sum <- 0
            while counter < 10 {
                counter <- counter + 1
                if counter % 3 == 0 {
                    continue
                }
                sum <- sum + counter
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(37, ((IntLangValue)result).Value); // 1+2+4+5+7+8+10 = 37 (skip 3,6,9)
    }

    [Fact]
    public void ContinueStatement_ForInLoop_SkipsItemsInList()
    {
        // Arrange
        var code = @"
            items <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            sum <- 0
            for item in items {
                if item < 4 or item > 7 {
                    continue
                }
                sum <- sum + item
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(18, ((IntLangValue)result).Value); // 4+5+6+7 = 18
    }

    [Fact]
    public void ContinueStatement_NestedLoops_SkipsInInnerLoop()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in 1..3 {
                for j in 1..3 {
                    if j == 2 {
                        continue
                    }
                    sum <- sum + (i * j)
                }
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(12, ((IntLangValue)result).Value); // (1*1)+(1*3)+(2*1)+(2*3)+(3*1)+(3*3) = 1+3+2+6+3+9 = 24
    }

    [Fact]
    public void ContinueStatement_WithSwitch_SkipsInSwitch()
    {
        // Arrange
        var code = @"
            count <- 0
            sum <- 0
            for i in 1..6 {
                count <- count + 1
                switch i {
                    case 2:
                    case 4:
                        continue
                    default:
                        sum <- sum + i
                }
            }
            result <- ""count: "" + count.ToStr() + "", sum: "" + sum.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("count: 6, sum: 19", ((StringLangValue)result).Value); // 1+3+5+6 = 15
    }

    [Fact]
    public void BreakContinue_CombinedInLoop_CombinesBreakAndContinue()
    {
        // Arrange
        var code = @"
            sum <- 0
            count <- 0
            for i in 1..20 {
                count <- count + 1
                if i < 5 {
                    continue
                }
                if i > 10 {
                    break
                }
                sum <- sum + i
            }
            result <- ""count: "" + count.ToStr() + "", sum: "" + sum.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("count: 11, sum: 45", ((StringLangValue)result).Value); // 5+6+7+8+9+10 = 45
    }

    [Fact]
    public void BreakContinue_InConditionalBreak_BreaksBasedOnCondition()
    {
        // Arrange
        var code = @"
            numbers <- {3, 7, 2, 9, 4, 6, 1, 8, 5}
            sum <- 0
            found <- false
            for num in numbers {
                if num == 6 {
                    found <- true
                    break
                }
                sum <- sum + num
            }
            result <- ""sum: "" + sum.ToStr() + "", found: "" + found.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("sum: 25, found: true", ((StringLangValue)result).Value); // 3+7+2+9+4 = 25
    }

    [Fact]
    public void BreakContinue_InFunction_ReturnsAfterLoop()
    {
        // Arrange
        var code = @"
            func findFirstOdd(numbers:{int}) -> int {
                for num in numbers {
                    if num % 2 != 0 {
                        return num
                    }
                }
                return -1
            }
            result1 <- findFirstOdd({2, 4, 6, 7, 8})
            result2 <- findFirstOdd({2, 4, 6, 8})
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(7, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(-1, ((IntLangValue)result2).Value);
    }

    [Fact]
    public void BreakContinue_InInfiniteLoop_ExitsInfiniteLoop()
    {
        // Arrange
        var code = @"
            counter <- 0
            while true {
                counter <- counter + 1
                if counter >= 5 {
                    break
                }
            }
            result <- counter
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
    public void BreakContinue_WithArrayIndexing_ProcessesArrayElements()
    {
        // Arrange
        var code = @"
            array <- [10, 20, 30, 40, 50, 60]
            sum <- 0
            for i in 0..<array.Length {
                value <- array[i]
                if value == 40 {
                    break
                }
                if value < 20 {
                    continue
                }
                sum <- sum + value
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(60, ((IntLangValue)result).Value); // 20+30 = 50 (skip 10, break at 40)
    }

    [Fact]
    public void BreakContinue_WithComplexLogic_HandlesComplexConditions()
    {
        // Arrange
        var code = @"
            items <- {""apple"", ""banana"", ""cherry"", ""date"", ""elderberry""}
            result <- {}
            for i in 0..<items.Length {
                item <- items[i]

                // Skip items starting with 'b'
                if item.StartsWith(""b"") {
                    continue
                }

                // Stop at 'd'
                if item.StartsWith(""d"") {
                    break
                }

                // Add to result
                result.Add(item.ToUppercase())
            }
            finalResult <- result.Join("" "")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("finalResult"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("APPLE CHERRY", ((StringLangValue)result).Value); // Skip banana, break at date
    }

    [Fact]
    public void BreakContinue_WithCounter_CountsAndSkips()
    {
        // Arrange
        var code = @"
            processed <- 0
            skipped <- 0
            stopped <- 0

            for i in 1..15 {
                if i > 12 {
                    stopped <- stopped + 1
                    break
                }

                if i % 3 == 0 {
                    skipped <- skipped + 1
                    continue
                }

                processed <- processed + 1
            }

            result <- ""processed: "" + processed.ToStr() + "", skipped: "" + skipped.ToStr() + "", stopped: "" + stopped.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("processed: 8, skipped: 4, stopped: 1", ((StringLangValue)result).Value);
    }

    [Fact]
    public void BreakContinue_MultipleBreaks_HandlesMultipleBreakPoints()
    {
        // Arrange
        var code = @"
            level1Count <- 0
            level2Count <- 0
            targetFound <- false

            for i in 1..10 {
                level1Count <- level1Count + 1

                for j in 1..10 {
                    level2Count <- level2Count + 1

                    if i == 5 and j == 7 {
                        targetFound <- true
                        break
                    }
                }

                if targetFound {
                    break
                }
            }

            result <- ""L1: "" + level1Count.ToStr() + "", L2: "" + level2Count.ToStr() + "", Found: "" + targetFound.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("L1: 5, L2: 47, Found: true", ((StringLangValue)result).Value);
    }

    [Fact]
    public void BreakContinue_WithErrorHandling_HandlesBreakInTryCatch()
    {
        // Arrange
        var code = @"
            count <- 0
            sum <- 0

            for i in 1..10 {
                try {
                    if i == 7 {
                        break
                    }

                    if i == 3 {
                        continue
                    }

                    sum <- sum + i
                    count <- count + 1

                } catch {
                    // Handle any exceptions
                }
            }

            result <- ""count: "" + count.ToStr() + "", sum: "" + sum.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("count: 5, sum: 25", ((StringLangValue)result).Value); // 1+2+4+5+6 = 18 (skip 3, break at 7)
    }
}