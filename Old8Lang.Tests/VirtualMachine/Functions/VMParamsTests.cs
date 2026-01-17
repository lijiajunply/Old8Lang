using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Functions;

/// <summary>
/// 虚拟机 params 可变参数测试
/// </summary>
[Collection("Sequential")]
public class VMParamsTests
{
    [Fact]
    public void ParamsFunction_WithNoArguments_ExecutesCorrectly()
    {
        // 测试不传入任何可变参数
        var code = @"
func sum(params args:array<int>) -> int {
    return args.Length
}

result <- sum()
";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(0, vm.GetGlobalVariable("result"));
    }

    [Fact]
    public void ParamsFunction_WithMultipleArguments_ExecutesCorrectly()
    {
        // 测试传入多个可变参数
        var code = @"
func sum(params args:array<int>) -> int {
    return args.Length
}

result <- sum(1, 2, 3, 4, 5)
";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(5, vm.GetGlobalVariable("result"));
    }

    [Fact]
    public void ParamsFunction_WithRegularParametersAndNoVarArgs_ExecutesCorrectly()
    {
        // 测试普通参数 + 不传可变参数
        var code = @"
func format(prefix:string, params args:array<string>) -> string {
    return prefix
}

result <- format(""start"")
";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal("start", vm.GetGlobalVariable("result"));
    }

    [Fact]
    public void ParamsFunction_WithRegularParametersAndVarArgs_ExecutesCorrectly()
    {
        // 测试普通参数 + 可变参数
        var code = @"
func getInfo(prefix:string, params args:array<string>) -> int {
    return args.Length
}

result <- getInfo(""start"", ""a"", ""b"", ""c"")
";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(3, vm.GetGlobalVariable("result"));
    }

    [Fact]
    public void ParamsFunction_ArrayLength_ExecutesCorrectly()
    {
        // 测试访问 params 数组的长度
        var code = @"
func getCount(params items:array<int>) -> int {
    return items.Length
}

result <- getCount(10, 20, 30)
";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(3, vm.GetGlobalVariable("result"));
    }
}
