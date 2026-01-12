using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Statements;

/// <summary>
/// 虚拟机控制流语句测试
/// 测试虚拟机执行if语句、循环语句等控制流的正确性
/// </summary>
[Collection("Sequential")]
public class VMControlFlowTests
{
    /// <summary>
    /// 执行虚拟机代码并捕获控制台输出
    /// </summary>
    private string ExecuteVMCode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // 编译为字节码
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        // 捕获控制台输出
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // 执行字节码
            var vm = new Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #region If语句测试

    [Fact]
    public void SimpleIfStatement_True_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            if x > 5 {
                PrintLine(""条件为真"")
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("条件为真", output);
    }

    [Fact]
    public void SimpleIfStatement_False_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 3
            if x > 5 {
                PrintLine(""不应该执行"")
            }
            PrintLine(""程序结束"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("程序结束", output);
    }

    [Fact]
    public void IfElseStatement_TrueBranch_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            if x > 5 {
                PrintLine(""大于5"")
            } else {
                PrintLine(""小于等于5"")
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("大于5", output);
    }

    [Fact]
    public void IfElseStatement_FalseBranch_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 3
            if x > 5 {
                PrintLine(""大于5"")
            } else {
                PrintLine(""小于等于5"")
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("小于等于5", output);
    }

    [Fact]
    public void IfElifElseStatement_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            score <- 85
            if score >= 90 {
                PrintLine(""优秀"")
            } elif score >= 80 {
                PrintLine(""良好"")
            } elif score >= 60 {
                PrintLine(""及格"")
            } else {
                PrintLine(""不及格"")
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("良好", output);
    }

    [Fact]
    public void NestedIfStatement_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            y <- 20
            if x > 5 {
                if y > 15 {
                    PrintLine(""两个条件都满足"")
                } else {
                    PrintLine(""只有第一个条件满足"")
                }
            } else {
                PrintLine(""第一个条件不满足"")
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("两个条件都满足", output);
    }

    #endregion

    #region While循环测试

    [Fact]
    public void SimpleWhileLoop_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            i <- 1
            sum <- 0
            while i <= 3 {
                sum <- sum + i
                i <- i + 1
            }
            PrintLine(sum.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("6", output); // 1 + 2 + 3 = 6
    }

    [Fact]
    public void WhileLoopWithCondition_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            count <- 0
            i <- 1
            while i <= 5 {
                if i % 2 == 0 {
                    count <- count + 1
                }
                i <- i + 1
            }
            PrintLine(count.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("2", output); // 偶数 2, 4 共2个
    }

    [Fact]
    public void WhileLoopZeroIterations_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            i <- 10
            while i < 5 {
                PrintLine(""不应该执行"")
            }
            PrintLine(""循环结束"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("循环结束", output);
    }

    #endregion

    #region For循环测试

    [Fact]
    public void SimpleForLoop_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i <- 1, i <= 5, i++ {
                sum <- sum + i
            }
            PrintLine(sum.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("15", output); // 1 + 2 + 3 + 4 + 5 = 15
    }

    [Fact]
    public void ForLoopWithStep_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i <- 0, i <= 10, i <- i + 2 {
                sum <- sum + i
            }
            PrintLine(sum.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("30", output); // 0 + 2 + 4 + 6 + 8 + 10 = 30
    }

    [Fact]
    public void ForLoopDecrementing_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- """"
            for i <- 3, i >= 1, i-- {
                result <- result + i.ToStr()
            }
            PrintLine(result)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("321", output);
    }

    [Fact]
    public void NestedForLoop_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            count <- 0
            for i <- 1, i <= 2, i++ {
                for j <- 1, j <= 3, j++ {
                    count <- count + 1
                }
            }
            PrintLine(count.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("6", output); // 2 * 3 = 6
    }

    #endregion

    #region For-in循环测试

    [Fact]
    public void ForInArray_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            sum <- 0
            for item in arr {
                sum <- sum + item
            }
            PrintLine(sum.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("15", output); // 1 + 2 + 3 + 4 + 5 = 15
    }

    [Fact]
    public void ForInList_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            list <- {10, 20, 30}
            result <- """"
            for item in list {
                result <- result + item.ToStr() + "" ""
            }
            PrintLine(result)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("10 20 30", output);
    }

    [Fact]
    public void ForInString_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            str <- ""abc""
            result <- """"
            for char in str {
                result <- result + char.ToStr()
            }
            PrintLine(result)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("abc", output);
    }

    #endregion

    #region 三元运算符测试

    [Fact]
    public void TernaryOperator_TrueCondition_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 5
            max <- a > b ? a : b
            PrintLine(max.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("10", output);
    }

    [Fact]
    public void TernaryOperator_FalseCondition_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 3
            b <- 8
            max <- a > b ? a : b
            PrintLine(max.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("8", output);
    }

    [Fact]
    public void NestedTernaryOperator_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            score <- 85
            grade <- score >= 90 ? ""A"" : (score >= 80 ? ""B"" : ""C"")
            PrintLine(grade)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("B", output);
    }

    #endregion

    #region 复杂控制流测试

    [Fact]
    public void ComplexControlFlow_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 0
            for i <- 1, i <= 10, i++ {
                if i % 2 == 0 {
                    j <- 1
                    while j <= 2 {
                        result <- result + i
                        j <- j + 1
                    }
                } else {
                    result <- result + (i > 5 ? i : 0)
                }
            }
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        // 偶数: 2*2 + 4*2 + 6*2 + 8*2 + 10*2 = 4 + 8 + 12 + 16 + 20 = 60
        // 奇数大于5: 7 + 9 = 16
        // 总计: 60 + 16 = 76
        Assert.Equal("76", output);
    }

    [Fact]
    public void ControlFlowWithVariableScope_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            outer <- 100
            if true {
                inner <- 200
                outer <- outer + inner
            }
            PrintLine(outer.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("300", output);
    }

    #endregion
}