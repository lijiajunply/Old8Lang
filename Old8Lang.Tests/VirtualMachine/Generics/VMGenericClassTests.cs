using Old8Lang.Bytecode;
using Xunit;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Generics;

/// <summary>
/// 虚拟机泛型类测试
/// </summary>
public class VMGenericClassTests
{
    [Fact]
    public void GenericClass_SimpleBox_ExecutesCorrectly()
    {
        // Arrange - 暂时跳过此测试，因为字节码模式下 this 关键字存在问题
        // 这不是泛型特有的问题，而是字节码模式的已知限制
        var code = @"
            class Box<T> {
                public value:T
            }

            box <- Box<int>()
            box.value <- 42
            result <- box.value
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
    public void GenericClass_StringBox_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Box<T> {
                public value:T
            }

            box <- Box<string>()
            box.value <- ""Hello Generic""
            result <- box.value
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Hello Generic", result);
    }

    [Fact]
    public void GenericClass_MultipleInstances_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Box<T> {
                public value:T
            }

            intBox <- Box<int>()
            intBox.value <- 42

            stringBox <- Box<string>()
            stringBox.value <- ""Test""

            result1 <- intBox.value
            result2 <- stringBox.value
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal(42, result1);
        Assert.Equal("Test", result2);
    }
}
