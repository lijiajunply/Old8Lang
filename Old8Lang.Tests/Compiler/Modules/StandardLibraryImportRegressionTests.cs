using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Modules;

public class StandardLibraryImportRegressionTests
{
    [Theory]
    [InlineData("import \"Math\"", "Math")]
    [InlineData("import \"Math\" as maths", "maths")]
    public void StandardLibraryModule_CallsMethodsAtTopLevelAndInsideFunctions(string import, string name)
    {
        var code = $@"
{import}
func phaseOneRoot(x: int) -> double {{ return {name}.Sqrt(x) }}
Assert.Equal(3.0, {name}.Sqrt(9))
Assert.Equal(8.0, {name}.Pow(2, 3))
Assert.Equal(4.0, phaseOneRoot(16))
";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        action();
    }

    [Fact]
    public void StandardLibraryModule_BridgesNonMathModuleMethods()
    {
        var code = @"
import ""Regex"" as regex
func isOld8(value: string) -> bool { return regex.RegexIsMatch(value, ""^Old8$"", false) }
Assert.True(regex.RegexIsMatch(""Old8"", ""^Old8$"", false))
Assert.True(isOld8(""Old8""))
";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);

        action();
    }
}
