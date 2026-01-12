using Old8Lang.Bytecode;
using Xunit;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Collections;

/// <summary>
/// 虚拟机字典测试
/// </summary>
public class VMDictionaryTests
{
    [Fact]
    public void Dictionary_Creation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            dict <- {""name"": ""Alice"", ""age"": 25}
            result <- dict
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.IsAssignableFrom<System.Collections.IDictionary>(result);
    }

    [Fact]
    public void Dictionary_Access_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            dict <- {""name"": ""Bob"", ""age"": 30}
            result1 <- dict[""name""]
            result2 <- dict[""age""]
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal("Bob", vm.GetGlobalVariable("result1"));
        Assert.Equal(30, vm.GetGlobalVariable("result2"));
    }

    [Fact]
    public void Dictionary_Keys_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            dict <- {""x"": 10, ""y"": 20, ""z"": 30}
            keys <- dict.Keys
            result <- keys.Count
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(3, vm.GetGlobalVariable("result"));
    }
}
