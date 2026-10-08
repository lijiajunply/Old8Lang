using Old8Lang.Error;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Functions;

/// <summary>
/// 虚拟机函数装饰器测试
/// </summary>
/// <remarks>
/// 装饰器的语义是：定义函数时把原函数交给装饰器，再把装饰器的返回值写回同名绑定，
/// 之后按名字调用拿到的就是包装后的函数。虚拟机里有两处会让它失效：
/// 一是包装函数捕获不到“被装饰的函数”这个外部变量（闭包捕获分析漏掉了调用表达式里的标识符），
/// 二是调用点按函数索引直达函数体、绕过运行期被改写过的全局绑定。
/// 本文件覆盖这两条路径以及常见的装饰器写法。
/// </remarks>
public class VMDecoratorTests
{
    private static object? Execute(string code)
    {
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();
        return vm.GetGlobalVariable("result");
    }

    [Fact]
    public void Decorator_Identity_ReturnsOriginalFunction()
    {
        var code = @"
            func identity(f) {
                return f
            }

            @identity
            func test(x:int) -> int {
                return x * 2
            }

            result <- test(5)
        ";

        Assert.Equal(10, Execute(code));
    }

    [Fact]
    public void Decorator_WrappingClosure_ModifiesResult()
    {
        var code = @"
            func double(f) {
                wrapper <- (x) -> {
                    result <- f(x)
                    return result * 2
                }
                return wrapper
            }

            @double
            func getValue(x:int) -> int {
                return x
            }

            result <- getValue(5)
        ";

        Assert.Equal(10, Execute(code));
    }

    [Fact]
    public void Decorator_WithArguments_AppliesCorrectly()
    {
        var code = @"
            func multiply(factor) {
                return (f) -> {
                    wrapper <- (x) -> {
                        result <- f(x)
                        return result * factor
                    }
                    return wrapper
                }
            }

            @multiply(factor: 3)
            func getValue(x:int) -> int {
                return x
            }

            result <- getValue(5)
        ";

        Assert.Equal(15, Execute(code));
    }

    [Fact]
    public void Decorator_WithExpressionArguments_EvaluatesCorrectly()
    {
        var code = @"
            func multiply(factor) {
                return (f) -> {
                    wrapper <- (x) -> {
                        return f(x) * factor
                    }
                    return wrapper
                }
            }

            @multiply(factor: 2 + 3)
            func getValue(x:int) -> int {
                return x
            }

            result <- getValue(4)
        ";

        Assert.Equal(20, Execute(code));
    }

    [Fact]
    public void Decorator_MultipleDecorators_AppliesBottomUp()
    {
        var code = @"
            func addOne(f) {
                wrapper <- (x) -> {
                    result <- f(x)
                    return result + 1
                }
                return wrapper
            }

            func double(f) {
                wrapper <- (x) -> {
                    result <- f(x)
                    return result * 2
                }
                return wrapper
            }

            @double
            @addOne
            func getValue(x:int) -> int {
                return x
            }

            result <- getValue(5)
        ";

        Assert.Equal(12, Execute(code));
    }

    [Fact]
    public void Decorator_ChainOfFive_AppliesCorrectly()
    {
        var code = @"
            func add1(f) { return (x) -> f(x) + 1 }
            func add2(f) { return (x) -> f(x) + 2 }
            func add3(f) { return (x) -> f(x) + 3 }
            func add4(f) { return (x) -> f(x) + 4 }
            func add5(f) { return (x) -> f(x) + 5 }

            @add5
            @add4
            @add3
            @add2
            @add1
            func getValue(x:int) -> int {
                return x
            }

            result <- getValue(0)
        ";

        Assert.Equal(15, Execute(code));
    }

    [Fact]
    public void Decorator_WithMultipleParameters_WorksCorrectly()
    {
        var code = @"
            func log(f) {
                wrapper <- (a, b) -> {
                    return f(a, b)
                }
                return wrapper
            }

            @log
            func add(a:int, b:int) -> int {
                return a + b
            }

            result <- add(3, 5)
        ";

        Assert.Equal(8, Execute(code));
    }

    [Fact]
    public void Decorator_WithNoParameters_WorksCorrectly()
    {
        var code = @"
            func decorator(f) {
                wrapper <- () -> {
                    result <- f()
                    return result + 10
                }
                return wrapper
            }

            @decorator
            func getValue() -> int {
                return 32
            }

            result <- getValue()
        ";

        Assert.Equal(42, Execute(code));
    }

    [Fact]
    public void Decorator_ChangingReturnType_WorksCorrectly()
    {
        var code = @"
            func stringify(f) {
                wrapper <- (x) -> {
                    result <- f(x)
                    return result.ToStr() + ""!""
                }
                return wrapper
            }

            @stringify
            func getValue(x:int) -> int {
                return x * 2
            }

            result <- getValue(5)
        ";

        Assert.Equal("10!", Execute(code));
    }

    [Fact]
    public void Decorator_CachingResults_CallsOriginalOnlyOncePerKey()
    {
        var code = @"
            callCount <- 0

            func cache(f) {
                cacheDict <- dict()
                wrapper <- (n) -> {
                    key <- n.ToStr()
                    if cacheDict.ContainsKey(key) {
                        return cacheDict[key]
                    }
                    callCount <- callCount + 1
                    result <- f(n)
                    cacheDict[key] <- result
                    return result
                }
                return wrapper
            }

            @cache
            func expensiveOp(n:int) -> int {
                return n * n
            }

            r1 <- expensiveOp(5)
            r2 <- expensiveOp(5)
            r3 <- expensiveOp(10)
            result <- callCount
        ";

        // 5 命中缓存，只有 5 和 10 真正算过
        Assert.Equal(2, Execute(code));
    }

