using Old8Lang.Bytecode;
using Xunit;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Functions;

/// <summary>
/// 虚拟机命名参数测试
/// </summary>
public class VMNamedArgumentsTests
{
    [Fact]
    public void NamedArguments_AllNamed_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func greet(name:string, age:int, message:string) -> string {
                return message + "", "" + name + ""! Age: "" + age.ToStr()
            }
            result <- greet(name: ""Bob"", age: 30, message: ""Hi"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Hi, Bob! Age: 30", result);
    }

    [Fact]
    public void NamedArguments_MixedWithPositional_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func greet(name:string, age:int, message:string) -> string {
                return message + "", "" + name + ""! Age: "" + age.ToStr()
            }
            result <- greet(""Charlie"", age: 35, message: ""Good morning"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Good morning, Charlie! Age: 35", result);
    }

    [Fact]
    public void NamedArguments_OutOfOrder_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func calculate(x:int, y:int, operation:string) -> int {
                if operation == ""add"" {
                    return x + y
                } elif operation == ""mul"" {
                    return x * y
                } else {
                    return x - y
                }
            }
            result <- calculate(operation: ""mul"", y: 3, x: 7)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(21, result);
    }
}
