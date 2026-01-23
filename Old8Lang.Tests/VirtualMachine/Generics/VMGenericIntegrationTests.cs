using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Generics;

/// <summary>
/// 虚拟机泛型集成测试 - 测试泛型类和泛型函数的组合使用
/// </summary>
public class VMGenericIntegrationTests
{
    [Fact]
    public void GenericClassAndFunction_Combined_ExecutesCorrectly()
    {
        // Arrange - 泛型类和泛型函数组合使用
        var code = @"
            class Box<T> {
                public value:T
            }

            func createBox<T>(val:T) -> object {
                box <- Box<T>()
                box.value <- val
                return box
            }

            intBox <- createBox<int>(42)
            result <- intBox.value
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.Equal(42, result);
    }

    [Fact]
    public void GenericFunction_ReturnsGenericClass_ExecutesCorrectly()
    {
        // Arrange - 泛型函数返回泛型类实例
        var code = @"
            class Container<T> {
                public data:T
            }

            func wrap<T>(value:T) -> object {
                container <- Container<T>()
                container.data <- value
                return container
            }

            wrapped <- wrap<string>(""Hello"")
            result <- wrapped.data
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.Equal("Hello", result);
    }
}
