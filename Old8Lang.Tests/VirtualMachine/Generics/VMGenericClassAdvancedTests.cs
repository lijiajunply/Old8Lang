using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Generics;

/// <summary>
/// 虚拟机泛型类高级测试
/// </summary>
public class VMGenericClassAdvancedTests
{
    [Fact]
    public void GenericClass_NestedGenericTypes_ExecutesCorrectly()
    {
        // Arrange - 嵌套泛型类型
        var code = @"
            class Container<T> {
                public data:T
            }

            class Pair<K, V> {
                public key:K
                public value:V
            }

            // 创建 Container<Pair<string, int>>
            outerBox <- Container<object>()
            innerPair <- Pair<string, int>()
            innerPair.key <- ""age""
            innerPair.value <- 25
            outerBox.data <- innerPair

            result <- outerBox.data
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
    public void GenericClass_MultipleTypeParameters_ExecutesCorrectly()
    {
        // Arrange - 多个类型参数
        var code = @"
            class Pair<K, V> {
                public key:K
                public value:V
            }

            pair1 <- Pair<string, int>()
            pair1.key <- ""age""
            pair1.value <- 30

            pair2 <- Pair<int, string>()
            pair2.key <- 100
            pair2.value <- ""score""

            result1 <- pair1.value
            result2 <- pair2.value
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal(30, result1);
        Assert.Equal("score", result2);
    }
}
