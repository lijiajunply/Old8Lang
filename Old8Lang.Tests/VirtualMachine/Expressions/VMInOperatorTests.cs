using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Expressions;

/// <summary>
/// 虚拟机 <c>in</c> 运算符。
/// </summary>
/// <remarks>
/// 原先 <c>BytecodeVisitor</c> 的二元运算符分派没有 <c>In</c> 分支，落到
/// <c>default: Emit(Nop)</c>，左右操作数都留在栈上，表达式结果变成右操作数：
/// <c>1 in [1,2,3]</c> 输出 <c>[1, 2, 3]</c>、<c>"a" in {"a":1}</c> 输出字典。
/// 现在新增 <c>OpCode.In</c>，复用 <see cref="Old8Lang.AST.Expression.Intermediates.ILangList.In"/>
/// 的语义（字符/子串、列表按值、字典按键），并补原生集合回退。
/// </remarks>
public class VMInOperatorTests
{
    [Theory]
    [InlineData("result <- 1 in [1, 2, 3]", true)]
    [InlineData("result <- 6 in [1, 2, 3]", false)]
    [InlineData("result <- 10 in {10, 20, 30}", true)]
    [InlineData("result <- 25 in {10, 20, 30}", false)]
    [InlineData("result <- \"name\" in {\"name\": 1}", true)]
    [InlineData("result <- \"city\" in {\"name\": 1}", false)]
    [InlineData("result <- 'h' in \"hello\"", true)]
    [InlineData("result <- 'x' in \"hello\"", false)]
    [InlineData("result <- \"ell\" in \"hello\"", true)]
    [InlineData("result <- \"xyz\" in \"hello\"", false)]
    public void InOperator_MatchesInterpreterSemantics(string code, bool expected)
    {
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(expected, vm.GetGlobalVariable("result"));
    }

    [Fact]
    public void InOperator_AsCondition_BranchesCorrectly()
    {
        const string code = @"
            if 2 in [1, 2, 3] {
                result <- ""yes""
            } else {
                result <- ""no""
            }
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal("yes", vm.GetGlobalVariable("result"));
    }
}
