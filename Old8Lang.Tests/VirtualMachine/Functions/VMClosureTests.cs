using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Functions;

/// <summary>
/// 虚拟机闭包测试
/// </summary>
/// <remarks>
/// 与解释器侧的 <c>Interpreter/Functions/ClosureTests.cs</c> 对应。此前虚拟机侧只有
/// lambda 的只读捕获用例，写回、跨层捕获、嵌套具名函数捕获都没有覆盖，因此
/// 「闭包写回外层局部变量无效」「嵌套具名函数把外层局部变量当全局名查」这两类缺陷
/// 一直没被发现。
///
/// 字节码模式原先按值快照捕获（MakeClosure 复制当前值），闭包与外层此后各看各的。
/// 现改为共享单元（<see cref="Old8Lang.Bytecode.Closures.UpValueCell"/>）：外层帧的槽位
/// 与闭包环境指向同一个盒子，双向可见，与解释器一致。
/// </remarks>
[Collection("Sequential")]
public class VMClosureTests
{
    private static string ExecuteVm(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            new VM(new Old8Lang.Bytecode.BytecodeCompiler().Compile(ast)).Execute();
            return writer.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static string ExecuteInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            ast.Run(interpreter.Manager);
            return writer.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #region 只读捕获

    [Fact]
    public void Closure_CapturesGlobalVariable()
    {
        var output = ExecuteVm("""
            factor <- 3
            scale <- (x) -> x * factor
            print(scale(7))
            """);

        Assert.Equal("21", output);
    }

    [Fact]
    public void Closure_CapturesOuterLocalVariable()
    {
        var output = ExecuteVm("""
            func makeAdder(n) {
                return (x) -> x + n
            }
            print(makeAdder(5)(3))
            """);

        Assert.Equal("8", output);
    }

    [Fact]
    public void Closure_CapturesMultipleVariables()
    {
        var output = ExecuteVm("""
            func makeCalculator(a, b) {
                return (op) -> {
                    if op == "add" {
                        return a + b
                    }
                    return a * b
                }
            }
            calc <- makeCalculator(10, 5)
            print(calc("add"))
            print(calc("mul"))
            """);

        Assert.Equal("1550", output);
    }

    [Fact]
    public void Closure_ReadsLatestValueOfCapturedVariable()
    {
        // 快照捕获会读到创建时的旧值，共享单元应读到最新值
        var output = ExecuteVm("""
            func make() {
                c <- 0
                read <- () -> c
                c <- 42
                print(read())
            }
            make()
            """);

        Assert.Equal("42", output);
    }

    #endregion

    #region 写回捕获

    [Fact]
    public void Closure_WritesBackToOuterLocal()
    {
        var output = ExecuteVm("""
            func make() {
                c <- 0
                inc <- () -> {
                    c <- c + 1
                    return c
                }
                print(inc())
                print(inc())
                print(c)
            }
            make()
            """);

        Assert.Equal("122", output);
    }

    [Fact]
    public void Closure_WritesBackToGlobal()
    {
        var output = ExecuteVm("""
            counter <- 0
            bump <- () -> { counter <- counter + 1 }
            bump()
            bump()
            print(counter)
            """);

        Assert.Equal("2", output);
    }

    [Fact]
    public void Closure_SiblingClosuresShareOneVariable()
    {
        var output = ExecuteVm("""
            func make() {
                c <- 0
                inc <- () -> { c <- c + 1 }
                read <- () -> c
                inc()
                inc()
                inc()
                print(read())
            }
            make()
            """);

        Assert.Equal("3", output);
    }

    [Fact]
    public void Closure_SeparateInvocationsDoNotShareState()
    {
        // 每次调用 make 都新建一个盒子，两个计数器互不影响
        var output = ExecuteVm("""
            func make() {
                c <- 0
                return () -> {
                    c <- c + 1
                    return c
                }
            }
            a <- make()
            b <- make()
            print(a())
            print(a())
            print(b())
            """);

        Assert.Equal("121", output);
    }

    [Fact]
    public void Closure_DeclaringNewLocal_DoesNotLeakOuterScope()
    {
        // 闭包内声明的自己的局部变量不应被当成外层变量
        var output = ExecuteVm("""
            func make() {
                return () -> {
                    local <- 7
                    return local * 2
                }
            }
            print(make()())
            """);

        Assert.Equal("14", output);
    }

    #endregion

    #region 嵌套具名函数

    [Fact]
    public void NestedNamedFunction_ReadsOuterLocal()
    {
        // 嵌套具名函数此前完全不做捕获分析，外层局部变量被当成全局名查找，
        // 运行时报「名称 'base' 未定义」且位置退化成 0:0
        var output = ExecuteVm("""
            func outer() {
                base <- 100
                func inner() -> int {
                    return base + 1
                }
                print(inner())
            }
            outer()
            """);

        Assert.Equal("101", output);
    }

    [Fact]
    public void NestedNamedFunction_WritesOuterLocal()
    {
        var output = ExecuteVm("""
            func outer() {
                total <- 0
                func bump() -> int {
                    total <- total + 10
                    return total
                }
                print(bump())
                print(bump())
                print(total)
            }
            outer()
            """);

        Assert.Equal("102020", output);
    }

    [Fact]
    public void NestedNamedFunction_AndSiblingLambda_ShareOuterLocal()
    {
        var output = ExecuteVm("""
            func outer() {
                total <- 0
                func addTen() { total <- total + 10 }
                func addFive() { total <- total + 5 }
                addTen()
                addFive()
                read <- () -> total
                print(read())
            }
            outer()
            """);

        Assert.Equal("15", output);
    }

    #endregion

    #region 跨层与循环

    [Fact]
    public void Closure_ThreeLevelsOfCapture()
    {
        var output = ExecuteVm("""
            level1 <- 1
            func createLevel1() {
                level2 <- 10
                return () -> {
                    func createLevel2() {
                        level3 <- 100
                        return () -> level1 + level2 + level3
                    }
                    return createLevel2()
                }
            }
            level1Func <- createLevel1()
            level2Func <- level1Func()
            print(level2Func())
            """);

        Assert.Equal("111", output);
    }

    [Fact]
    public void Closure_FactoryReturningDistinctClosures()
    {
        var output = ExecuteVm("""
            func getOperation(op) {
                if op == "double" {
                    return (x) -> x * 2
                }
                return (x) -> x * 3
            }
            doubler <- getOperation("double")
            tripler <- getOperation("triple")
            print(doubler(5))
            print(tripler(5))
            """);

        Assert.Equal("1015", output);
    }

    [Fact]
    public void Closure_CapturesLoopVariablePerIteration()
    {
        // 函数内的循环：每轮迭代为循环变量建立独立绑定
        var output = ExecuteVm("""
            func build() {
                fs <- {}
                for i <- 0, i < 3, i++ {
                    fs.Add(() -> i)
                }
                out <- ""
                for f in fs {
                    out <- out + f().ToStr()
                }
                return out
            }
            print(build())
            """);

        Assert.Equal("012", output);
    }

    [Fact]
    public void Closure_CapturesForInVariablePerIteration()
    {
        var output = ExecuteVm("""
            func build() {
                fs <- {}
                for i in [0~2] {
                    fs.Add(() -> i)
                }
                out <- ""
                for f in fs {
                    out <- out + f().ToStr()
                }
                return out
            }
            print(build())
            """);

        Assert.Equal("012", output);
    }

    [Fact]
    public void Closure_TopLevelForInVariable_PerIteration()
    {
        var output = ExecuteVm("""
            fs <- {}
            for i in [0~2] {
                fs.Add(() -> i)
            }
            result <- ""
            for f in fs {
                result <- result + f().ToStr()
            }
            print(result)
            """);

        Assert.Equal("012", output);
    }

    [Fact]
    public void Closure_TopLevelCStyleLoopVariable_SharesFinalValue()
    {
        // 已知分歧（本组用例之前就存在，与共享单元无关）：脚本顶层的 C 风格 for 循环
        // 变量被声明为**全局**变量，闭包读到的是同一张全局表里的最终值，因此得到 333；
        // 解释器同一段代码得到 012（每轮独立绑定）。此处显式钉住虚拟机现状，
        // 避免行为变化时无人察觉；两模式对齐需要另行处理顶层循环变量的作用域。
        var output = ExecuteVm("""
            fs <- {}
            for i <- 0, i < 3, i++ {
                fs.Add(() -> i)
            }
            result <- ""
            for f in fs {
                result <- result + f().ToStr()
            }
            print(result)
            """);

        Assert.Equal("333", output);
        Assert.Equal("012", ExecuteInterpreter("""
            fs <- {}
            for i <- 0, i < 3, i++ {
                fs.Add(() -> i)
            }
            result <- ""
            for f in fs {
                result <- result + f().ToStr()
            }
            print(result)
            """));
    }

    #endregion

    #region 与解释器一致性

    /// <remarks>
    /// 这里只收「两模式结论相同」的用例。解释器在 lambda 写回外层局部变量上自身不一致
    /// （lambda 写回不传回外层，同等的具名嵌套函数却传回），因此那类断言写在下面
    /// <see cref="Closure_LambdaWriteBack_IsVisibleToEnclosingScope"/> 等虚拟机专属用例里，
    /// 以共享单元的语义为准，而不是拿解释器当基准。
    /// </remarks>
    [Theory]
    [InlineData("""
        func makeAdder(n) {
            return (x) -> x + n
        }
        print(makeAdder(5)(3))
        """)]
    [InlineData("""
        func outer() {
            base <- 100
            func inner() -> int {
                return base + 1
            }
            print(inner())
        }
        outer()
        """)]
    [InlineData("""
        func outer() {
            total <- 0
            func bump() -> int {
                total <- total + 10
                return total
            }
            print(bump())
            print(total)
        }
        outer()
        """)]
    [InlineData("""
        counter <- 0
        bump <- () -> { counter <- counter + 1 }
        bump()
        print(counter)
        """)]
    [InlineData("""
        func build() {
            fs <- {}
            for i <- 0, i < 3, i++ {
                fs.Add(() -> i)
            }
            out <- ""
            for f in fs {
                out <- out + f().ToStr()
            }
            return out
        }
        print(build())
        """)]
    public void Closure_MatchesInterpreter(string code)
    {
        // 两种执行模式的闭包语义必须一致：模式之间行为漂移正是本组用例要消除的问题
        Assert.Equal(ExecuteInterpreter(code), ExecuteVm(code));
    }

    #endregion

    #region 解释器侧尚不一致的场景（以共享单元语义为准）

    [Fact]
    public void Closure_LambdaWriteBack_IsVisibleToEnclosingScope()
    {
        // 解释器对 lambda 的写回不传回外层（此处解释器会打印 1,2,0），而同等的具名嵌套
        // 函数却传回。虚拟机两种写法都传回，语义自洽。
        var output = ExecuteVm("""
            func make() {
                c <- 0
                inc <- () -> {
                    c <- c + 1
                    return c
                }
                print(inc())
                print(inc())
                print(c)
            }
            make()
            """);

        Assert.Equal("122", output);
    }

    [Fact]
    public void Closure_LambdaReadsLatestValue_AfterOuterAssignment()
    {
        // 解释器此处打印 0（读到赋值前的旧值），虚拟机读到最新值 99
        var output = ExecuteVm("""
            func make() {
                c <- 0
                read <- () -> c
                c <- 99
                print(read())
            }
            make()
            """);

        Assert.Equal("99", output);
    }

    [Fact]
    public void Closure_SiblingLambdaAndNamedFunction_ShareOuterLocal()
    {
        var output = ExecuteVm("""
            func outer() {
                total <- 0
                inc <- () -> { total <- total + 1 }
                func bump() { total <- total + 10 }
                inc()
                bump()
                print(total)
            }
            outer()
            """);

        Assert.Equal("11", output);
    }

    #endregion
}
