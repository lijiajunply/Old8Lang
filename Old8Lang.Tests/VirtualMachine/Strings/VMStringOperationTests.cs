using Old8Lang.Bytecode;
using Xunit;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Strings;

/// <summary>
/// 虚拟机字符串操作测试
/// </summary>
public class VMStringOperationTests
{
    [Fact]
    public void StringConcatenation_TwoStrings_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- ""Hello"" + "" World""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void StringComparison_Equal_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- ""test"" == ""test""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(true, result);
    }
}
