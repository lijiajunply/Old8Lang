using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Types;

/// <summary>
/// IL 模式下泛型可空参数（<c>T?</c>）与泛型特化的实参转换。
/// </summary>
/// <remarks>
/// <c>T?</c> 在泛型特化里解析成 <c>object</c>，而调用点加载的是原生 int：
/// 构造 <c>Box&lt;int&gt;(5)</c> 时 init 的形参是 object，泛型函数
/// <c>identity&lt;int&gt;(456)</c> 的形参也是 object。此前都不做装箱，生成无效 IL。
/// 另外「由构造函数实参推断类型参数」在 IL 模式不支持，现在给出明确编译错误
/// 而不是静默返回 null。
/// </remarks>
[Collection("Sequential")]
public class GenericNullableIlTests
{
    private const string BoxCode = @"
        class Box<T?> {
            value: T?

            func init(v: T?) {
                this.value <- v
            }

            func getValue() -> T? {
                return this.value
            }

            func hasValue() -> bool {
                return this.value != null
            }
        }
    ";

    [Fact]
    public void GenericClassWithNullableParameter_StoresValue()
    {
        var code = BoxCode + @"
            box <- Box<int>(7)
            Assert.True(box.hasValue())
            Assert.Equal(""7"", box.getValue().ToStr())
        ";
        CompileAndRun(code);
    }

    [Fact]
    public void GenericClassWithNullableParameter_AcceptsNull()
    {
        var code = BoxCode + @"
            box <- Box<int>(null)
            Assert.False(box.hasValue())
        ";
        CompileAndRun(code);
    }

    [Fact]
    public void GenericFunctionWithNullableParameter_BoxesValueArgument()
    {
        var code = @"
            func identity<T?>(v: T?) -> T? {
                return v
            }

            result <- identity<int>(456)
            Assert.Equal(""456"", result.ToStr())
        ";
        CompileAndRun(code);
    }

    [Fact]
    public void GenericClassWithoutTypeArguments_ReportsClearError()
    {
        var code = BoxCode + @"
            box <- Box(123)
        ";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        try
        {
            var exception = Assert.ThrowsAny<Exception>(() =>
                Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));

            Assert.DoesNotContain("InvalidProgram", exception.GetType().Name);
            Assert.Contains("泛型类", exception.Message);
        }
        finally
        {
            Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;
        }
    }

    private static void CompileAndRun(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        try
        {
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)();
        }
        finally
        {
            Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;
        }
    }
}
