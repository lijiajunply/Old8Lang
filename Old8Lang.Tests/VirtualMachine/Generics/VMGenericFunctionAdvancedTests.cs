using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Generics;

/// <summary>
/// 虚拟机泛型函数高级测试
/// </summary>
public class VMGenericFunctionAdvancedTests
{
    [Fact]
    public void GenericFunction_MultipleTypeParameters_ExecutesCorrectly()
    {
        // Arrange - 多个类型参数的泛型函数
        var code = @"
            func makePair<K, V>(key:K, value:V) -> object {
                result <- {key, value}
                return result
            }

            pair <- makePair<string, int>(""age"", 25)
            result <- pair
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
    }

    [Fact]
    public void GenericFunction_WithArrays_ExecutesCorrectly()
    {
        // Arrange - 泛型函数处理数组
        var code = @"
            func getFirst<T>(arr:object) -> T {
                return arr[0]
            }

            numbers <- [10, 20, 30]
            result <- getFirst<int>(numbers)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.Equal(10, result);
    }
}
