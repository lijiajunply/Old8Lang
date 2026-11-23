using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Functions;

[Collection("Console output isolation")]
public class ReturnConversionRegressionTests
{
    [Fact]
    public void StringReturn_UsesLanguageDisplayConversion()
    {
        const string code = "func value() -> string { return 42 } Assert.Equal(\"42\", value())";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        action();
    }

    [Theory]
    [InlineData("string", "true", "\"true\"")]
    [InlineData("string", "{1, 2}", "\"[1, 2]\"")]
    [InlineData("int", "\"42\"", "42")]
    [InlineData("double", "\"2.5\"", "2.5")]
    [InlineData("bool", "1", "true")]
    public void Return_ConvertsCompatibleValues(string type, string expression, string expected)
    {
        var code = $"func value() -> {type} {{ return {expression} }} Assert.Equal({expected}, value())";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    [Fact]
    public void DynamicReturn_ConvertsDoubleToInt()
    {
        const string code = @"
            func value() -> int {
                values <- {3.9}
                return values[0]
            }
            Assert.Equal(3, value())
        ";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    [Fact]
    public void InvalidReturn_ThrowsLanguageError()
    {
        const string code = "func value() -> int { return \"not a number\" } value()";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        Assert.ThrowsAny<Old8Lang.Error.Old8Exception>(() => action());
    }

    [Fact]
    public void InvalidDynamicReturn_ThrowsTypeError()
    {
        const string code = "func value() -> int { values <- {\"bad\"} return values[0] } value()";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        Assert.Throws<Old8Lang.Error.TypeError>(() => action());
    }

    [Fact]
    public void NumericReturnInTryFinally_IsConvertedBeforeLeaving()
    {
        const string code = @"
            func value() -> double {
                try { return 42 } finally { PrintLine(""finally"") }
            }
            PrintLine(value())
        ";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        var previous = Console.Out;
        using var output = new StringWriter();
        try { Console.SetOut(output); action(); }
        finally { Console.SetOut(previous); }
        Assert.Equal($"finally{Environment.NewLine}42{Environment.NewLine}", output.ToString());
    }
}
