using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Collections;

/// <summary>
/// 虚拟机列表测试
/// </summary>
public class VMListTests
{
    [Fact]
    public void List_Creation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            list <- {1, 2, 3}
            result <- list
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.IsAssignableFrom<System.Collections.IList>(result);
    }

    [Fact(Skip = "虚拟机暂不支持对象方法调用（list.Add）")]
    public void List_Add_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            list <- {1, 2, 3}
            list.Add(4)
            list.Add(5)
            result <- list.Count
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(5, vm.GetGlobalVariable("result"));
    }

    [Fact(Skip = "虚拟机暂不支持对象方法调用（list.Remove）")]
    public void List_Remove_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            list <- {10, 20, 30, 40}
            list.Remove(20)
            result <- list.Count
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(3, vm.GetGlobalVariable("result"));
    }
}
