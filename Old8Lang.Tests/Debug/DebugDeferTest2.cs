using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;
using Xunit;

namespace Old8Lang.Tests.Debug;

public class DebugDeferTest2
{
    [Fact]
    public void TestDeferParsing_NoLeadingNewline()
    {
        var code = @"func test() {
    defer PrintLine(""cleanup"")
}";
        var tokens = LangInterpreter.Tokenize(code);
        Console.WriteLine($"Tokens count: {tokens.Count}");
        foreach (var token in tokens.Take(10))
        {
            Console.WriteLine($"  {token.Type}: '{token.Value}' at Line {token.Line}");
        }

        var parser = new LangParser.LangParser(tokens, code);
        var result = parser.ParseProgram();

        Console.WriteLine($"Result.Count: {result.Count}");
        Assert.True(result.Count > 0, $"Expected result.Count > 0, but got {result.Count}");
    }
}
