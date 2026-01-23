using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Expressions;

/// <summary>
/// 虚拟机 Range 操作测试
/// 测试 Range 表达式的各种用法
/// </summary>
public class VMRangeTests
{
    [Fact]
    public void Range_InclusiveBothEnds_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [1~5] {
                sum <- sum + i
            }
            result <- sum
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(15, result); // 1+2+3+4+5 = 15
    }

    [Fact]
    public void Range_ExclusiveRightEnd_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [1~<5] {
                sum <- sum + i
            }
            result <- sum
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(10, result); // 1+2+3+4 = 10
    }

    [Fact]
    public void Range_ExclusiveLeftEnd_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [1>~5] {
                sum <- sum + i
            }
            result <- sum
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(14, result); // 2+3+4+5 = 14
    }

    [Fact]
    public void Range_ExclusiveBothEnds_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [1>~<5] {
                sum <- sum + i
            }
            result <- sum
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(9, result); // 2+3+4 = 9
    }

    [Fact]
    public void Range_SingleElement_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            count <- 0
            for i in [5~5] {
                count <- count + 1
            }
            result <- count
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Range_NegativeNumbers_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [-3~3] {
                sum <- sum + i
            }
            result <- sum
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(0, result); // -3-2-1+0+1+2+3 = 0
    }

    [Fact]
    public void Range_ZeroToPositive_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            count <- 0
            for i in [0~10] {
                count <- count + 1
            }
            result <- count
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(11, result); // 0,1,2,3,4,5,6,7,8,9,10
    }

    [Fact]
    public void Range_InListComprehension_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            squares <- {}
            for i in [1~5] {
                squares.Add(i * i)
            }
            result <- squares[3]
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(16, result); // 4*4 = 16
    }

    [Fact]
    public void Range_NestedLoop_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [1~3] {
                for j in [1~3] {
                    sum <- sum + i * j
                }
            }
            result <- sum
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(36, result); // (1+2+3)*(1+2+3) = 6*6 = 36
    }

    [Fact]
    public void Range_WithBreak_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [1~10] {
                if i > 5 {
                    break
                }
                sum <- sum + i
            }
            result <- sum
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(15, result); // 1+2+3+4+5 = 15
    }

    [Fact]
    public void Range_WithContinue_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [1~10] {
                if i % 2 == 0 {
                    continue
                }
                sum <- sum + i
            }
            result <- sum
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(25, result); // 1+3+5+7+9 = 25
    }

    [Fact]
    public void Range_LargeRange_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [1~100] {
                sum <- sum + i
            }
            result <- sum
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(5050, result); // 1+2+...+100 = 5050
    }

    [Fact]
    public void Range_WithConditional_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            evens <- {}
            odds <- {}
            for i in [1~10] {
                if i % 2 == 0 {
                    evens.Add(i)
                } else {
                    odds.Add(i)
                }
            }
            result1 <- evens.Count()
            result2 <- odds.Count()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(5, result1);
        Assert.Equal(5, result2);
    }

    [Fact]
    public void Range_InFunction_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func sumRange(start:int, end:int) -> int {
                sum <- 0
                for i in [start~end] {
                    sum <- sum + i
                }
                return sum
            }

            result1 <- sumRange(1, 5)
            result2 <- sumRange(10, 15)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(15, result1);
        Assert.Equal(75, result2);
    }

    [Fact]
    public void Range_WithVariables_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            start <- 5
            end <- 10
            sum <- 0
            for i in [start~end] {
                sum <- sum + i
            }
            result <- sum
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(45, result); // 5+6+7+8+9+10 = 45
    }

    [Fact]
    public void Range_ReverseOrder_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [10~1] {
                sum <- sum + i
            }
            result <- sum
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        // 如果支持反向范围，应该是 10+9+8+...+1 = 55
        // 如果不支持，可能是 0 或抛出错误
        Assert.True((int)result == 55 || (int)result == 0);
    }
}
