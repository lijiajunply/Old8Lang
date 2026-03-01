using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Async;

/// <summary>
/// VM CallAsync 指令操作数兼容性测试
/// </summary>
public class VMCallAsyncOperandCompatibilityTests
{
    /// <summary>
    /// 辅助方法：从全局变量中提取 int，兼容 IntLangValue 等封装值
    /// </summary>
    private static int GetIntValue(object? value)
    {
        return value switch
        {
            int i => i,
            long l => (int)l,
            IntLangValue ilv => ilv.Value,
            _ => Convert.ToInt32(value)
        };
    }

    [Fact]
    public void CallAsync_OldOperandFormat_ExecutesCorrectly()
    {
        var code = @"
            async func add(a:int, b:int) -> int {
                return a + b
            }

            task <- add(6, 7)
            result <- await task
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var mainFunction = bytecodeFile.Functions[bytecodeFile.EntryPointIndex];
        var callAsyncInstruction = mainFunction.Instructions.First(i => i.OpCode == OpCode.CallAsync);
        var operands = Assert.IsType<object[]>(callAsyncInstruction.Operand);
        Assert.True(operands.Length >= 3);

        // 模拟旧格式: [argCount, funcName]
        callAsyncInstruction.Operand = new object[] { operands[0], operands[1] };

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(13, GetIntValue(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void CallAsync_InvalidFunctionIndex_FallsBackToFunctionName()
    {
        var code = @"
            async func add(a:int, b:int) -> int {
                return a + b
            }

            task <- add(8, 9)
            result <- await task
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var mainFunction = bytecodeFile.Functions[bytecodeFile.EntryPointIndex];
        var callAsyncInstruction = mainFunction.Instructions.First(i => i.OpCode == OpCode.CallAsync);
        callAsyncInstruction.Operand = new object[] { 2, "add", 9_999_999 };

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(17, GetIntValue(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void CallAsync_NewOperandFormat_IncludesFunctionIndex_ExecutesCorrectly()
    {
        var code = @"
            async func mul(a:int, b:int) -> int {
                return a * b
            }

            task <- mul(4, 11)
            result <- await task
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var mainFunction = bytecodeFile.Functions[bytecodeFile.EntryPointIndex];
        var callAsyncInstruction = mainFunction.Instructions.First(i => i.OpCode == OpCode.CallAsync);
        var operands = Assert.IsType<object[]>(callAsyncInstruction.Operand);

        Assert.Equal(3, operands.Length);
        Assert.Equal(2, Assert.IsType<int>(operands[0]));
        Assert.Equal("mul", Assert.IsType<string>(operands[1]));
        Assert.True(Assert.IsType<int>(operands[2]) >= 0);

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(44, GetIntValue(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void CallAsync_NegativeFunctionIndex_FallsBackToFunctionName()
    {
        var code = @"
            async func add(a:int, b:int) -> int {
                return a + b
            }

            task <- add(9, 6)
            result <- await task
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var mainFunction = bytecodeFile.Functions[bytecodeFile.EntryPointIndex];
        var callAsyncInstruction = mainFunction.Instructions.First(i => i.OpCode == OpCode.CallAsync);
        callAsyncInstruction.Operand = new object[] { 2, "add", -1 };

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(15, GetIntValue(vm.GetGlobalVariable("result")));
    }
}
