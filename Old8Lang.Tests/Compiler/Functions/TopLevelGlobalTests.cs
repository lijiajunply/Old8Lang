using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Functions;

/// <summary>
/// IL 模式下具名函数访问顶层变量的回归测试。
/// </summary>
/// <remarks>
/// 顶层变量默认是 <c>OldLangRun</c> 的局部变量；被函数体引用的变量必须提升为运行期全局槽位，
/// 否则编译函数体会报「变量未声明」，或者把顶层 LocalBuilder 错用到另一个动态方法里。
/// </remarks>
public class TopLevelGlobalTests
{
    private static LangInterpreter CompileAndRun(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();
        return interpreter;
    }

    private static int GetInt(LangInterpreter interpreter, string name)
    {
        var value = interpreter.Manager.GetValue(new LangId(name));
        Assert.IsType<IntLangValue>(value);
        return ((IntLangValue)value!).Value;
    }

    [Fact]
    public void NamedFunction_ReadsTopLevelVariable()
    {
        var interpreter = CompileAndRun("""
            g <- 10
            func f() -> int {
                return g + 1
            }
            result <- f()
            """);

        Assert.Equal(11, GetInt(interpreter, "result"));
    }

    [Fact]
    public void NamedFunction_WritesTopLevelVariable()
    {
        var interpreter = CompileAndRun("""
            counter <- 0
            func inc() -> void {
                counter <- counter + 1
            }
            inc()
            inc()
            """);

        Assert.Equal(2, GetInt(interpreter, "counter"));
    }

    [Fact]
    public void NamedFunction_CanReferenceVariableDeclaredLater()
    {
        var interpreter = CompileAndRun("""
            func f() -> int {
                return later + 1
            }
            later <- 41
            result <- f()
            """);

        Assert.Equal(42, GetInt(interpreter, "result"));
    }

    [Fact]
    public void MultipleFunctions_ShareTopLevelVariable()
    {
        var interpreter = CompileAndRun("""
            count <- 0
            func inc(step:int) -> void { count <- count + step }
            func dec(step:int) -> void { count <- count - step }
            inc(10)
            inc(5)
            dec(3)
            """);

        Assert.Equal(12, GetInt(interpreter, "count"));
    }

    [Fact]
    public void NamedFunction_ReadsStringAndDoubleGlobals()
    {
        var interpreter = CompileAndRun("""
            name <- "old8"
            pi <- 3.5
            func greet() -> string {
                return "hi " + name
            }
            func area(r:double) -> double {
                return pi * r * r
            }
            text <- greet()
            value <- area(2.0)
            """);

        Assert.Equal("hi old8", ((StringLangValue)interpreter.Manager.GetValue(new LangId("text"))!).Value);
        Assert.Equal(14.0, ((DoubleLangValue)interpreter.Manager.GetValue(new LangId("value"))!).Value, 5);
    }

    [Fact]
    public void TopLevelLambda_WritesTopLevelVariable()
    {
        var interpreter = CompileAndRun("""
            total <- 0
            add <- (v:int) -> { total <- total + v }
            add(4)
            add(6)
            """);

        Assert.Equal(10, GetInt(interpreter, "total"));
    }

    [Fact]
    public void TopLevelLambda_SeesLaterTopLevelAssignment()
    {
        var interpreter = CompileAndRun("""
            x <- 1
            f <- () -> x
            x <- 2
            result <- f()
            """);

        Assert.Equal(2, GetInt(interpreter, "result"));
    }

    [Fact]
    public void ClassMethod_ReadsAndWritesTopLevelVariable()
    {
        var interpreter = CompileAndRun("""
            shared <- 5
            class Box {
                func read() -> int { return shared }
                func write(v:int) -> void { shared <- v }
            }
            box <- Box()
            initial <- box.read()
            box.write(42)
            updated <- box.read()
            """);

        Assert.Equal(5, GetInt(interpreter, "initial"));
        Assert.Equal(42, GetInt(interpreter, "shared"));
        Assert.Equal(42, GetInt(interpreter, "updated"));
    }

    [Fact]
    public void ClassFieldInitializer_ReadsTopLevelVariable()
    {
        var interpreter = CompileAndRun("""
            shared <- 5
            class Counter {
                value <- shared + 1
                func get() -> int { return this.value }
            }
            counter <- Counter()
            result <- counter.get()
            """);

        Assert.Equal(6, GetInt(interpreter, "result"));
    }

    [Fact]
    public void LoopVariable_DoesNotOverwriteTopLevelVariable()
    {
        var interpreter = CompileAndRun("""
            i <- 100
            func readI() -> int { return i }
            for i <- 0, i < 3, i++ {
            }
            result <- readI()
            """);

        Assert.Equal(100, GetInt(interpreter, "result"));
    }

    [Fact]
    public void FunctionParameterShadowsTopLevelVariable()
    {
        var interpreter = CompileAndRun("""
            value <- 10
            func identity(value:int) -> int {
                return value
            }
            result <- identity(3)
            """);

        Assert.Equal(3, GetInt(interpreter, "result"));
        Assert.Equal(10, GetInt(interpreter, "value"));
    }

    [Fact]
    public void GlobalValues_AreResetForEachExecution()
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build("""
            counter <- 0
            func inc() -> void { counter <- counter + 1 }
            inc()
            """);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        compiledAction();
        Assert.Equal(1, GetInt(interpreter, "counter"));

        compiledAction();
        Assert.Equal(1, GetInt(interpreter, "counter"));
    }

    [Fact]
    public void ReadingUnassignedGlobal_ThrowsNameError()
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build("""
            func read() -> int {
                return later
            }
            read()
            later <- 1
            """);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Assert.Throws<Old8Exception>(() => compiledAction());
        Assert.Contains("later", exception.Message);
    }
}
