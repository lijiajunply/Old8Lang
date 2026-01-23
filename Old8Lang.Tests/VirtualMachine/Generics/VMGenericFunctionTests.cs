using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Generics;

/// <summary>
/// 虚拟机泛型函数测试
/// </summary>
public class VMGenericFunctionTests
{
    [Fact]
    public void GenericFunction_Identity_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func identity<T>(value:T) -> T {
                return value
            }

            result <- identity<int>(42)
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
    public void GenericFunction_IdentityString_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func identity<T>(value:T) -> T {
                return value
            }

            result <- identity<string>(""Hello"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Hello", result);
    }
}
