namespace Old8Lang.Tests.VirtualMachine.Expressions;

using VM = Old8Lang.Bytecode.VM.VirtualMachine;

/// <summary>
/// 虚拟机混合数值类型（int / double）的比较。
/// </summary>
/// <remarks>
/// `Equals` 曾经只处理同类型比较，`3 == 3.0` 会落到 `object.Equals` 得到 false；
/// `>=` / `<=` 又是用「大于/小于 或 相等」拼出来的，于是 `3 >= 3.0`、`3.0 <= 3`
/// 都被判成 false，而解释器与 IL 模式为 true。
/// </remarks>
public class VMMixedNumericComparisonTests
{
    [Fact]
    public void Equality_AcrossIntAndDouble_UsesNumericValue()
    {
        const string code = @"
            a <- 3
            b <- 3.0
            r1 <- a == b
            r2 <- a != b
            r3 <- a == 2.5
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(true, vm.GetGlobalVariable("r1"));
        Assert.Equal(false, vm.GetGlobalVariable("r2"));
        Assert.Equal(false, vm.GetGlobalVariable("r3"));
    }

    [Fact]
    public void Ordering_AcrossIntAndDouble_MatchesInterpreter()
    {
        const string code = @"
            a <- 3
            b <- 3.0
            r1 <- a >= b
            r2 <- b <= a
            r3 <- 2.5 < a
            r4 <- b > 4
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(true, vm.GetGlobalVariable("r1"));
        Assert.Equal(true, vm.GetGlobalVariable("r2"));
        Assert.Equal(true, vm.GetGlobalVariable("r3"));
        Assert.Equal(false, vm.GetGlobalVariable("r4"));
    }
}
