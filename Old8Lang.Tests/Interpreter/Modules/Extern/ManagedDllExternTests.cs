using Old8Lang.Error;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.Extern;

/// <summary>
/// 供 ManagedDllExternTests 通过 dotnetdll: 导入的托管测试目标类
/// </summary>
public static class ManagedDllExternTestHelper
{
    public static int Add(int a, int b) => a + b;
    public static double WeightedSum(double left, double right) => left * 2.0 + right;
    public static string FormatTag(string prefix, int code) => $"{prefix}-{code}";
}

/// <summary>
/// .NET 托管 DLL Extern 功能测试（解释模式）
/// 覆盖 C#: / cs: / csharp: / dotnetdll: 前缀、方法块、单方法、别名、命名参数与错误处理
/// </summary>
[Collection("Sequential")]
public class ManagedDllExternTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void ManagedExtern_BlockImport_CallsMultipleSystemMathMethods()
    {
        var code = @"
extern ""C#:System"" Math {
    func Pow(x:double, y:double) -> double,
    func Sqrt(x:double) -> double
}

powResult <- Pow(2.0, 10.0)
sqrtResult <- Sqrt(81.0)
";
        CreateTempModuleFile("managed_block_import.old8", code);

        var (interpreter, exception) = ExecuteCodeFile("managed_block_import.old8");

        Assert.Null(exception);
        AssertVariableValue(interpreter, "powResult", 1024.0);
        AssertVariableValue(interpreter, "sqrtResult", 9.0);
    }

    [Fact]
    public void ManagedExtern_SingleMethodImport_CallsSuccessfully()
    {
        var code = @"
extern ""C#:System"" Math func Pow(x:double, y:double) -> double

result <- Pow(3.0, 4.0)
";
        CreateTempModuleFile("managed_single_import.old8", code);

        var (interpreter, exception) = ExecuteCodeFile("managed_single_import.old8");

        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", 81.0);
    }

    [Fact]
    public void ManagedExtern_AliasImport_CallsByAlias()
    {
        var code = @"
extern ""C#:System"" Math {
    func Pow(x:double, y:double) -> double as Power,
    func Abs(x:int) -> int as IntAbs
}

p <- Power(2.0, 8.0)
a <- IntAbs(-42)
";
        CreateTempModuleFile("managed_alias_import.old8", code);

        var (interpreter, exception) = ExecuteCodeFile("managed_alias_import.old8");

        Assert.Null(exception);
        AssertVariableValue(interpreter, "p", 256.0);
        AssertVariableValue(interpreter, "a", 42);
    }

    [Fact]
    public void ManagedExtern_NamedArguments_BindsParametersByName()
    {
        var code = @"
extern ""C#:System"" Math func Pow(x:double, y:double) -> double

result <- Pow(y: 3.0, x: 2.0)
";
        CreateTempModuleFile("managed_named_args.old8", code);

        var (interpreter, exception) = ExecuteCodeFile("managed_named_args.old8");

        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", 8.0);
    }

    [Fact]
    public void ManagedExtern_CsAndCSharpPrefixes_EquivalentToCSharpColon()
    {
        var code = @"
extern ""cs:System"" Math func Pow(x:double, y:double) -> double as CsPow
extern ""csharp:System"" Math func Sqrt(x:double) -> double as CSharpSqrt

r1 <- CsPow(5.0, 2.0)
r2 <- CSharpSqrt(49.0)
";
        CreateTempModuleFile("managed_prefix_variants.old8", code);

        var (interpreter, exception) = ExecuteCodeFile("managed_prefix_variants.old8");

        Assert.Null(exception);
        AssertVariableValue(interpreter, "r1", 25.0);
        AssertVariableValue(interpreter, "r2", 7.0);
    }

    [Fact]
    public void ManagedExtern_CustomDotNetDll_LoadsAndInvokesStaticMethods()
    {
        var assemblyPath = typeof(ManagedDllExternTestHelper).Assembly.Location.Replace("\\", "/");
        var code = $@"
extern ""dotnetdll:{assemblyPath}"" ManagedDllExternTestHelper {{
    func Add(a:int, b:int) -> int,
    func WeightedSum(left:double, right:double) -> double as WSum,
    func FormatTag(prefix:string, code:int) -> string
}}

sum <- Add(19, 23)
weighted <- WSum(right: 5.0, left: 3.5)
tag <- FormatTag(""OK"", 200)
";
        CreateTempModuleFile("managed_custom_dll.old8", code);

        var (interpreter, exception) = ExecuteCodeFile("managed_custom_dll.old8");

        Assert.Null(exception);
        AssertVariableValue(interpreter, "sum", 42);
        AssertVariableValue(interpreter, "weighted", 12.0);
        AssertVariableValue(interpreter, "tag", "OK-200");
    }

    [Fact]
    public void ManagedExtern_NonExistentAssembly_ThrowsImportError()
    {
        var code = @"
extern ""C#:NonExistentAssembly_Old8_XYZ_12345"" SomeClass func DoWork() -> void
";
        CreateTempModuleFile("managed_missing_assembly.old8", code);

        AssertExecutionThrows("managed_missing_assembly.old8", typeof(ImportError));
    }

    [Fact]
    public void ManagedExtern_NonExistentType_ThrowsInvalidOperationError()
    {
        var code = @"
extern ""C#:System"" NonExistentType_Old8_XYZ func Foo() -> void
";
        CreateTempModuleFile("managed_missing_type.old8", code);

        AssertExecutionThrows("managed_missing_type.old8", typeof(InvalidOperationError));
    }

    [Fact]
    public void ManagedExtern_NonExistentMethod_ThrowsInvalidOperationError()
    {
        var code = @"
extern ""C#:System"" Math func NonExistentMethod_Old8(x:double) -> double
";
        CreateTempModuleFile("managed_missing_method.old8", code);

        AssertExecutionThrows("managed_missing_method.old8", typeof(InvalidOperationError));
    }
}
