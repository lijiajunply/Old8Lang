using Old8Lang.Bytecode;
using Xunit;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Functions;

/// <summary>
/// 虚拟机函数调用测试
/// </summary>
public class VMFunctionCallTests
{
    [Fact]
    public void FunctionCall_WithDefaultParameters_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func greet(name:string, message: ""Hello"") -> string {
                return message + "", "" + name
            }

            result1 <- greet(""Alice"")
            result2 <- greet(""Bob"", ""Hi"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal("Hello, Alice", vm.GetGlobalVariable("result1"));
        Assert.Equal("Hi, Bob", vm.GetGlobalVariable("result2"));
    }

    [Fact]
    public void FunctionCall_NestedCalls_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }

            func multiply(x:int, y:int) -> int {
                return x * y
            }

            result <- multiply(add(2, 3), add(4, 6))
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(50, vm.GetGlobalVariable("result")); // (2+3) * (4+6) = 5 * 10 = 50
    }

    [Fact]
    public void FunctionCall_ReturnValue_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func calculate(x:int, y:int) -> int {
                sum <- x + y
                product <- x * y
                return sum + product
            }

            result <- calculate(3, 4)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(19, vm.GetGlobalVariable("result")); // (3+4) + (3*4) = 7 + 12 = 19
    }
}
