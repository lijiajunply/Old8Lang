using System.Globalization;
using BenchmarkDotNet.Attributes;
using Old8Lang.Benchmarks.Benchmarks.VM.Config;
using Old8Lang.Bytecode;
using Old8Lang.Bytecode.VM;
using Old8Lang.Interpreter;

namespace Old8Lang.Benchmarks.Benchmarks.VM.Base;

/// <summary>
/// VM 模式基准测试基类
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 8)]
[Config(typeof(VMBenchmarkConfig))]
public abstract class VMBenchmarkBase
{
    protected static string ResolveTestDataPath(string fileName)
    {
        var directPath = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
        if (File.Exists(directPath))
        {
            return directPath;
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidateInCurrent = Path.Combine(current.FullName, "TestData", fileName);
            if (File.Exists(candidateInCurrent))
            {
                return candidateInCurrent;
            }

            var candidateInProject = Path.Combine(current.FullName, "Old8Lang.Benchmarks", "TestData", fileName);
            if (File.Exists(candidateInProject))
            {
                return candidateInProject;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"固定大文件测试数据不存在: {directPath}");
    }

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

    protected static long ExecuteAndAssertGlobalInt(BytecodeFile bytecodeFile, string variableName, long expectedValue)
    {
        var vm = new VirtualMachine(bytecodeFile);
        vm.Execute();

        var raw = vm.GetGlobalVariable(variableName);
        if (!TryConvertToInt64(raw, out var actual))
        {
            throw new InvalidOperationException(
                $"VM 正确性校验失败：变量 {variableName} 不是整数，实际值: {raw ?? "null"}");
        }

        if (actual != expectedValue)
        {
            throw new InvalidOperationException(
                $"VM 正确性校验失败：变量 {variableName} 期望 {expectedValue}，实际 {actual}");
        }

        return actual;
    }

    private static bool TryConvertToInt64(object? value, out long result)
    {
        switch (value)
        {
            case null:
                result = 0;
                return false;
            case long l:
                result = l;
                return true;
            case int i:
                result = i;
                return true;
            case short s:
                result = s;
                return true;
            case byte b:
                result = b;
                return true;
            case double d:
                result = (long)d;
                return Math.Abs(d - result) < 0.000001d;
            case float f:
                result = (long)f;
                return Math.Abs(f - result) < 0.000001f;
            default:
                if (long.TryParse(
                        Convert.ToString(value, CultureInfo.InvariantCulture),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var parsed))
                {
                    result = parsed;
                    return true;
                }

                result = 0;
                return false;
        }
    }
}
