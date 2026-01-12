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

    [Fact]
    public void StringLength_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            str <- ""Hello World""
            result <- str.Length
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(11, result);
    }

    [Fact(Skip = "虚拟机不支持对象方法调用 - Substring方法未实现")]
    public void StringSubstring_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            str <- ""Hello World""
            result <- str.Substring(6, 5)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("World", result);
    }

    [Fact(Skip = "虚拟机不支持对象方法调用 - IndexOf方法未实现")]
    public void StringIndexOf_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            str <- ""Hello World""
            result <- str.IndexOf(""World"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(6, result);
    }

    [Fact(Skip = "虚拟机不支持对象方法调用 - IndexOf方法未实现")]
    public void StringIndexOf_NotFound_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            str <- ""Hello World""
            result <- str.IndexOf(""xyz"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(-1, result);
    }
}
