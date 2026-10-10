using Old8Lang.Bytecode;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Integration;

/// <summary>
/// 虚拟机模式缺口修复的回归用例。
/// </summary>
/// <remarks>
/// 每个用例锁定一处「文档记录为不可用 / 与解释器不一致」的行为，覆盖：
/// 元组 <c>Count()</c>、集合直接打印、原生函数 void 与 null、<c>await</c> 结果与返回类型注解、
/// 异步生成器含 <c>await</c>、<c>async for-in</c> 多标识符解构、生成器 yield 集合、
/// <c>TypeLangValue</c> 实例方法、<c>TypeOf</c>/<c>GetType</c> 内建类型、
/// 运行期错误源码位置与除零错误、<c>TaskCompletionSource</c> / <c>CancellationTokenSource</c>。
/// 实测报告见 <c>Reports/2026-10-10-23-20-虚拟机模式缺口修复测试.md</c>。
/// </remarks>
[Collection("Sequential")]
public class VMGapRegressionTests
{
    private static string ExecuteVm(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var bytecodeFile = new BytecodeCompiler().Compile(ast);

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            new Bytecode.VM.VirtualMachine(bytecodeFile).Execute();
            return writer.ToString().Trim().Replace("\r\n", "\n");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static Old8Exception ExecuteVmExpectingOld8Error(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var bytecodeFile = new BytecodeCompiler().Compile(ast);
        return Assert.ThrowsAny<Old8Exception>(() => new Bytecode.VM.VirtualMachine(bytecodeFile).Execute());
    }

    // ===== 缺口2：元组 Count() =====

    [Fact]
    public void TupleCount_WorksForAnyArity()
    {
        var output = ExecuteVm("""
            PrintLine((1, 2).Count().ToStr())
            PrintLine((1, 2, 3).Count().ToStr())
            PrintLine((1, 2, 3, 4).Count().ToStr())
            PrintLine({1, 2, 3}.Count().ToStr())
            PrintLine([1, 2, 3].Count().ToStr())
            """);

        Assert.Equal("2\n3\n4\n3\n3", output);
    }

    // ===== 缺口3：集合直接打印 =====

    [Fact]
    public void CollectionDisplay_MatchesToStr()
    {
        var output = ExecuteVm("""
            l <- {1, 2, 3}
            d <- {"k": "v"}
            a <- [1, 2, 3]
            t <- (1, 2, 3)
            PrintLine(l)
            PrintLine(d)
            PrintLine(a)
            PrintLine(t)
            PrintLine({"a", "b"}.ToStr())
            """);

        Assert.Equal("[1, 2, 3]\n{\"k\": \"v\"}\n[1, 2, 3]\n(1, 2, 3)\n[\"a\", \"b\"]", output);
    }

    // ===== 缺口4：原生函数无返回值 != null =====

    [Fact]
    public void NativeVoidFunction_IsNotEqualToNull()
    {
        var output = ExecuteVm("""
            r <- Sleep(1)
            PrintLine((r == null).ToStr())
            d <- {"a": 1}
            c <- d.Clear()
            PrintLine((c == null).ToStr())
            """);

        Assert.Equal("false\nfalse", output);
    }

    // ===== 缺口5：await 结果 + 返回类型注解 =====

    [Fact]
    public void AwaitResult_SatisfiesReturnTypeAnnotation()
    {
        var output = ExecuteVm("""
            async func gInt() -> int {
                return 5
            }
            async func gList() -> list {
                return {1, 2, 3}
            }
            async func fInt() -> int {
                return await gInt()
            }
            async func fList() -> list {
                return await gList()
            }
            async func run() -> object {
                PrintLine((await fInt()).ToStr())
                PrintLine((await fList()).ToStr())
                return null
            }
            await run()
            """);

        Assert.Equal("5\n[1, 2, 3]", output);
    }

    // ===== 缺口1：异步生成器含 await、async for-in 多标识符 =====

    [Fact]
    public void AsyncGenerator_WithAwait_YieldsValues()
    {
        var output = ExecuteVm("""
            async func delayed() {
                i <- 1
                while i <= 3 {
                    await Task.Delay(1)
                    yield i
                    i <- i + 1
                }
            }
            async for item in delayed() {
                PrintLine(item.ToStr())
            }
            """);

        Assert.Equal("1\n2\n3", output);
    }

    [Fact]
    public void AsyncForIn_MultiIdentifier_DestructuresTuple()
    {
        var output = ExecuteVm("""
            async func pairs() {
                yield ("a", 1)
                yield ("b", 2)
            }
            async for key, value in pairs() {
                PrintLine(key + "=" + value.ToStr())
            }
            """);

        Assert.Equal("a=1\nb=2", output);
    }

    [Fact]
    public void SyncGenerator_YieldingCollection_YieldsRawValue()
    {
        var output = ExecuteVm("""
            func gen() {
                yield {1, 2}
            }
            for x in gen() {
                PrintLine(x.ToStr())
            }
            """);

        Assert.Equal("[1, 2]", output);
    }

    // ===== 缺口8：TypeLangValue 实例方法 =====

    [Fact]
    public void TypeLangValue_Methods_WorkInVm()
    {
        var output = ExecuteVm("""
            interface IDrawable {
                func draw()
            }
            class Animal {
                public species:string <- "unknown"
            }
            class Dog extends Animal implements IDrawable {
                public breed:string <- "unknown"
                public func bark() {}
                public func draw() {}
            }
            t <- GetType("Dog")
            PrintLine("IsClass=" + t.IsClass().ToStr())
            PrintLine("IsInterface=" + t.IsInterface().ToStr())
            PrintLine("IsPrimitive=" + t.IsPrimitive().ToStr())
            PrintLine("IsGeneric=" + t.IsGeneric().ToStr())
            PrintLine("Base=" + t.GetBaseType().ToStr())
            PrintLine("Interfaces=" + t.GetInterfaces().ToStr())
            PrintLine("Assignable(Animal)=" + t.IsAssignableFrom(GetType("Animal")).ToStr())
            PrintLine("Assignable(Dog)=" + t.IsAssignableFrom(GetType("Dog")).ToStr())
            PrintLine("HasSpecies=" + t.GetFieldNames().ToStr().Contains("species").ToStr())
            PrintLine("HasBark=" + t.GetMethodNames().ToStr().Contains("bark").ToStr())
            """);

        Assert.Equal(
            "IsClass=true\nIsInterface=false\nIsPrimitive=false\nIsGeneric=false\n" +
            "Base=Animal\nInterfaces=[IDrawable]\nAssignable(Animal)=false\nAssignable(Dog)=true\n" +
            "HasSpecies=true\nHasBark=true",
            output);
    }

    [Fact]
    public void TypeOf_Class_IsGeneric_ForSpecializedGenericClass()
    {
        var output = ExecuteVm("""
            class Box<T> {
                public v:T? <- null
            }
            b <- Box<int>()
            PrintLine(TypeOf(b).IsGeneric().ToStr())
            """);

        Assert.Equal("true", output);
    }

    // ===== 缺口9：TypeOf / GetType 内建类型 =====

    [Fact]
    public void TypeOf_And_GetType_CoverBuiltinTypes()
    {
        var output = ExecuteVm("""
            PrintLine(TypeOf({1, 2}).ToStr())
            PrintLine(TypeOf(1).ToStr())
            PrintLine(TypeOf(1.5).ToStr())
            PrintLine(TypeOf(true).ToStr())
            PrintLine(TypeOf(null).ToStr())
            PrintLine(GetType("int").ToStr())
            PrintLine(GetType("int").IsPrimitive().ToStr())
            PrintLine(GetType("list").ToStr())
            """);

        Assert.Equal("List\nInt\nDouble\nBool\nNull\nInt\ntrue\nList", output);
    }

    // ===== 缺口6：运行期错误位置与除零错误 =====

    [Fact]
    public void RuntimeError_CarriesSourcePosition()
    {
        var error = ExecuteVmExpectingOld8Error("""
            func add(a:int, b:int) -> int {
                return a + b
            }
            PrintLine(add("x", 1).ToStr())
            """);

        Assert.Equal(4, error.Position.Line);
    }

    [Fact]
    public void ZeroDivision_IsLanguageErrorWithPosition()
    {
        var error = ExecuteVmExpectingOld8Error("""
            func div(a:int, b:int) -> int {
                return a / b
            }
            PrintLine(div(10, 0).ToStr())
            """);

        Assert.Equal("ZERO_DIVISION_ERROR", error.ErrorCode);
        // 除法指令在函数体第 2 行
        Assert.Equal(2, error.Position.Line);
    }

    // ===== 缺口7：TaskCompletionSource / CancellationTokenSource =====

    [Fact]
    public void TaskCompletionSource_And_CancellationTokenSource_WorkInVm()
    {
        var output = ExecuteVm("""
            tcs <- TaskCompletionSource()
            PrintLine(tcs.TrySetResult(42).ToStr())
            PrintLine(tcs.TrySetResult(43).ToStr())
            PrintLine(tcs.Task.ToString())

            cts <- CancellationTokenSource()
            PrintLine(cts.IsCancellationRequested.ToStr())
            cts.Cancel()
            PrintLine(cts.IsCancellationRequested.ToStr())
            cts.Dispose()
            """);

        Assert.Equal("true\nfalse\nTask(Completed: 42)\nfalse\ntrue", output);
    }
}
