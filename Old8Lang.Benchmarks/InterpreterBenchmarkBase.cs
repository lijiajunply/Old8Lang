using BenchmarkDotNet.Attributes;
using Old8Lang.Interpreter;
using Old8Lang.AST.Statement;

namespace Old8Lang.Benchmarks;

/// <summary>
/// 解释器基准测试基类
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public abstract class InterpreterBenchmarkBase
{
    protected LangInterpreter Interpreter { get; private set; } = null!;

    [GlobalSetup]
    public virtual void Setup()
    {
        Interpreter = new LangInterpreter();
    }

    [GlobalCleanup]
    public virtual void Cleanup()
    {
        // Cleanup resources if needed
    }

    /// <summary>
    /// 执行 Old8Lang 代码并返回结果
    /// </summary>
    protected void ExecuteCode(string code)
    {
        BlockStatement ast = Interpreter.Build(code);
        ast.Run(Interpreter.Manager);
    }

    /// <summary>
    /// 仅解析代码（不执行）
    /// </summary>
    protected BlockStatement ParseCode(string code)
    {
        return Interpreter.Build(code);
    }
}
