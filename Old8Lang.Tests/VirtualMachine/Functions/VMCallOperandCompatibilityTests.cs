using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Functions;

/// <summary>
/// VM Call 指令操作数兼容性测试
/// </summary>
public class VMCallOperandCompatibilityTests
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
    public void Call_Positional_OldOperandFormat_ExecutesCorrectly()
    {
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }

            result <- add(3, 4)
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var mainFunction = bytecodeFile.Functions[bytecodeFile.EntryPointIndex];
        var callInstruction = mainFunction.Instructions.First(i => i.OpCode == OpCode.Call);
        var operands = Assert.IsType<object[]>(callInstruction.Operand);
        Assert.True(operands.Length >= 3);

        // 模拟旧格式: [argCount, funcName]
        callInstruction.Operand = new object[] { operands[0], operands[1] };

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(7, GetIntValue(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void Call_Named_OldOperandFormat_ExecutesCorrectly()
    {
        var code = @"
            func greet(name:string, age:int, message:string) -> string {
                return message + "", "" + name + ""! Age: "" + age.ToStr()
            }

            result <- greet(name: ""Bob"", age: 30, message: ""Hi"")
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var mainFunction = bytecodeFile.Functions[bytecodeFile.EntryPointIndex];
        var callInstruction = mainFunction.Instructions.First(i => i.OpCode == OpCode.Call);
        var operands = Assert.IsType<object[]>(callInstruction.Operand);
        Assert.True(operands.Length >= 5);

        // 模拟旧格式: [positionalCount, namedCount, funcName, namedArgNames[]]
        callInstruction.Operand = new object[] { operands[0], operands[1], operands[2], operands[3] };

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal("Hi, Bob! Age: 30", vm.GetGlobalVariable("result"));
    }

    [Fact]
    public void Call_FunctionIndexMismatch_FallsBackToFunctionName()
    {
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }

            func sub(a:int, b:int) -> int {
                return a - b
            }

            result <- add(20, 5)
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var addIndex = bytecodeFile.Functions.FindIndex(f => f.Name == "add");
        var subIndex = bytecodeFile.Functions.FindIndex(f => f.Name == "sub");
        Assert.True(addIndex >= 0 && subIndex >= 0 && addIndex != subIndex);

        var mainFunction = bytecodeFile.Functions[bytecodeFile.EntryPointIndex];
        var callInstruction = mainFunction.Instructions.First(i => i.OpCode == OpCode.Call);
        callInstruction.Operand = new object[] { 2, "add", subIndex };

        var vm = new VM(bytecodeFile);
        vm.Execute();

        // 若未回退会得到 15；正确回退应为 25
        Assert.Equal(25, GetIntValue(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void Call_Positional_NewOperandFormat_IncludesFunctionIndex_ExecutesCorrectly()
    {
        var code = @"
            func mul(a:int, b:int) -> int {
                return a * b
            }

            result <- mul(6, 7)
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var mainFunction = bytecodeFile.Functions[bytecodeFile.EntryPointIndex];
        var callInstruction = mainFunction.Instructions.First(i => i.OpCode == OpCode.Call);
        var operands = Assert.IsType<object[]>(callInstruction.Operand);

        Assert.Equal(3, operands.Length);
        Assert.Equal(2, Assert.IsType<int>(operands[0]));
        Assert.Equal("mul", Assert.IsType<string>(operands[1]));
        Assert.True(Assert.IsType<int>(operands[2]) >= 0);

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(42, GetIntValue(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void Call_Named_InvalidFunctionIndex_FallsBackToFunctionName()
    {
        var code = @"
            func greet(name:string, age:int, message:string) -> string {
                return message + "", "" + name + ""! Age: "" + age.ToStr()
            }

            result <- greet(name: ""Alice"", age: 28, message: ""Hello"")
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var mainFunction = bytecodeFile.Functions[bytecodeFile.EntryPointIndex];
        var callInstruction = mainFunction.Instructions.First(i => i.OpCode == OpCode.Call);
        var operands = Assert.IsType<object[]>(callInstruction.Operand);

        callInstruction.Operand = new object[] { operands[0], operands[1], operands[2], operands[3], -1 };

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal("Hello, Alice! Age: 28", vm.GetGlobalVariable("result"));
    }
}
