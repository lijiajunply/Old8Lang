namespace Old8Lang.Tests.VirtualMachine.Expressions;

using VM = Old8Lang.Bytecode.VM.VirtualMachine;

/// <summary>
/// 虚拟机：范围字面量在端点相等且被排除时的结果（与解释器/IL 对齐）。
/// </summary>
public class VMRangeBoundaryTests
{
    [Theory]
    [InlineData("[5>~<5]", 0)]
    [InlineData("[5>~5]", 0)]
    [InlineData("[5~<5]", 0)]
    [InlineData("[5~5]", 1)]
    [InlineData("[1~<4]", 3)]
    [InlineData("[1>~4]", 3)]
    [InlineData("[10~1]", 10)]
    public void EqualAndExcludedEndpoints_ProduceExpectedLength(string expression, int expectedLength)
    {
        var code = $"range <- {expression}";
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var range = vm.GetGlobalVariable("range");
        var array = Assert.IsType<int[]>(range);
        Assert.Equal(expectedLength, array.Length);
    }
}
