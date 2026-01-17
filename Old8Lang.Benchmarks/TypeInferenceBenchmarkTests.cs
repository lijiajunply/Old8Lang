using BenchmarkDotNet.Attributes;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

namespace Old8Lang.Benchmarks;

/// <summary>
/// 类型推断性能基准测试
/// 测量类型推断引擎的性能和内存占用
/// </summary>
[MemoryDiagnoser]
public class TypeInferenceBenchmarkTests
{
    private string SimpleCode = "";
    private string MediumCode = "";
    private string ComplexCode = "";
    private string GenericCode = "";

    [GlobalSetup]
    public void Setup()
    {
        // 简单代码：基本类型推断
        SimpleCode = @"func simple_test(a:int, b:int) -> int {
    c <- a + b
    d <- c * 2
    return d
}";

        // 中等复杂度：函数调用链
        MediumCode = @"func add(x:int, y:int) -> int {
    return x + y
}

func multiply(x:int, y:int) -> int {
    return x * y
}

func complex_calc(a:int, b:int, c:int) -> int {
    temp1 <- add(a, b)
    temp2 <- multiply(temp1, c)
    temp3 <- add(temp2, a)
    return temp3
}";

        // 复杂代码：类和递归
        ComplexCode = @"class Calculator {
    value:int

    func init(initial:int) -> void {
        this.value <- initial
    }

    func add(x:int) -> int {
        this.value <- this.value + x
        return this.value
    }

    func multiply(x:int) -> int {
        this.value <- this.value * x
        return this.value
    }
}

func factorial(n:int) -> int {
    if n <= 1 {
        return 1
    }
    return n * factorial(n - 1)
}

func main() -> int {
    calc <- Calculator(10)
    calc.add(5)
    calc.multiply(2)
    result <- factorial(5)
    return calc.value + result
}";

        // 泛型代码
        GenericCode = @"func identity<T>(value:T) -> T {
    return value
}

func swap<T, U>(a:T, b:U) -> {U, T} {
    return {b, a}
}

class Box<T> {
    data:T

    func init(value:T) -> void {
        this.data <- value
    }

    func get() -> T {
        return this.data
    }
}";
    }

    [Benchmark(Description = "Type Inference - Simple Code")]
    public void TypeInferenceSimple()
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(SimpleCode);

        // 创建 LocalManager 用于类型推断
        var localManager = new Old8Lang.Compiler.LocalManager
        {
            Interpreter = interpreter
        };

        var engine = new TypeInferenceEngine(localManager);
        engine.InferTypes(ast);
    }

    [Benchmark(Description = "Type Inference - Medium Code")]
    public void TypeInferenceMedium()
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(MediumCode);

        var localManager = new Old8Lang.Compiler.LocalManager
        {
            Interpreter = interpreter
        };

        var engine = new TypeInferenceEngine(localManager);
        engine.InferTypes(ast);
    }

    [Benchmark(Description = "Type Inference - Complex Code")]
    public void TypeInferenceComplex()
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(ComplexCode);

        var localManager = new Old8Lang.Compiler.LocalManager
        {
            Interpreter = interpreter
        };

        var engine = new TypeInferenceEngine(localManager);
        engine.InferTypes(ast);
    }

    [Benchmark(Description = "Type Inference - Generic Code")]
    public void TypeInferenceGeneric()
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(GenericCode);

        var localManager = new Old8Lang.Compiler.LocalManager
        {
            Interpreter = interpreter
        };

        var engine = new TypeInferenceEngine(localManager);
        engine.InferTypes(ast);
    }

    [Benchmark(Description = "Multiple Type Inferences")]
    public void MultipleTypeInferences()
    {
        for (int i = 0; i < 10; i++)
        {
            var interpreter = new LangInterpreter();
            var ast = interpreter.Build(SimpleCode);

            var localManager = new Old8Lang.Compiler.LocalManager
            {
                Interpreter = interpreter
            };

            var engine = new TypeInferenceEngine(localManager);
            engine.InferTypes(ast);
        }
    }
}
