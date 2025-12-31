using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.StandardLibrary;

/// <summary>
/// RegexLib 库测试 - 测试正则表达式功能
/// </summary>
public class RegexLibTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_Regex_ShouldWorkCorrectly()
    {
        var code = @"
import Regex

PrintLine(""Regex library imported"")
";
        CreateTempModuleFile("./StandardLibrary/regex_test.old8", code);
        var (interpreter, exception) = ExecuteCodeFile("./StandardLibrary/regex_test.old8");

        Assert.Null(exception);
        var regexLib = interpreter.Manager.GetValue(new LangId("Regex"));
        Assert.NotNull(regexLib);
        Assert.IsAssignableFrom<IModuleValueType>(regexLib);
    }

    [Fact]
    public void RegexIsMatch_ShouldFindDigits()
    {
        var code = @"
import Regex

text <- ""hello123world""
pattern <- ""\\d+""
result <- Regex.RegexIsMatch(text, pattern)
PrintLine($""Text: {text}"")
PrintLine($""Pattern: {pattern}"")
PrintLine($""Match: {result}"")
";
        CreateTempModuleFile("./StandardLibrary/regex_ismatch_digits_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/regex_ismatch_digits_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void RegexIsMatch_ShouldNotFindDigitsInTextWithoutThem()
    {
        var code = @"
import Regex

text <- ""helloworld""
pattern <- ""\\d+""
result <- Regex.RegexIsMatch(text, pattern)
PrintLine($""Text: {text}"")
PrintLine($""Pattern: {pattern}"")
PrintLine($""Match: {result}"")
";
        CreateTempModuleFile("./StandardLibrary/regex_ismatch_nodigits_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/regex_ismatch_nodigits_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void RegexReplace_ShouldReplaceDigits()
    {
        var code = @"
import Regex

text <- ""Price: 100 dollars""
pattern <- ""\\d+""
replacement <- ""XXX""
result <- Regex.RegexReplace(text, pattern, replacement)
PrintLine($""Original: {text}"")
PrintLine($""Result: {result}"")
";
        CreateTempModuleFile("./StandardLibrary/regex_replace_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/regex_replace_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void RegexMatch_ShouldExtractEmailPattern()
    {
        var code = @"
import Regex

text <- ""Contact us at: support@example.com""
pattern <- ""[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}""
result <- Regex.RegexMatch(text, pattern)
PrintLine($""Text: {text}"")
PrintLine($""Match: {result}"")
";
        CreateTempModuleFile("./StandardLibrary/regex_match_email_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/regex_match_email_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void RegexMatches_ShouldFindAllNumbers()
    {
        var code = @"
import Regex

text <- ""I have 10 apples and 20 bananas""
pattern <- ""\\d+""
matches <- Regex.RegexMatches(text, pattern)
PrintLine($""Text: {text}"")
PrintLine($""Matches: {matches}"")
";
        CreateTempModuleFile("./StandardLibrary/regex_matches_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/regex_matches_test.old8");

        Assert.Null(exception);
    }
}
