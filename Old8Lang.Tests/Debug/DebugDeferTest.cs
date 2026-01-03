using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;
using Xunit;

namespace Old8Lang.Tests.Debug;

public class DebugDeferTest
{
    [Fact]
    public void TestDeferParsing()
    {
        var code = @"
func test() {
    defer PrintLine(""cleanup"")
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        var result = parser.ParseProgram();

        // 调试输出
        Console.WriteLine($"Result type: {result.GetType().Name}");
        Console.WriteLine($"Result.Count: {result.Count}");
        Console.WriteLine($"Result.OtherStatements.Count: {result.OtherStatements.Count}");

        Assert.True(result.Count > 0, $"Expected result.Count > 0, but got {result.Count}");
    }
}
