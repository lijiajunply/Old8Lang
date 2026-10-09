using System.Runtime.InteropServices;
using Old8Lang.Bytecode;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Extern;

/// <summary>
/// 虚拟机 extern（原生库导入）测试
/// </summary>
/// <remarks>
/// 与解释器侧的 <c>Interpreter/Modules/Extern/NativeDllExternTests.cs</c> 对应。
/// 此前虚拟机侧只有 3 个用例，只覆盖了「函数能调通」，调用约定、批量/单函数导入、
/// 缺库缺符号的报错分支、参数与返回类型转换、重复调用的委托缓存都没有覆盖。
///
/// 调用约定（cdecl/stdcall/winapi）在三个平台上都能解析并调用 libc 的函数，
/// 因此这里全部写成跨平台用例，不用「非 Windows 就提前 return」那种会让用例
/// 在 macOS/Linux 上静默通过、从而制造覆盖率假象的写法。
/// </remarks>
[Collection("Sequential")]
public class VMExternConventionTests
{
    /// <summary>
    /// 当前平台的 C 标准库名称
    /// </summary>
    private static string CLibrary => OperatingSystem.IsWindows() ? "msvcrt.dll"
        : OperatingSystem.IsMacOS() ? "libSystem.dylib"
        : "libc.so.6";

    private static string RunVm(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            new VM(new BytecodeCompiler().Compile(ast)).Execute();
            return writer.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #region 调用约定

    [Theory]
    [InlineData("cdecl")]
    [InlineData("stdcall")]
    [InlineData("winapi")]
    public void ExternCallingConvention_BlockForm_InvokesCorrectly(string convention)
    {
        var output = RunVm($$"""
            extern "{{CLibrary}}" {{convention}} {
                func abs(x:int) -> int
            }
            print(abs(-9))
            """);

        Assert.Equal("9", output);
    }

    [Theory]
    [InlineData("cdecl")]
    [InlineData("stdcall")]
    [InlineData("winapi")]
    public void ExternCallingConvention_SingleFunctionForm_InvokesCorrectly(string convention)
    {
        var output = RunVm($$"""
            extern "{{CLibrary}}" {{convention}} func abs(x:int) -> int
            print(abs(-11))
            """);

        Assert.Equal("11", output);
    }

    [Fact]
    public void ExternPerFunctionConvention_OverridesBlockDefault()
    {
        // 块级给 cdecl，函数级显式写 stdcall，两者都必须能调通
        var output = RunVm($$"""
            extern "{{CLibrary}}" cdecl {
                stdcall func abs(x:int) -> int
            }
            print(abs(-13))
            """);

        Assert.Equal("13", output);
    }

    #endregion

    #region 导入形式

    [Fact]
    public void ExternBatchImport_BindsEveryFunction()
    {
        var output = RunVm($$"""
            extern "{{CLibrary}}" {
                func abs(x:int) -> int
                func labs(x:long) -> long
            }
            print(abs(-4))
            print(labs(-5))
            """);

        Assert.Equal("45", output);
    }

    [Fact]
    public void ExternBlock_WithAlias_BindsAliasName()
    {
        var output = RunVm($$"""
            extern "{{CLibrary}}" {
                func abs(x:int) -> int as absolute
            }
            print(absolute(-6))
            """);

        Assert.Equal("6", output);
    }

    [Fact]
    public void ExternMultipleBlocks_AllRemainCallable()
    {
        var output = RunVm($$"""
            extern "{{CLibrary}}" {
                func abs(x:int) -> int
            }
            extern "{{CLibrary}}" {
                func labs(x:long) -> long
            }
            print(abs(-2))
            print(labs(-3))
            """);

        Assert.Equal("23", output);
    }

    #endregion

    #region 类型转换

    [Fact]
    public void ExternDoubleReturnAndArgument_ConvertsBothWays()
    {
        // cos(0.0) == 1.0，覆盖 double 参数的封送与 double 返回值的转换
        var output = RunVm($$"""
            extern "{{CLibrary}}" {
                func cos(x:double) -> double
            }
            print(cos(0.0))
            """);

        Assert.Equal("1", output);
    }

    [Fact]
    public void ExternLongArgumentAndReturn_ConvertsCorrectly()
    {
        var output = RunVm($$"""
            extern "{{CLibrary}}" {
                func labs(x:long) -> long
            }
            print(labs(-1234567890123))
            """);

        Assert.Equal("1234567890123", output);
    }

    #endregion

    #region 报错分支

    [Fact]
    public void ExternMissingLibrary_ReportsIoError()
    {
        // 报错必须指向「加载不到库」，而不是退化成「名称未定义」之类的无关信息
        var exception = Assert.Throws<IOError>(() => RunVm("""
            extern "no_such_library_old8lang_test.dll" {
                func abs(x:int) -> int
            }
            print(abs(-1))
            """));

        Assert.Contains("无法加载 DLL", exception.Message);
        Assert.Contains("no_such_library_old8lang_test.dll", exception.Message);
    }

    [Fact]
    public void ExternMissingSymbol_ReportsMethodNotFound()
    {
        var exception = Assert.Throws<MethodNotFoundError>(() => RunVm($$"""
            extern "{{CLibrary}}" {
                func no_such_symbol_old8lang_test(x:int) -> int
            }
            print(no_such_symbol_old8lang_test(-1))
            """));

        Assert.Contains("no_such_symbol_old8lang_test", exception.Message);
    }

    #endregion

    #region 重复调用

    [Fact]
    public void ExternRepeatedCalls_ReuseCachedDelegate()
    {
        // 同一 extern 函数多次调用走的是缓存的函数指针与委托，结果必须一致
        var output = RunVm($$"""
            extern "{{CLibrary}}" {
                func abs(x:int) -> int
            }
            total <- 0
            for i in [1~5] {
                total <- total + abs(-i)
            }
            print(total)
            """);

        Assert.Equal("15", output);
    }

    [Fact]
    public void ExternCalledFromFunctionAndClosure_Works()
    {
        var output = RunVm($$"""
            extern "{{CLibrary}}" {
                func abs(x:int) -> int
            }
            func viaFunc(v) -> int {
                return abs(-v)
            }
            f <- (v) -> abs(-v)
            print(viaFunc(7))
            print(f(8))
            """);

        Assert.Equal("78", output);
    }

    #endregion
}
