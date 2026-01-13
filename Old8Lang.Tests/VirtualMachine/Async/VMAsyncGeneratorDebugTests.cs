using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.VirtualMachine.Async;

/// <summary>
/// 调试异步生成器问题
/// </summary>
[Collection("Sequential")]
public class VMAsyncGeneratorDebugTests
{
    private readonly ITestOutputHelper _output;

    public VMAsyncGeneratorDebugTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DebugAsyncGeneratorFlags()
    {
        // Arrange
        var code = @"
async func asyncRange(start:int, end:int) -> object {
    i <- start
    while i < end {
        yield i
        i <- i + 1
    }
}

gen <- asyncRange(1, 4)
";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // 编译为字节码
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        // 打印所有函数的信息
        _output.WriteLine("=== 字节码文件中的函数 ===");
        foreach (var func in bytecodeFile.Functions)
        {
            _output.WriteLine($"函数名: {func.Name}");
            _output.WriteLine($"  IsAsync: {func.IsAsync}");
            _output.WriteLine($"  IsGenerator: {func.IsGenerator}");
            _output.WriteLine($"  参数数量: {func.Parameters.Count}");
            _output.WriteLine("");
        }

        // 查找 asyncRange 函数
        var asyncRangeFunc = bytecodeFile.Functions.FirstOrDefault(f => f.Name == "asyncRange");
        Assert.NotNull(asyncRangeFunc);

        // 验证标志
        _output.WriteLine($"asyncRange 函数标志:");
        _output.WriteLine($"  IsAsync: {asyncRangeFunc.IsAsync} (期望: true)");
        _output.WriteLine($"  IsGenerator: {asyncRangeFunc.IsGenerator} (期望: true)");

        Assert.True(asyncRangeFunc.IsAsync, "asyncRange 应该被标记为异步函数");
        Assert.True(asyncRangeFunc.IsGenerator, "asyncRange 应该被标记为生成器函数");
    }

    [Fact]
    public void DebugMainFunctionInstructions()
    {
        // Arrange
        var code = @"
async func asyncRange(start:int, end:int) -> object {
    i <- start
    while i < end {
        yield i
        i <- i + 1
    }
}

gen <- asyncRange(1, 4)
";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // 编译为字节码
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        // 查找主函数
        var mainFunc = bytecodeFile.Functions.FirstOrDefault(f => f.Name == "<main>");
        Assert.NotNull(mainFunc);

        // 打印主函数的指令
        _output.WriteLine("=== 主函数指令 ===");
        for (int i = 0; i < mainFunc.Instructions.Count; i++)
        {
            var instr = mainFunc.Instructions[i];
            _output.WriteLine($"{i:D4}: {instr.OpCode} {instr.Operand}");
        }
    }
}
