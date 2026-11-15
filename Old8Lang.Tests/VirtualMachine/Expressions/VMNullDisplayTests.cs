namespace Old8Lang.Tests.VirtualMachine.Expressions;

using VM = Old8Lang.Bytecode.VM.VirtualMachine;

/// <summary>
/// 虚拟机：null 的 ToStr / 字符串插值显示为 <c>null</c>（与解释器一致）。
/// </summary>
public class VMNullDisplayTests
{
    [Fact]
    public void Null_ToStrAndInterpolation_UseLowercaseNull()
    {
        const string code = @"
x <- null
asString <- x.ToStr()
interpolated <- $""v={x}""
";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal("null", vm.GetGlobalVariable("asString"));
        Assert.Equal("v=null", vm.GetGlobalVariable("interpolated"));
    }
}
