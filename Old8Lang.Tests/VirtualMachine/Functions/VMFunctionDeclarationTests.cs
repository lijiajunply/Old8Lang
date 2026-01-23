using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Functions;

/// <summary>
/// 虚拟机函数声明测试
/// </summary>
public class VMFunctionDeclarationTests
{
    [Fact]
    public void FunctionDeclaration_NoParameters_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func sayHello() -> string {
                return ""Hello, World!""
            }
            result <- sayHello()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void FunctionDeclaration_WithParameters_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }
            result <- add(5, 3)
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
    public void FunctionDeclaration_WithTypeAnnotations_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func multiply(x:int, y:int) -> int {
                return x * y
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
}
