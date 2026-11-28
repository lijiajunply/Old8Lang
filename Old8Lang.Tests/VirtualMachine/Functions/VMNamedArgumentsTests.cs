using VM = Old8Lang.Bytecode.VM.VirtualMachine;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.Tests.VirtualMachine.Functions;

/// <summary>
/// 虚拟机命名参数测试
/// </summary>
public class VMNamedArgumentsTests
{
    [Fact]
    public void NativeNamedArguments_AreBoundByNameWithPositionalArguments()
    {
        const string code = @"
            channel <- ChannelCreate()
            ChannelSend(value: 42, channelId: channel)
            first <- ChannelReceive(channelId: channel)
            ChannelSend(channel, value: 77)
            second <- ChannelReceive(channel)
            ChannelClose(channel)
        ";
        var vm = new VM(CompileHelper.CompileToBytecode(code));
        try
        {
            vm.Execute();
            Assert.Equal(42, vm.GetGlobalVariable("first"));
            Assert.Equal(77, vm.GetGlobalVariable("second"));
        }
        finally
        {
            if (vm.GetGlobalVariable("channel") is int channel)
                Old8Lang.Concurrency.ResourceManager.DisposeChannel(channel);
        }
    }

    [Fact]
    public void NativeNamedArguments_SurviveBytecodeSerialization()
    {
        var bytecode = CompileHelper.CompileToBytecode("sem <- SemaphoreCreate(maxCount: 2, initialCount: 1)");
        var path = Path.Combine(Path.GetTempPath(), $"o8named_{Guid.NewGuid():N}.o8bc");
        VM? vm = null;
        try
        {
            bytecode.SaveToFile(path);
            vm = new VM(Bytecode.BytecodeFile.LoadFromFile(path));
            vm.Execute();
            Assert.IsType<int>(vm.GetGlobalVariable("sem"));
        }
        finally
        {
            if (vm?.GetGlobalVariable("sem") is int semaphore)
                Old8Lang.Concurrency.ResourceManager.DisposeSemaphore(semaphore);
            File.Delete(path);
        }
    }

    [Fact]
    public void UserNamedArguments_SurviveBytecodeSerialization()
    {
        const string code = "func subtract(x:int, y:int) -> int { return x - y } result <- subtract(y: 3, x: 10)";
        var bytecode = CompileHelper.CompileToBytecode(code);
        var path = Path.Combine(Path.GetTempPath(), $"o8named_{Guid.NewGuid():N}.o8bc");
        try
        {
            bytecode.SaveToFile(path);
            var vm = new VM(Bytecode.BytecodeFile.LoadFromFile(path));
            vm.Execute();
            Assert.Equal(7, vm.GetGlobalVariable("result"));
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("SemaphoreCreate(1, initialCount: 1)")]
    [InlineData("SemaphoreCreate(initialCount: 1, unknown: 1)")]
    [InlineData("SemaphoreCreate(initialCount: 1, initialCount: 1)")]
    [InlineData("SemaphoreCreate(maxCount: 1)")]
    public void NativeNamedArguments_InvalidBindingThrowsArgumentError(string call)
    {
        var vm = new VM(CompileHelper.CompileToBytecode(call));
        Assert.Throws<Old8Lang.Error.ArgumentError>(() => vm.Execute());
    }

    [Fact]
    public void NamedArguments_EmitsFunctionIndexOperand_AndExecutes()
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

        Assert.Equal(5, operands.Length);
        Assert.Equal(0, Assert.IsType<int>(operands[0]));
        Assert.Equal(3, Assert.IsType<int>(operands[1]));
        Assert.Equal("greet", Assert.IsType<string>(operands[2]));
        Assert.NotEmpty(Assert.IsType<string[]>(operands[3]));
        Assert.True(Assert.IsType<int>(operands[4]) >= 0);

        var vm = new VM(bytecodeFile);
        vm.Execute();
        Assert.Equal("Hi, Bob! Age: 30", vm.GetGlobalVariable("result"));
    }

    [Fact]
    public void NamedArguments_AllNamed_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func greet(name:string, age:int, message:string) -> string {
                return message + "", "" + name + ""! Age: "" + age.ToStr()
            }
            result <- greet(name: ""Bob"", age: 30, message: ""Hi"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Hi, Bob! Age: 30", result);
    }

    [Fact]
    public void NamedArguments_MixedWithPositional_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func greet(name:string, age:int, message:string) -> string {
                return message + "", "" + name + ""! Age: "" + age.ToStr()
            }
            result <- greet(""Charlie"", age: 35, message: ""Good morning"")
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Good morning, Charlie! Age: 35", result);
    }

    [Fact]
    public void NamedArguments_OutOfOrder_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func calculate(x:int, y:int, operation:string) -> int {
                if operation == ""add"" {
                    return x + y
                } elif operation == ""mul"" {
                    return x * y
                } else {
                    return x - y
                }
            }
            result <- calculate(operation: ""mul"", y: 3, x: 7)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(21, result);
    }
}
