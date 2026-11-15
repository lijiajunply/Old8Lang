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

    [Fact]
    public void StringMethods_OnNativeLiteralReceiver_Work()
    {
        // 接收者是原生 string、实参也是原生的（字面量或字符串变量），
        // 这些方法原先把原生值直接传给 LangValueType helper，导致无效 IL 或访问越界。
        const string code = @"
            Assert.True(""abc"".StartsWith(""a""))
            Assert.True(""abc"".EndsWith(""c""))
            Assert.True(""abc"".Contains(""b""))
            Assert.Equal(1, ""abc"".IndexOf(""b""))
            Assert.Equal(""aXc"", ""abc"".Replace(""b"", ""X""))
            Assert.Equal(2, ""a,b"".Split("","").Count())
            Assert.Equal(""a"", ""  a  "".Trim())
            Assert.Equal(""ab"", ""AB"".ToLower())
            Assert.Equal(""AB"", ""ab"".ToUpper())
            Assert.Equal(""bcd"", ""abcd"".Substring(1))
            Assert.Equal(""cba"", ""abc"".Reverse())
            Assert.Equal(""  a"", ""a"".PadLeft(3))
            Assert.Equal(3, ""abc"".ToCharArray().Count())
            Assert.Equal(""YWJj"", ""abc"".ToBase64())

            // 模板结果是原生 string，链式调用同样要走包装路径
            line <- ""hello world""
            text <- $""{line}!""
            Assert.True(text.StartsWith(""hello""))
            Assert.True(text.Contains(""world""))
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