    [Fact]
    public void Decorator_AccessingClosureVariable_WorksCorrectly()
    {
        var code = @"
            multiplier <- 10

            func useMultiplier(f) {
                wrapper <- (x) -> {
                    result <- f(x)
                    return result * multiplier
                }
                return wrapper
            }

            @useMultiplier
            func getValue(x:int) -> int {
                return x
            }

            result <- getValue(5)
        ";

        Assert.Equal(50, Execute(code));
    }

    [Fact]
    public void Decorator_OnRecursiveFunction_WorksCorrectly()
    {
        var code = @"
            func decorator(f) {
                return f
            }

            @decorator
            func factorial(n:int) -> int {
                if n <= 1 {
                    return 1
                }
                return n * factorial(n - 1)
            }

            result <- factorial(5)
        ";

        Assert.Equal(120, Execute(code));
    }

    [Fact]
    public void Decorator_KeepsFunctionName_StillCallable()
    {
        var code = @"
            func decorator(f) {
                return f
            }

            @decorator
            func myFunction(x:int) -> int {
                return x * 2
            }

            result <- myFunction(5)
        ";

        Assert.Equal(10, Execute(code));
    }

    [Fact]
    public void DecoratedFunction_CalledFromAnotherFunction_UsesDecoratedBinding()
    {
        var code = @"
            func twice(f) {
                return (x) -> f(x) * 2
            }

            @twice
            func getValue(x:int) -> int {
                return x
            }

            func caller(x:int) -> int {
                return getValue(x) + 1
            }

            result <- caller(5)
        ";

        // getValue(5) = 10（已装饰），再 +1
        Assert.Equal(11, Execute(code));
    }

    [Fact]
    public void DecoratedFunction_CalledFromClosure_UsesDecoratedBinding()
    {
        var code = @"
            func twice(f) {
                return (x) -> f(x) * 2
            }

            @twice
            func getValue(x:int) -> int {
                return x
            }

            func makeCaller() {
                return (x) -> getValue(x) + 1
            }

            result <- makeCaller()(5)
        ";

        Assert.Equal(11, Execute(code));
    }

    [Fact]
    public void DecoratedFunction_PassedAsValue_KeepsWrapper()
    {
        var code = @"
            func twice(f) {
                return (x) -> f(x) * 2
            }

            func apply(f, x) {
                return f(x)
            }

            @twice
            func getValue(x:int) -> int {
                return x
            }

            result <- apply(getValue, 5)
        ";

        Assert.Equal(10, Execute(code));
    }

    [Fact]
    public void Decorator_MultipleFunctions_WorkIndependently()
    {
        var code = @"
            func double(f) {
                return (x) -> f(x) * 2
            }

            func triple(f) {
                return (x) -> f(x) * 3
            }

            @double
            func func1(x:int) -> int {
                return x
            }

            @triple
            func func2(x:int) -> int {
                return x
            }

            result <- func1(5) + func2(5)
        ";

        Assert.Equal(25, Execute(code));
    }

    [Fact]
    public void Decorator_NotReturningFunction_ThrowsOnCall()
    {
        var code = @"
            func badDecorator(f) {
                return 123
            }

            @badDecorator
            func test(x:int) -> int {
                return x
            }

            result <- test(5)
        ";

        Assert.Throws<TypeError>(() => Execute(code));
    }

    [Fact]
    public void Decorator_WithArgumentsNotReturningFunction_ThrowsOnCall()
    {
        var code = @"
            func badDecorator(timeout) {
                return 123
            }

            @badDecorator(timeout: 60)
            func test(x:int) -> int {
                return x
            }

            result <- test(5)
        ";

        Assert.Throws<TypeError>(() => Execute(code));
    }

    [Fact]
    public void Decorator_Undefined_ThrowsNameError()
    {
        var code = @"
            @nonexistent
            func test(x:int) -> int {
                return x
            }

            result <- test(5)
        ";

        Assert.Throws<NameError>(() => Execute(code));
    }

    [Fact]
    public void Decorator_OnNestedFunction_WorksCorrectly()
    {
        var code = @"
            func twice(f) {
                return (x) -> f(x) * 2
            }

            func outer(x:int) -> int {
                @twice
                func inner(y:int) -> int {
                    return y + 1
                }
                return inner(x)
            }

            result <- outer(5)
        ";

        // inner(5) = 6，装饰后 12
        Assert.Equal(12, Execute(code));
    }

    [Fact]
    public void Decorator_OnAsyncFunction_ReportsUnsupported()
    {
        var code = @"
            func twice(f) {
                return (x) -> f(x) * 2
            }

            @twice
            async func fetch(x:int) -> int {
                return x
            }

            result <- await fetch(10)
        ";

        // 异步函数在虚拟机里不走全局绑定，装饰器无法生效，
        // 与其静默丢弃装饰器，不如明确报不支持。
        Assert.Throws<VmUnsupportedError>(() => Execute(code));
    }

    [Fact]
    public void DecoratedFunction_SerializedAndReloaded_KeepsDecorator()
    {
        var code = @"
            func twice(f) {
                return (x) -> f(x) * 2
            }

            @twice
            func getValue(x:int) -> int {
                return x
            }

            result <- getValue(5)
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);

        var path = Path.Combine(Path.GetTempPath(), $"o8decorator_{Guid.NewGuid():N}.o8bc");
        try
        {
            bytecodeFile.SaveToFile(path);
            var reloaded = Old8Lang.Bytecode.BytecodeFile.LoadFromFile(path);

            var vm = new VM(reloaded);
            vm.Execute();

            Assert.Equal(10, vm.GetGlobalVariable("result"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
