using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode;
using Old8Lang.Error;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Types;

/// <summary>
/// 虚拟机下的 Assert 静态类测试
/// </summary>
/// <remarks>
/// 解释器把 <c>Assert</c> 注册为全局对象，断言失败用普通 <c>Exception</c> 表示；
/// 虚拟机改为在编译期改写成带限定名的原生调用，并把断言失败统一成
/// <see cref="AssertionError"/>，同时修正了两处相等语义问题（见各用例注释）。
/// </remarks>
[Collection("Sequential")]
public class VMStaticClassAssertTests
{
    private static BytecodeFile Compile(string code) => CompileHelper.CompileToBytecode(code);

    private static int ToInt(object? value) => value switch
    {
        int intValue => intValue,
        long longValue => (int)longValue,
        IntLangValue langValue => langValue.Value,
        _ => throw new InvalidOperationException($"期望整数结果，实际为 {value?.GetType().Name ?? "null"}")
    };

    [Fact]
    public void AssertEqual_PassingAssertion_Succeeds()
    {
        var vm = new VM(Compile("""
            Assert.Equal(3, 1 + 2)
            result <- 1
            """));

        vm.Execute();

        Assert.Equal(1, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void AssertEqual_MixedRepresentation_Succeeds()
    {
        // 42 是原始 CLR 值，await 的结果是 IntLangValue，两侧口径不同也必须判等
        var vm = new VM(Compile("""
            task <- Task.FromResult(42)
            Assert.Equal(42, await task)
            result <- 1
            """));

        vm.Execute();

        Assert.Equal(1, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void AssertEqual_ListValues_ComparesElementWise()
    {
        var vm = new VM(Compile("""
            Assert.Equal({1, 2, 3}, {1, 2, 3})
            result <- 1
            """));

        vm.Execute();

        Assert.Equal(1, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void AssertEqual_FailingAssertion_ThrowsAssertionError()
    {
        var vm = new VM(Compile("Assert.Equal(1, 2)"));

        var exception = Assert.Throws<AssertionError>(() => vm.Execute());

        Assert.Equal(AssertionError.ErrorCode, ((Old8Exception)exception).ErrorCode);
        Assert.Contains("期望值", exception.Message);
    }

    [Fact]
    public void AssertEqual_FailingAssertion_IsCatchableInLanguage()
    {
        var vm = new VM(Compile("""
            result <- ""
            try {
                Assert.Equal(1, 2)
            } catch (e) {
                result <- "caught"
            }
            """));

        vm.Execute();

        Assert.Equal("caught", vm.GetGlobalVariable("result"));
    }

    [Fact]
    public void AssertEqual_CustomMessage_IsUsedInFailure()
    {
        var vm = new VM(Compile("""Assert.Equal(1, 2, "自定义说明")"""));

        var exception = Assert.Throws<AssertionError>(() => vm.Execute());

        Assert.Contains("自定义说明", exception.Message);
    }

    [Fact]
    public void AssertNotEqual_SameClassDistinctInstances_Passes()
    {
        // 解释器的断言相等在遇到不认识的对象时会退回比较 ToDisplayString()，
        // 而虚拟机类实例的 ToString() 只含类名，两个不同实例会被判成相等（假阳性）。
        // 虚拟机改用自身的相等语义，这条用例锁住该差异。
        var vm = new VM(Compile("""
            class Person {
                public name:string <- ""
            }
            first <- Person()
            second <- Person()
            Assert.NotEqual(first, second)
            Assert.Equal(first, first)
            result <- 1
            """));

        vm.Execute();

        Assert.Equal(1, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void AssertEqual_SameClassDistinctInstances_FailsWithDistinguishableMessage()
    {
        var vm = new VM(Compile("""
            class Person {
                public name:string <- ""
            }
            Assert.Equal(Person(), Person())
            """));

        var exception = Assert.Throws<AssertionError>(() => vm.Execute());

        Assert.Contains("Person", exception.Message);
    }

    [Fact]
    public void AssertTrueAndFalse_BehaveAsExpected()
    {
        var vm = new VM(Compile("""
            Assert.True(1 < 2)
            Assert.False(1 > 2)
            result <- 1
            """));

        vm.Execute();

        Assert.Equal(1, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void AssertTrue_Failing_ThrowsWithoutDuplicatedPrefix()
    {
        var vm = new VM(Compile("Assert.True(false)"));

        var exception = Assert.Throws<AssertionError>(() => vm.Execute());

        // Old8Exception.Message 带错误码前缀，用包含式断言；关键是前缀没有被加两次
        Assert.Contains("断言失败: 期望为 true 但实际为 false", exception.Message);
        Assert.DoesNotContain("断言失败: 断言失败:", exception.Message);
    }

    [Fact]
    public void AssertNullAndNotNull_BehaveAsExpected()
    {
        var vm = new VM(Compile("""
            Assert.NotNull(1)
            value <- null
            Assert.Null(value)
            result <- 1
            """));

        vm.Execute();

        Assert.Equal(1, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void AssertComparisonsAndString_BehaveAsExpected()
    {
        var vm = new VM(Compile("""
            Assert.Greater(3, 2)
            Assert.GreaterOrEqual(2, 2)
            Assert.Less(1, 2)
            Assert.LessOrEqual(2, 2)
            Assert.Contains("hello", "ell")
            Assert.StartsWith("hello", "he")
            Assert.EndsWith("hello", "lo")
            result <- 1
            """));

        vm.Execute();

        Assert.Equal(1, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void AssertCollectionAssertions_BehaveAsExpected()
    {
        var vm = new VM(Compile("""
            values <- {1, 2, 3}
            Assert.Length(values, 3)
            Assert.NotEmpty(values)
            Assert.ContainsItem(values, 2)
            Assert.NotContainsItem(values, 9)
            empty <- {}
            Assert.Empty(empty)
            result <- 1
            """));

        vm.Execute();

        Assert.Equal(1, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void AssertInstanceOf_AcceptsInterpreterStyleTypeNames()
    {
        // 解释器的类型名是 Int / List / Dictionary 这类首字母大写口径，
        // 虚拟机的 GetValueTypeName 是小写，断言必须两种都认。
        var vm = new VM(Compile("""
            class Person {
                public name:string <- ""
            }
            Assert.InstanceOf(1, "Int")
            Assert.InstanceOf(1, "int")
            Assert.InstanceOf({1, 2}, "List")
            Assert.InstanceOf("text", "String")
            Assert.InstanceOf(Person(), "Person")
            Assert.NotInstanceOf(1, "String")
            result <- 1
            """));

        vm.Execute();

        Assert.Equal(1, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void AssertThrows_WithThrowingCallable_Passes()
    {
        var vm = new VM(Compile("""
            Assert.Throws(() -> {
                throw "boom"
            })
            result <- 1
            """));

        vm.Execute();

        Assert.Equal(1, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void AssertThrows_WithNonThrowingCallable_Fails()
    {
        var vm = new VM(Compile("Assert.Throws(() -> 1)"));

        var exception = Assert.Throws<AssertionError>(() => vm.Execute());

        Assert.Contains("期望抛出异常但未抛出", exception.Message);
    }

    [Fact]
    public void AssertNotThrows_WithNonThrowingCallable_Passes()
    {
        var vm = new VM(Compile("""
            Assert.NotThrows(() -> 1)
            result <- 1
            """));

        vm.Execute();

        Assert.Equal(1, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void AssertThrows_InnerAssertionFailure_IsNotSwallowed()
    {
        // Assert.Throws 只把“被断言的异常”算作预期异常；断言自身失败必须继续往外抛，
        // 否则 Assert.Throws(() -> Assert.True(false)) 会假通过。
        var vm = new VM(Compile("""
            Assert.Throws(() -> {
                Assert.True(false)
            })
            """));

        var exception = Assert.Throws<AssertionError>(() => vm.Execute());

        Assert.Contains("期望为 true 但实际为 false", exception.Message);
    }

    [Fact]
    public void AssertThrows_DoesNotLeakEvaluationStack()
    {
        // 被断言的调用在表达式求值中途抛异常时，求值栈必须回滚，
        // 否则调用方后续的取值会错位。
        var vm = new VM(Compile("""
            func run() -> int {
                Assert.Throws(() -> {
                    throw "boom"
                })
                return 42
            }
            result <- run()
            """));

        vm.Execute();

        Assert.Equal(42, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void Assert_InsideClosure_Works()
    {
        var vm = new VM(Compile("""
            check <- () -> Assert.Equal(1, 1)
            check()
            result <- 1
            """));

        vm.Execute();

        Assert.Equal(1, ToInt(vm.GetGlobalVariable("result")));
    }
}
