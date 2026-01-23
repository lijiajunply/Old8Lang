using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Collections;

/// <summary>
/// 虚拟机数组测试
/// </summary>
public class VMArrayTests
{
    [Fact]
    public void Array_Creation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            result <- arr
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        // 虚拟机使用动态类型，返回 object[] 而不是 int[]
        Assert.IsAssignableFrom<Array>(result);
        var array = (Array)result;
        Assert.Equal(5, array.Length);
        Assert.Equal(1, array.GetValue(0));
        Assert.Equal(5, array.GetValue(4));
    }

    [Fact]
    public void Array_Access_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [10, 20, 30, 40, 50]
            result1 <- arr[0]
            result2 <- arr[2]
            result3 <- arr[4]
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(10, vm.GetGlobalVariable("result1"));
        Assert.Equal(30, vm.GetGlobalVariable("result2"));
        Assert.Equal(50, vm.GetGlobalVariable("result3"));
    }

    [Fact]
    public void Array_Length_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            result <- arr.Length
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(5, vm.GetGlobalVariable("result"));
    }
}
