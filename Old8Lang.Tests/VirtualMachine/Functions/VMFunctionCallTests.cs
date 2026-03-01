using VM = Old8Lang.Bytecode.VM.VirtualMachine;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.Tests.VirtualMachine.Functions;

/// <summary>
/// 虚拟机函数调用测试
/// </summary>
public class VMFunctionCallTests
{
    [Fact]
    public void FunctionCall_EmitsFunctionIndexOperand_AndExecutes()
    {
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }

            result <- add(1, 2)
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var mainFunction = bytecodeFile.Functions[bytecodeFile.EntryPointIndex];
        var callInstruction = mainFunction.Instructions.First(i => i.OpCode == OpCode.Call);
        var operands = Assert.IsType<object[]>(callInstruction.Operand);

        Assert.Equal(3, operands.Length);
        Assert.Equal(2, Assert.IsType<int>(operands[0]));
        Assert.Equal("add", Assert.IsType<string>(operands[1]));
        Assert.True(Assert.IsType<int>(operands[2]) >= 0);

        var vm = new VM(bytecodeFile);
        vm.Execute();
        Assert.Equal(3, vm.GetGlobalVariable("result"));
    }

    [Fact]
    public void FunctionCall_InvalidFunctionIndex_FallsBackToFunctionName()
    {
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }

            result <- add(10, 20)
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var mainFunction = bytecodeFile.Functions[bytecodeFile.EntryPointIndex];
        int callInstructionIndex = mainFunction.Instructions.FindIndex(i => i.OpCode == OpCode.Call);
        Assert.True(callInstructionIndex >= 0);

        mainFunction.Instructions[callInstructionIndex].Operand = new object[] { 2, "add", 9_999_999 };

        var vm = new VM(bytecodeFile);
        vm.Execute();
        Assert.Equal(30, vm.GetGlobalVariable("result"));
    }

    [Fact]
    public void FunctionCall_WithDefaultParameters_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func greet(name:string, message: ""Hello"") -> string {
                return message + "", "" + name
            }

            result1 <- greet(""Alice"")
            result2 <- greet(""Bob"", ""Hi"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal("Hello, Alice", vm.GetGlobalVariable("result1"));
        Assert.Equal("Hi, Bob", vm.GetGlobalVariable("result2"));
    }

    [Fact]
    public void FunctionCall_NestedCalls_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }

            func multiply(x:int, y:int) -> int {
                return x * y
            }

            result <- multiply(add(2, 3), add(4, 6))
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(50, vm.GetGlobalVariable("result")); // (2+3) * (4+6) = 5 * 10 = 50
    }

    [Fact]
    public void FunctionCall_ReturnValue_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func calculate(x:int, y:int) -> int {
                sum <- x + y
                product <- x * y
                return sum + product
            }

            result <- calculate(3, 4)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(19, vm.GetGlobalVariable("result")); // (3+4) + (3*4) = 7 + 12 = 19
    }
}
