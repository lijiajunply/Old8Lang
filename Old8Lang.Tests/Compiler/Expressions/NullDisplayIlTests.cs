using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Expressions;

/// <summary>
/// IL 模式：null 的 ToStr / 字符串插值显示为 <c>null</c>（与解释器一致）。
/// </summary>
[Collection("Sequential")]
public class NullDisplayIlTests
{
    [Fact]
    public void Null_ToStrAndInterpolation_UseLowercaseNull()
    {
        const string code = @"
x <- null
Assert.Equal(""null"", x.ToStr())
Assert.Equal(""v=null"", $""v={x}"")
Assert.Equal(""null"", null.ToStr())
";
        CompileAndRun(code);
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
