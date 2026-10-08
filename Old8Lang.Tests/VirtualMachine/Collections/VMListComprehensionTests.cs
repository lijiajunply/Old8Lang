using System.Collections;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Collections;

/// <summary>
/// 虚拟机列表推导式测试
/// </summary>
/// <remarks>
/// 列表推导式在字节码模式下曾是空实现（VisitListComprehension 直接返回 null），
/// 表达式不产出任何字节码会导致栈失衡，最终在无关指令处报 "Stack empty"。
/// 这里同时覆盖解释器与虚拟机的结果一致性。
/// </remarks>
[Collection("Sequential")]
public class VMListComprehensionTests
{
    private static object? ExecuteVm(string code, string variableName)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var bytecodeFile = new Old8Lang.Bytecode.BytecodeCompiler().Compile(ast);
        var vm = new VM(bytecodeFile);
        vm.Execute();
        return vm.GetGlobalVariable(variableName);
    }

    private static object? ExecuteInterpreter(string code, string variableName)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);
        return interpreter.Manager.GetValue(new Old8Lang.AST.Expression.LangId(variableName))?.GetValue();
    }

    private static List<object?> AsList(object? value)
    {
        var list = Assert.IsAssignableFrom<IList>(value);
        return list.Cast<object?>().ToList();
    }

    [Fact]
    public void Comprehension_BasicMapping_ProducesMappedList()
    {
        var result = ExecuteVm("result <- [x * 2 for x in [1, 2, 3]]", "result");

        Assert.Equal([2, 4, 6], AsList(result));
    }

    [Fact]
    public void Comprehension_WithCondition_FiltersElements()
    {
        var result = ExecuteVm("result <- [x for x in [1, 2, 3, 4] if x % 2 == 0]", "result");

        Assert.Equal([2, 4], AsList(result));
    }

    [Fact]
    public void Comprehension_NestedLoops_ProducesCartesianProduct()
    {
        var result = ExecuteVm("result <- [x + y for x in [1, 2] for y in [10, 20]]", "result");

        Assert.Equal([11, 21, 12, 22], AsList(result));
    }

    [Fact]
    public void Comprehension_ConditionOnBothLevels_FiltersBoth()
    {
        var code = "result <- [x * y for x in [1, 2, 3] if x > 1 for y in [10, 20] if y > 15]";
        var result = ExecuteVm(code, "result");

        Assert.Equal([40, 60], AsList(result));
    }

    [Fact]
    public void Comprehension_OverString_ProducesCharList()
    {
        var result = ExecuteVm("result <- [c for c in \"abc\"]", "result");

        Assert.Equal(3, AsList(result).Count);
    }

    [Fact]
    public void Comprehension_EmptySource_ProducesEmptyList()
    {
        var result = ExecuteVm("result <- [x for x in []]", "result");

        Assert.Empty(AsList(result));
    }

    [Fact]
    public void Comprehension_CanReferenceOuterVariables()
    {
        var result = ExecuteVm("n <- 3\nresult <- [x * n for x in [1, 2]]", "result");

        Assert.Equal([3, 6], AsList(result));
    }

    [Fact]
    public void Comprehension_ResultIsUsableAsValue()
    {
        var result = ExecuteVm("result <- [x + 1 for x in [1, 2, 3]][2]", "result");

        Assert.Equal(4, result);
    }

    [Theory]
    [InlineData("result <- [x * 2 for x in [1, 2, 3]]")]
    [InlineData("result <- [x for x in [1, 2, 3, 4] if x > 2]")]
    [InlineData("result <- [x + y for x in [1, 2] for y in [10, 20]]")]
    [InlineData("result <- [x for x in [] ]")]
    public void Comprehension_MatchesInterpreter(string code)
    {
        // 两种执行模式的结果必须一致：模式之间行为漂移正是本次修复要消除的问题
        var viaVm = AsList(ExecuteVm(code, "result"));
        var viaInterpreter = AsList(ExecuteInterpreter(code, "result"));

        Assert.Equal(viaInterpreter, viaVm);
    }
}
