using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.StandardLibrary;

/// <summary>
/// Time 库测试 - 测试时间处理功能
/// </summary>
public class TimeTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_Time_ShouldWorkCorrectly()
    {
        var code = @"
import Time

PrintLine(""Time library imported"")
";
        CreateTempModuleFile("./StandardLibrary/time_test.old8", code);
        var (interpreter, exception) = ExecuteCodeFile("./StandardLibrary/time_test.old8");

        Assert.Null(exception);
        var timeLib = interpreter.Manager.GetValue(new LangId("Time"));
        Assert.NotNull(timeLib);
        Assert.IsAssignableFrom<IModuleValueType>(timeLib);
    }

    [Fact]
    public void GetLocalTime_ShouldReturnCurrentTime()
    {
        var code = @"
import Time

currentTime <- Time.GetLocalTime()
PrintLine($""Current local time: {currentTime}"")
";
        CreateTempModuleFile("./StandardLibrary/time_getlocal_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/time_getlocal_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void GetLocalTime_WithFormat_ShouldReturnFormattedTime()
    {
        var code = @"
import Time

formattedTime <- Time.GetLocalTime(""yyyy-MM-dd HH:mm:ss"")
PrintLine($""Formatted time: {formattedTime}"")
";
        CreateTempModuleFile("./StandardLibrary/time_formatted_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/time_formatted_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void GetUtcTime_ShouldReturnUtcTime()
    {
        var code = @"
import Time

utcTime <- Time.GetUtcTime()
PrintLine($""UTC time: {utcTime}"")
";
        CreateTempModuleFile("./StandardLibrary/time_getutc_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/time_getutc_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void GetUnixTimeSeconds_ShouldReturnTimestamp()
    {
        var code = @"
import Time

timestamp <- Time.GetUnixTimeSeconds()
PrintLine($""Unix timestamp (seconds): {timestamp}"")
";
        CreateTempModuleFile("./StandardLibrary/time_unix_seconds_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/time_unix_seconds_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void GetUnixTimeMilliseconds_ShouldReturnTimestamp()
    {
        var code = @"
import Time

timestamp <- Time.GetUnixTimeMilliseconds()
PrintLine($""Unix timestamp (milliseconds): {timestamp}"")
";
        CreateTempModuleFile("./StandardLibrary/time_unix_millis_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/time_unix_millis_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void FromUnixTimeSeconds_ShouldConvertToDateTime()
    {
        var code = @"
import Time

timestamp <- 1609459200
dateTime <- Time.FromUnixTimeSeconds(timestamp)
PrintLine($""Timestamp {timestamp} = {dateTime}"")
";
        CreateTempModuleFile("./StandardLibrary/time_from_unix_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/time_from_unix_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void GetCommonFormats_ShouldReturnFormats()
    {
        var code = @"
import Time

formats <- Time.GetCommonFormats()
PrintLine($""Common formats count: {len(formats)}"")
";
        CreateTempModuleFile("./StandardLibrary/time_formats_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/time_formats_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void StartTimer_And_StopTimer_ShouldMeasureTime()
    {
        var code = @"
import Time

Time.StartTimer()
Sleep(50)
elapsed <- Time.StopTimer()
PrintLine($""Elapsed time: {elapsed} ms"")
";
        CreateTempModuleFile("./StandardLibrary/time_timer_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/time_timer_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void GetElapsedMilliseconds_ShouldGetElapsedTime()
    {
        var code = @"
import Time

Time.StartTimer()
Sleep(30)
elapsed1 <- Time.GetElapsedMilliseconds()
PrintLine($""Elapsed time 1: {elapsed1} ms"")

Sleep(20)
elapsed2 <- Time.GetElapsedMilliseconds()
PrintLine($""Elapsed time 2: {elapsed2} ms"")
";
        CreateTempModuleFile("./StandardLibrary/time_elapsed_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/time_elapsed_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void ResetTimer_ShouldResetTimer()
    {
        var code = @"
import Time

Time.StartTimer()
Sleep(30)
ResetTimer()
elapsed <- Time.GetElapsedMilliseconds()
PrintLine($""Elapsed after reset: {elapsed} ms"")
";
        CreateTempModuleFile("./StandardLibrary/time_reset_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/time_reset_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void TimeStamp_Compatibility_ShouldWork()
    {
        var code = @"
import Time

timestamp <- Time.TimeStamp()
PrintLine($""Timestamp (compatibility): {timestamp}"")
";
        CreateTempModuleFile("./StandardLibrary/time_compat_timestamp_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/time_compat_timestamp_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void TimeFormat_Compatibility_ShouldWork()
    {
        var code = @"
import Time

formats <- Time.TimeFormat()
PrintLine($""Formats (compatibility): {len(formats)}"")
";
        CreateTempModuleFile("./StandardLibrary/time_compat_format_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/time_compat_format_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void TimeStart_And_TimeStop_Compatibility_ShouldWork()
    {
        var code = @"
import Time

Time.TimeStart()
Sleep(40)
elapsed <- Time.TimeStop()
PrintLine($""Elapsed (compatibility): {elapsed} ms"")
";
        CreateTempModuleFile("./StandardLibrary/time_compat_timer_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/time_compat_timer_test.old8");

        Assert.Null(exception);
    }
}
