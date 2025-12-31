using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.StandardLibrary;

/// <summary>
/// OS 库测试 - 测试操作系统相关功能
/// </summary>
public class OSTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_OS_ShouldWorkCorrectly()
    {
        var code = @"
import OS

PrintLine(""OS library imported"")
";
        CreateTempModuleFile("./StandardLibrary/os_test.old8", code);
        var (interpreter, exception) = ExecuteCodeFile("./StandardLibrary/os_test.old8");

        Assert.Null(exception);
        var osLib = interpreter.Manager.GetValue(new LangId("OS"));
        Assert.NotNull(osLib);
        Assert.IsAssignableFrom<IModuleValueType>(osLib);
    }

    [Fact]
    public void OsInfo_ShouldReturnSystemInformation()
    {
        var code = @"
import OS

info <- OS.OsInfo()
PrintLine(info)
";
        CreateTempModuleFile("./StandardLibrary/os_osinfo_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/os_osinfo_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void OsInfo_ResultShouldContainMachineName()
    {
        var code = @"
import OS

info <- OS.OsInfo()
hasName <- info.Contains(""MachineName"")
PrintLine(hasName)
";
        CreateTempModuleFile("./StandardLibrary/os_machname_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/os_machname_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void OsInfo_ResultShouldContainUserName()
    {
        var code = @"
import OS

info <- OS.OsInfo()
hasUser <- info.Contains(""UserName"")
PrintLine(hasUser)
";
        CreateTempModuleFile("./StandardLibrary/os_username_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/os_username_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Process_EchoCommand_ShouldReturnOutput()
    {
        var code = @"
import OS

result <- OS.Process(""echo Hello from Old8Lang"")
PrintLine(result)
";
        CreateTempModuleFile("./StandardLibrary/os_process_echo_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/os_process_echo_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Process_SimpleListCommand_ShouldExecuteSuccessfully()
    {
        var code = @"
import OS

result <- OS.Process(""ls"")
PrintLine(result)
";
        CreateTempModuleFile("./StandardLibrary/os_process_ls_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/os_process_ls_test.old8");

        Assert.Null(exception);
    }
}
