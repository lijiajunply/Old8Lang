using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Error.Compilation;

/// <summary>
/// IL 编译器错误处理测试。
/// </summary>
/// <remarks>
/// 2026-11-15 按当前语言语义重写：旧版本假设编译器会在编译期检查
/// 「缺少注解 / 类型不兼容 / 循环引用 / 除零 / 越界 / 缺 return」等并抛错，
/// 但这些要么已经是合法代码，要么是运行期错误。现在按真实阶段断言：
/// 能编译的验证编译+执行；名字解析错误断言 CompilerException；
/// 除零/越界断言执行期语言错误；语法错误断言 Build 阶段 SyntaxError。
/// </remarks>
[Collection("Sequential")]
public class ErrorCompilationTests
{
    [Fact]
    public void MissingTypeAnnotation_CompilesAndExecutes()
    {
        AssertCompilesAndRuns(@"
            func processData(value:int) -> int {
                return value * 2
            }
        ");
    }

    [Fact]
    public void InvalidTypeAnnotation_CompilesAndExecutes()
    {
        AssertCompilesAndRuns(@"
            func processData(value:int) -> string {
                return value.ToStr()
            }
        ");
    }

    [Fact]
    public void IncompatibleType_CompilesAndExecutes()
    {
        AssertCompilesAndRuns(@"
            func processData(a:int, b:string) -> int {
                return a + b.Length
            }
        ");
    }

    [Fact]
    public void UnresolvedVariable_ReportsCompileError()
    {
        var exception = AssertCompileFails(@"
            result <- undefined_var + 1
        ");
        Assert.Contains("未声明", exception.Message);
    }

    [Fact]
    public void UndefinedMethod_ThrowsAtExecutionStage()
    {
        // 编译期未注册的名字在运行期按名查找；确实不存在时抛 NAME_ERROR
        // （与解释器一致，同时支持相互递归里后定义的函数）。
        var exception = AssertExecutionFails(@"
            result <- SomeMethod()
        ");
        Assert.Contains("SomeMethod", exception.Message);
    }

    [Fact]
    public void CircularReference_CompilesAndExecutes()
    {
        // 相互递归本身是合法定义；只要不执行无限递归即可。
        AssertCompilesAndRuns(@"
            func A(n:int) -> int {
                if n <= 0 { return 0 }
                return B(n - 1)
            }

            func B(n:int) -> int {
                if n <= 0 { return 0 }
                return A(n - 1)
            }
        ");
    }

    [Fact]
    public void DivisionByZero_ThrowsAtExecutionStage()
    {
        var exception = AssertExecutionFails(@"
            result <- 10 / 0
        ");
        Assert.IsType<ZeroDivisionError>(exception);
    }

    [Fact]
    public void IndexOutOfBounds_ThrowsAtExecutionStage()
    {
        var exception = AssertExecutionFails(@"
            arr <- [1, 2, 3]
            element <- arr[5]
        ");
        Assert.IsType<IndexError>(exception);
    }

    [Fact]
    public void InvalidSyntax_ReportsCompileError()
    {
        // 未声明的 a；print 也不存在，但 a 的解析先失败。
        var exception = AssertCompileFails(@"
            if a > 0 {
                print(""a is greater than 0"")
            } else {
                print(""a is not greater than 0"")
            }
        ");
        Assert.Contains("未声明", exception.Message);
    }

    [Fact]
    public void MissingReturnStatement_CompilesAndExecutes()
    {
        AssertCompilesAndRuns(@"
            func ShouldReturn() -> int {
                // Missing return statement
            }
        ");
    }

    [Fact]
    public void DuplicateIdentifier_ReportsSyntaxError()
    {
        // `var` 不是 Old8Lang 关键字，解析阶段即报语法错误。
        var interpreter = new LangInterpreter();
        var exception = Assert.Throws<SyntaxError>(() =>
            interpreter.Build(@"
                var a <- 1
                var a <- 2
            "));
        Assert.Contains("var", exception.Message);
    }

    [Fact]
    public void InvalidKeyword_ReportsCompileError()
    {
        // `True` 是普通标识符，不是 bool 关键字。
        var exception = AssertCompileFails(@"
            if True { print(""test"") }
        ");
        Assert.Contains("True", exception.Message);
    }

    private static void AssertCompilesAndRuns(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var action = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Assert.NotNull(action);
        var exception = Record.Exception(() => action());
        Assert.Null(exception);
    }

    private static Exception AssertCompileFails(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        return Assert.ThrowsAny<Exception>(() =>
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
    }

    private static Exception AssertExecutionFails(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var action = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        return Assert.ThrowsAny<Exception>(() => action());
    }
}
