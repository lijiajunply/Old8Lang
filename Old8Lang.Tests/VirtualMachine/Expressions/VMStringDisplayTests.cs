namespace Old8Lang.Tests.VirtualMachine.Expressions;

using VM = Old8Lang.Bytecode.VM.VirtualMachine;

/// <summary>
/// 虚拟机：字符串加法与插值按语言显示规则格式化（bool / list / null）。
/// </summary>
/// <remarks>
/// 此前 VM 的 <c>string + 值</c> 与 <c>String::Concat</c> 都走 CLR <c>ToString</c>，
/// 得到 <c>xTrue</c> / <c>System.Int32[]</c>；解释器与 IL 是 <c>xtrue</c> / <c>[1, 2]</c>。
/// </remarks>
public class VMStringDisplayTests
{
    [Fact]
    public void StringConcat_And_Interpolation_UseLanguageDisplay()
    {
        const string code = @"
            list <- [1, 2]
            b <- 1 in list
            concatBool <- ""x"" + b
            concatNull <- ""x"" + null
            concatList <- ""v="" + list
            interpolated <- $""i={1} b={b} l={list} n={null}""
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal("xtrue", vm.GetGlobalVariable("concatBool"));
        Assert.Equal("xnull", vm.GetGlobalVariable("concatNull"));
        Assert.Equal("v=[1, 2]", vm.GetGlobalVariable("concatList"));
        Assert.Equal("i=1 b=true l=[1, 2] n=null", vm.GetGlobalVariable("interpolated"));
    }
}
