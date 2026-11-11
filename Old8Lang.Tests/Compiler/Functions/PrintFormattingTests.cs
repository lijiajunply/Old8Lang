using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Functions;

[CollectionDefinition("Console output isolation", DisableParallelization = true)]
public class ConsoleOutputCollection { }

[Collection("Console output isolation")]
public class PrintFormattingTests
{
    [Fact]
    public void PrintAndPrintLine_FormatAllValuesWithLanguageRules()
    {
        const string code = @"
            func part() -> int {
                Print(""inner"")
                return 3
            }
            PrintLine(true)
            PrintLine({1, 2})
            Print(false, "" / "", null)
            PrintLine("" / "", [3, 4])
            PrintLine(""outer"", part())
            PrintLine()
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var action = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        var previous = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            action();
        }
        finally
        {
            Console.SetOut(previous);
        }

        Assert.Equal(string.Join(Environment.NewLine,
            "true", "[1, 2]", "false / null / [3, 4]", "innerouter3", "", ""), output.ToString());
    }
}
