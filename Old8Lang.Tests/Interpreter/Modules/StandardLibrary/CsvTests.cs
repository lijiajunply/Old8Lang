using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.StandardLibrary;

/// <summary>
/// Csv 库测试 - 测试 CSV 文件处理功能
/// </summary>
public class CsvTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_Csv_ShouldWorkCorrectly()
    {
        var code = @"
import Csv

PrintLine(""Csv library imported"")
";
        CreateTempModuleFile("./StandardLibrary/csv_test.old8", code);
        var (interpreter, exception) = ExecuteCodeFile("./StandardLibrary/csv_test.old8");

        Assert.Null(exception);
        var csvLib = interpreter.Manager.GetValue(new LangId("Csv"));
        Assert.NotNull(csvLib);
        Assert.IsAssignableFrom<IModuleValueType>(csvLib);
    }

    [Fact]
    public void ParseCsvLine_ShouldParseSimpleLine()
    {
        var code = @"
import Csv

line <- ""a,b,c""
parsed <- Csv.ParseCsvLine(line)
PrintLine($""Line: {line}"")
PrintLine($""Parsed: {parsed}"")
";
        CreateTempModuleFile("./StandardLibrary/csv_parseline_simple_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/csv_parseline_simple_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void ParseCsvLine_ShouldParseLineWithQuotes()
    {
        var code = """
import Csv

line <- "\"a\",\"b\",\"c\""
parsed <- Csv.ParseCsvLine(line)
count <- len(parsed)
PrintLine("Parsed array length: " + count.ToStr())
for i <- 0, i < count, i <- i + 1 {
    PrintLine("  [" + i.ToStr() + "]: " + parsed[i])
}
""";
        CreateTempModuleFile("./StandardLibrary/csv_parseline_quotes_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/csv_parseline_quotes_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void FormatCsvLine_ShouldFormatArray()
    {
        var code = @"
import Csv

data <- [""apple"", ""banana"", ""cherry""]
formatted <- Csv.FormatCsvLine(data)
PrintLine($""Data: {data}"")
PrintLine($""Formatted: {formatted}"")
";
        CreateTempModuleFile("./StandardLibrary/csv_formatline_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/csv_formatline_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void ParseCsvLine_ThenFormat_ShouldBeReversible()
    {
        var code = @"
import Csv

original <- ""value1,value2,value3""
parsed <- Csv.ParseCsvLine(original)
formatted <- Csv.FormatCsvLine(parsed)
PrintLine($""Original: {original}"")
PrintLine($""Parsed: {parsed}"")
PrintLine($""Formatted: {formatted}"")
";
        CreateTempModuleFile("./StandardLibrary/csv_reversible_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/csv_reversible_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void ParseCsvContent_ShouldParseMultilineCSV()
    {
        var code = @"
import Csv

csvContent <- ""name,age,city
Alice,30,NYC
Bob,25,LA
Charlie,35,Chicago""
parsed <- Csv.ParseCsvContent(csvContent, true)
PrintLine($""CSV Content:"")
PrintLine(csvContent)
PrintLine($""Parsed rows: {len(parsed)}"")
";
        CreateTempModuleFile("./StandardLibrary/csv_parsecontent_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/csv_parsecontent_test.old8");

        Assert.Null(exception);
    }
}
