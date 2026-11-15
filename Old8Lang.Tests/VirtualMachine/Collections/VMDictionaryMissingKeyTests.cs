namespace Old8Lang.Tests.VirtualMachine.Collections;

using VM = Old8Lang.Bytecode.VM.VirtualMachine;

/// <summary>
/// 虚拟机：字典缺失键统一抛 KEY_ERROR。
/// </summary>
public class VMDictionaryMissingKeyTests
{
    [Fact]
    public void MissingKey_IndexAccess_ThrowsKeyError()
    {
        const string code = @"
d <- {""a"": 1}
value <- d[""missing""]
";
        var exception = Run(code);
        Assert.Contains("KEY_ERROR", exception.Message);
        Assert.Contains("missing", exception.Message);
    }

    [Fact]
    public void MissingKey_GetValue_ThrowsKeyError()
    {
        const string code = @"
d <- {""a"": 1}
value <- d.GetValue(""missing"")
";
        var exception = Run(code);
        Assert.Contains("KEY_ERROR", exception.Message);
        Assert.Contains("missing", exception.Message);
    }

    [Fact]
    public void GetOrElse_OnMissingKey_StillReturnsDefault()
    {
        const string code = @"
d <- {""a"": 1}
value <- d.GetOrElse(""missing"", 42)
";
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(42, vm.GetGlobalVariable("value"));
    }

    private static System.Exception Run(string code)
    {
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        return Assert.ThrowsAny<System.Exception>(() => vm.Execute());
    }
}
