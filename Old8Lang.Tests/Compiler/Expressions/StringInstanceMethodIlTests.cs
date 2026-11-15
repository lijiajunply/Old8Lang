using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Expressions;

/// <summary>
/// 接收者为原生 CLR string 的实例方法（IL 模式）。
/// </summary>
/// <remarks>
/// <c>"=".Repeat(5)</c> 在 IL 里接收者是原生 <c>string</c>、实参是原生 <c>int</c>，
/// 而 <c>StringRepeatMethod</c> 的 helper 形参是 <c>StringLangValue</c> / <c>IntLangValue</c>。
/// 之前直接加载原生值调用 helper，生成无效 IL（InvalidProgramException）。
/// </remarks>
[Collection("Sequential")]
public class StringInstanceMethodIlTests
{
    [Fact]
    public void Repeat_WithNativeStringReceiver_ProducesExpectedText()
    {
        const string code = @"
            repeated <- ""ab"".Repeat(3)
            Assert.True(repeated == ""ababab"")
            Assert.True(""a"".Repeat(2) == ""aa"")
            Assert.True(""="".Repeat(4) == ""===="")
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
