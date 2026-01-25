using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Modules.StandardLibrary;

/// <summary>
/// Terminal 库测试 - 测试终端控制功能
/// </summary>
[Collection("Sequential")]
public class TerminalTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_Terminal_ShouldWorkCorrectly()
    {
        var code = @"
import Terminal

PrintLine(""Terminal library imported"")
";
        CreateTempModuleFile("./StandardLibrary/terminal_test.old8", code);
        var (interpreter, exception) = ExecuteCodeFile("./StandardLibrary/terminal_test.old8");

        Assert.Null(exception);
        var terminalLib = interpreter.Manager.GetValue(new LangId("Terminal"));
        Assert.NotNull(terminalLib);
        Assert.IsAssignableFrom<IModuleValueType>(terminalLib);
    }

    [Fact]
    public void Title_ShouldSetConsoleTitle()
    {
        var code = @"
import Terminal

Terminal.Title(""Old8Lang Test Title"")
PrintLine(""Console title set successfully"")
";
        CreateTempModuleFile("./StandardLibrary/terminal_title_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/terminal_title_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Beep_ShouldExecuteSuccessfully()
    {
        var code = @"
import Terminal

Terminal.Beep()
PrintLine(""Beep executed"")
";
        CreateTempModuleFile("./StandardLibrary/terminal_beep_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/terminal_beep_test.old8");

        Assert.Null(exception);
    }
}
