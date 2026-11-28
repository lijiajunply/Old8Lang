using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

/// <summary>
/// IL 模式：字典缺失键统一抛 KEY_ERROR（原生字典与语言值字典两条路径）。
/// </summary>
[Collection("Sequential")]
public class DictionaryMissingKeyIlTests
{
    [Fact]
    public void MissingKey_IndexAccess_ThrowsKeyError()
    {
        const string code = @"
d <- {""a"": 1}
value <- d[""missing""]
";
        var exception = CompileAndRunExpectingError(code);
        Assert.Contains("KEY_ERROR", exception.Message);
        Assert.Contains("missing", exception.Message);
    }

    [Fact]
    public void MissingKey_GetValue_ThrowsKeyError()
    {
        const string code = @"
d <- {""a"": 1}
value <- d.GetValue(""missing"")
";
        var exception = CompileAndRunExpectingError(code);
        Assert.Contains("KEY_ERROR", exception.Message);
        Assert.Contains("missing", exception.Message);
    }

    [Fact]
    public void GetOrElse_OnMissingKey_StillReturnsDefault()
    {
        const string code = @"
d <- {""a"": 1}
value <- d.GetOrElse(""missing"", 42)
Assert.Equal(42, value)
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

    private static Exception CompileAndRunExpectingError(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        try
        {
            var action = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
            return Assert.ThrowsAny<Exception>(() => action());
        }
        finally
        {
            Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;
        }
    }
}
