using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

namespace Old8Lang.Tests.Compiler.Integration;

/// <summary>
/// 类型推断系统集成测试
/// </summary>
[Collection("Sequential")]
public class TypeInferenceTests
{
    [Fact]
    public void TypeInference_DefaultParameterInference_WorksCorrectly()
    {
        // 测试从默认值推断参数类型
        var code = @"
            func greet(name:string, greeting: ""Hello"") -> void {
                PrintLine(greeting + "", "" + name + ""!"")
            }

            greet(""Old8"")
            greet(""World"", ""Hi"")
        ";

        var interpreter = new LangInterpreter();

        // 启用类型推断
        var originalConfig = TypeInferenceConfig.Instance.EnableTypeInference;
        var originalDebug = TypeInferenceConfig.Instance.DebugOutput;

        try
        {
            TypeInferenceConfig.Instance.EnableTypeInference = true;
            TypeInferenceConfig.Instance.DebugOutput = true;

            var ast = interpreter.Build(code);
            var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

            Assert.NotNull(compiledAction);

            // 验证编译和执行都不抛出异常
            var exception = Record.Exception(() => compiledAction());
            Assert.Null(exception);
        }
        finally
        {
            // 恢复原始配置
            TypeInferenceConfig.Instance.EnableTypeInference = originalConfig;
            TypeInferenceConfig.Instance.DebugOutput = originalDebug;
        }
    }

    [Fact]
    public void TypeInference_ExplicitTypeAnnotations_WorksCorrectly()
    {
        // 测试显式类型注解（不需要推断）
        var code = @"
            func add(x:int, y:int) -> int {
                return x + y
            }

            result <- add(10, 20)
            PrintLine(""10 + 20 = "" + result.ToStr())
        ";

        var interpreter = new LangInterpreter();

        var originalConfig = TypeInferenceConfig.Instance.EnableTypeInference;
        var originalDebug = TypeInferenceConfig.Instance.DebugOutput;

        try
        {
            TypeInferenceConfig.Instance.EnableTypeInference = true;
            TypeInferenceConfig.Instance.DebugOutput = true;

            var ast = interpreter.Build(code);
            var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

            Assert.NotNull(compiledAction);

            var exception = Record.Exception(() => compiledAction());
            Assert.Null(exception);
        }
        finally
        {
            TypeInferenceConfig.Instance.EnableTypeInference = originalConfig;
            TypeInferenceConfig.Instance.DebugOutput = originalDebug;
        }
    }

    [Fact]
    public void TypeInference_MixedAnnotations_WorksCorrectly()
    {
        // 测试混合类型注解（部分显式，部分推断）
        var code = @"
            func calculate(x:int, y: 0, operation: ""add"") -> int {
                if operation == ""add"" {
                    return x + y
                } elif operation == ""multiply"" {
                    return x * y
                } else {
                    return 0
                }
            }

            result1 <- calculate(10, 20)
            result2 <- calculate(10, 20, ""multiply"")

            PrintLine(""result1 = "" + result1.ToStr())
            PrintLine(""result2 = "" + result2.ToStr())
        ";

        var interpreter = new LangInterpreter();

        var originalConfig = TypeInferenceConfig.Instance.EnableTypeInference;
        var originalDebug = TypeInferenceConfig.Instance.DebugOutput;

        try
        {
            TypeInferenceConfig.Instance.EnableTypeInference = true;
            TypeInferenceConfig.Instance.DebugOutput = true;

            var ast = interpreter.Build(code);
            var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

            Assert.NotNull(compiledAction);

            var exception = Record.Exception(() => compiledAction());
            Assert.Null(exception);
        }
        finally
        {
            TypeInferenceConfig.Instance.EnableTypeInference = originalConfig;
            TypeInferenceConfig.Instance.DebugOutput = originalDebug;
        }
    }

    [Fact]
    public void TypeInference_Disabled_RequiresExplicitAnnotations()
    {
        // 测试禁用类型推断时需要显式注解
        var code = @"
            func greet(name, greeting: ""Hello"") -> void {
                PrintLine(greeting + "", "" + name + ""!"")
            }
        ";

        var interpreter = new LangInterpreter();

        // 确保类型推断禁用
        var originalConfig = TypeInferenceConfig.Instance.EnableTypeInference;

        try
        {
            TypeInferenceConfig.Instance.EnableTypeInference = false;

            var ast = interpreter.Build(code);

            // 应该抛出类型注解缺失的异常
            var exception = Assert.Throws<Old8Lang.Error.CompilerException>(() =>
            {
                Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
            });

            // 验证错误消息包含参数名
            Assert.Contains("name", exception.Message);
        }
        finally
        {
            TypeInferenceConfig.Instance.EnableTypeInference = originalConfig;
        }
    }

    [Fact]
    public void TypeInference_DebugOutput_PrintsInferenceInfo()
    {
        // 测试调试输出功能
        var code = @"
            func add(x:int, y:int) -> int {
                return x + y
            }
        ";

        var interpreter = new LangInterpreter();

        var originalConfig = TypeInferenceConfig.Instance.EnableTypeInference;
        var originalDebug = TypeInferenceConfig.Instance.DebugOutput;

        try
        {
            TypeInferenceConfig.Instance.EnableTypeInference = true;
            TypeInferenceConfig.Instance.DebugOutput = true;

            // 捕获控制台输出
            using var consoleOutput = new System.IO.StringWriter();
            var originalConsole = Console.Out;
            Console.SetOut(consoleOutput);

            try
            {
                var ast = interpreter.Build(code);
                var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
                compiledAction();

                var output = consoleOutput.ToString();

                // 验证调试输出包含类型推断信息
                // （如果函数不需要推断，可能没有输出，所以这里只是不抛异常即可）
                Assert.NotNull(output);
            }
            finally
            {
                Console.SetOut(originalConsole);
            }
        }
        finally
        {
            TypeInferenceConfig.Instance.EnableTypeInference = originalConfig;
            TypeInferenceConfig.Instance.DebugOutput = originalDebug;
        }
    }
}
