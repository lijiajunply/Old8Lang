using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Classes;

/// <summary>
/// 扩展方法虚拟机模式测试
/// </summary>
[Collection("Sequential")]
public class ExtensionMethodVMTests
{
    /// <summary>
    /// 测试基本扩展方法执行
    /// </summary>
    [Fact]
    public void Execute_BasicExtensionMethod_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func double() -> int {
                    return this * 2
                }
            }

            x <- 5
            result <- x.double()
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

    /// <summary>
    /// 测试扩展方法带参数
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodWithParameter_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func add(n:int) -> int {
                    return this + n
                }
            }

            x <- 5
            result <- x.add(10)
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

    /// <summary>
    /// 测试字符串扩展方法
    /// </summary>
    [Fact]
    public void Execute_StringExtensionMethod_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension string {
                func repeat(n:int) -> string {
                    result <- """"
                    i <- 0
                    while i < n {
                        result <- result + this
                        i <- i + 1
                    }
                    return result
                }
            }

            text <- ""Hello""
            result <- text.repeat(3)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("HelloHelloHello", result);
    }

    /// <summary>
    /// 测试列表扩展方法
    /// </summary>
    [Fact]
    public void Execute_ListExtensionMethod_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension list {
                func sum() -> int {
                    total <- 0
                    for item in this {
                        total <- total + item
                    }
                    return total
                }
            }

            numbers <- [1, 2, 3, 4, 5]
            result <- numbers.sum()
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

    /// <summary>
    /// 测试内置列表sum方法（验证修复）
    /// </summary>
    [Fact]
    public void Execute_BuiltInListSumMethod_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            result <- numbers.Sum()
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

    /// <summary>
    /// 测试混合类型列表求和
    /// </summary>
    [Fact]
    public void Execute_MixedTypeListSum_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2.5, 3, 4.5]
            result <- numbers.Sum()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(11.0, result);
    }

    /// <summary>
    /// 测试多个扩展方法
    /// </summary>
    [Fact]
    public void Execute_MultipleExtensionMethods_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func double() -> int {
                    return this * 2
                }

                func triple() -> int {
                    return this * 3
                }

                func isEven() -> bool {
                    return this % 2 == 0
                }
            }

            x <- 5
            doubled <- x.double()
            tripled <- x.triple()
            isEven <- x.isEven()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var doubled = vm.GetGlobalVariable("doubled");
        var tripled = vm.GetGlobalVariable("tripled");
        var isEven = vm.GetGlobalVariable("isEven");

        Assert.Equal(10, doubled);
        Assert.Equal(15, tripled);
        Assert.False((bool)isEven!);
    }

    /// <summary>
    /// 测试扩展方法链式调用
    /// </summary>
    [Fact]
    public void Execute_ChainedExtensionMethods_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func add(n:int) -> int {
                    return this + n
                }

                func multiply(n:int) -> int {
                    return this * n
                }
            }

            x <- 5
            result <- x.add(3).multiply(2)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(16, result); // (5 + 3) * 2 = 16
    }

    /// <summary>
    /// 测试扩展方法访问this
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodAccessingThis_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func square() -> int {
                    return this * this
                }

                func cube() -> int {
                    return this * this * this
                }
            }

            x <- 3
            squared <- x.square()
            cubed <- x.cube()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var squared = vm.GetGlobalVariable("squared");
        var cubed = vm.GetGlobalVariable("cubed");

        Assert.Equal(9, squared);
        Assert.Equal(27, cubed);
    }

    /// <summary>
    /// 测试多个参数的扩展方法
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodWithMultipleParameters_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func between(min:int, max:int) -> bool {
                    return this >= min and this <= max
                }
            }

            x <- 5
            result1 <- x.between(1, 10)
            result2 <- x.between(6, 10)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");

        Assert.True((bool)result1!);
        Assert.False((bool)result2!);
    }

    /// <summary>
    /// 测试扩展方法返回不同类型
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodReturningDifferentType_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func toStr() -> string {
                    return this.ToStr()
                }

                func isPositive() -> bool {
                    return this > 0
                }
            }

            x <- 42
            str <- x.toStr()
            positive <- x.isPositive()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var str = vm.GetGlobalVariable("str");
        var positive = vm.GetGlobalVariable("positive");

        Assert.Equal("42", str);
        Assert.True((bool)positive!);
    }

    /// <summary>
    /// 测试为不同类型添加同名扩展方法
    /// </summary>
    [Fact]
    public void Execute_SameMethodNameForDifferentTypes_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func getValue() -> int {
                    return this
                }
            }

            extension string {
                func getValue() -> string {
                    return this
                }
            }

            x <- 42
            s <- ""hello""
            intValue <- x.getValue()
            strValue <- s.getValue()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var intValue = vm.GetGlobalVariable("intValue");
        var strValue = vm.GetGlobalVariable("strValue");

        Assert.Equal(42, intValue);
        Assert.Equal("hello", strValue);
    }

    /// <summary>
    /// 测试扩展方法中的局部变量
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodWithLocalVariables_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func factorial() -> int {
                    result <- 1
                    i <- 1
                    while i <= this {
                        result <- result * i
                        i <- i + 1
                    }
                    return result
                }
            }

            x <- 5
            result <- x.factorial()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(120, result); // 5! = 120
    }

    /// <summary>
    /// 测试扩展方法调用其他函数
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodCallingOtherFunctions_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func helper(n:int) -> int {
                return n * 2
            }

            extension int {
                func doubleWithHelper() -> int {
                    return helper(this)
                }
            }

            x <- 5
            result <- x.doubleWithHelper()
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

    /// <summary>
    /// 测试扩展方法中的条件语句
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodWithConditionals_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func abs() -> int {
                    if this < 0 {
                        return -this
                    }
                    return this
                }
            }

            x <- -5
            y <- 10
            absX <- x.abs()
            absY <- y.abs()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var absX = vm.GetGlobalVariable("absX");
        var absY = vm.GetGlobalVariable("absY");

        Assert.Equal(5, absX);
        Assert.Equal(10, absY);
    }

    /// <summary>
    /// 测试扩展方法中的循环
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodWithLoops_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func sumUpTo() -> int {
                    sum <- 0
                    for i in [1 ~ this] {
                        sum <- sum + i
                    }
                    return sum
                }
            }

            x <- 10
            result <- x.sumUpTo()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(55, result); // 1+2+3+...+10 = 55
    }

    /// <summary>
    /// 测试扩展方法与内置方法共存
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodWithBuiltInMethods_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension list {
                func customSum() -> int {
                    total <- 0
                    for item in this {
                        total <- total + item
                    }
                    return total
                }
            }

            numbers <- [1, 2, 3, 4, 5]
            builtInSum <- numbers.Sum()
            customSum <- numbers.customSum()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var builtInSum = vm.GetGlobalVariable("builtInSum");
        var customSum = vm.GetGlobalVariable("customSum");

        Assert.Equal(15, builtInSum);
        Assert.Equal(15, customSum);
    }

    /// <summary>
    /// 测试空列表的扩展方法
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodOnEmptyList_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension list {
                func sum() -> int {
                    total <- 0
                    for item in this {
                        total <- total + item
                    }
                    return total
                }
            }

            numbers <- []
            result <- numbers.sum()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(0, result);
    }

    /// <summary>
    /// 测试扩展方法返回this
    /// </summary>
    [Fact]
    public void Execute_ExtensionMethodReturningThis_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            extension int {
                func identity() -> int {
                    return this
                }
            }

            x <- 42
            result <- x.identity()
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
}
