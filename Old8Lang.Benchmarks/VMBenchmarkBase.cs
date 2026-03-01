using BenchmarkDotNet.Attributes;
using Old8Lang.Bytecode;
using Old8Lang.Bytecode.VM;
using Old8Lang.Interpreter;

namespace Old8Lang.Benchmarks;

/// <summary>
/// VM 模式基准测试基类
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 8)]
[Config(typeof(VMBenchmarkConfig))]
public abstract class VMBenchmarkBase
{
    protected static BytecodeFile CompileToBytecode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiler = new BytecodeCompiler();
        return compiler.Compile(ast);
    }

    protected static void ExecuteBytecode(BytecodeFile bytecodeFile)
    {
        var vm = new VirtualMachine(bytecodeFile);
        vm.Execute();
    }
}
