using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Expressions;

/// <summary>
/// 虚拟机 Lambda 表达式测试
/// 测试 Lambda 表达式的各种用法
/// </summary>
public class VMLambdaExpressionTests
{
    [Fact]
    public void Lambda_SimpleExpression_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            add <- (a:int, b:int) -> a + b
            result <- add(3, 5)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(8, result);
    }

    [Fact]
    public void Lambda_WithBlock_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            multiply <- (a:int, b:int) -> {
                return a * b
            }
            result <- multiply(4, 6)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(24, result);
    }

    [Fact]
    public void Lambda_SingleParameter_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            square <- (x:int) -> x * x
            result <- square(7)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(49, result);
    }

    [Fact]
    public void Lambda_NoParameters_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            getConstant <- () -> 42
            result <- getConstant()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(42, result);
    }

    [Fact]
    public void Lambda_AsParameter_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func apply(x:int, f:any) -> int {
                return f(x)
            }

            double <- (n:int) -> n * 2
            result <- apply(5, double)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(10, result);
    }

    [Fact]
    public void Lambda_InlineAsParameter_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func apply(x:int, f:any) -> int {
                return f(x)
            }

            result <- apply(5, (n:int) -> n * 3)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(15, result);
    }

    [Fact]
    public void Lambda_ClosureCapture_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func makeAdder(x:int) -> any {
                return (y:int) -> x + y
            }

            add5 <- makeAdder(5)
            result1 <- add5(3)
            result2 <- add5(10)
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
        Assert.Equal(8, result1);
        Assert.Equal(15, result2);
    }

    [Fact]
    public void Lambda_WithStringType_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            greet <- (name:string) -> ""Hello, "" + name
            result <- greet(""Alice"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Hello, Alice", result);
    }

    [Fact]
    public void Lambda_WithConditional_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            isPositive <- (n:int) -> {
                if n > 0 {
                    return true
                } else {
                    return false
                }
            }

            result1 <- isPositive(5)
            result2 <- isPositive(-3)
            result3 <- isPositive(0)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.True((bool)result1);
        Assert.False((bool)result2);
        Assert.False((bool)result3);
    }

    [Fact]
    public void Lambda_NestedLambda_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func makeMultiplier(x:int) -> any {
                return (y:int) -> {
                    return (z:int) -> x * y * z
                }
            }

            mult2 <- makeMultiplier(2)
            mult2_3 <- mult2(3)
            result <- mult2_3(4)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(24, result);
    }

    [Fact]
    public void Lambda_WithListMap_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func map(list:list, f:any) -> list {
                result <- {}
                for item in list {
                    result.Add(f(item))
                }
                return result
            }

            numbers <- {1, 2, 3, 4, 5}
            doubled <- map(numbers, (n:int) -> n * 2)
            result <- doubled[2]
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(6, result);
    }

    [Fact]
    public void Lambda_WithListFilter_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func filter(list:list, predicate:any) -> list {
                result <- {}
                for item in list {
                    if predicate(item) {
                        result.Add(item)
                    }
                }
                return result
            }

            numbers <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            evens <- filter(numbers, (n:int) -> n % 2 == 0)
            result <- evens.Count()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(5, result);
    }

    [Fact]
    public void Lambda_WithListReduce_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func reduce(list:list, initial:int, f:any) -> int {
                accumulator <- initial
                for item in list {
                    accumulator <- f(accumulator, item)
                }
                return accumulator
            }

            numbers <- {1, 2, 3, 4, 5}
            sum <- reduce(numbers, 0, (acc:int, n:int) -> acc + n)
            product <- reduce(numbers, 1, (acc:int, n:int) -> acc * n)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var sum = vm.GetGlobalVariable("sum");
        var product = vm.GetGlobalVariable("product");
        Assert.NotNull(sum);
        Assert.NotNull(product);
        Assert.Equal(15, sum);
        Assert.Equal(120, product);
    }

    [Fact]
    public void Lambda_MultipleClosureCaptures_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func makeCalculator(a:int, b:int) -> any {
                return (op:string) -> {
                    if op == ""add"" {
                        return a + b
                    } elif op == ""multiply"" {
                        return a * b
                    } else {
                        return 0
                    }
                }
            }

            calc <- makeCalculator(10, 5)
            result1 <- calc(""add"")
            result2 <- calc(""multiply"")
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
        Assert.Equal(50, result2);
    }

    [Fact]
    public void Lambda_AsReturnValue_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func getOperation(op:string) -> any {
                if op == ""double"" {
                    return (x:int) -> x * 2
                } elif op == ""triple"" {
                    return (x:int) -> x * 3
                } else {
                    return (x:int) -> x
                }
            }

            doubler <- getOperation(""double"")
            tripler <- getOperation(""triple"")
            result1 <- doubler(5)
            result2 <- tripler(5)
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
        Assert.Equal(10, result1);
        Assert.Equal(15, result2);
    }
}
