using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.StandardLibrary;

/// <summary>
/// NetLib 库测试 - 测试网络功能（HTTP、MQTT、WebSocket 等）
/// </summary>
public class NetLibTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_Net_ShouldWorkCorrectly()
    {
        var code = @"
import Net

PrintLine(""Net library imported"")
";
        CreateTempModuleFile("./StandardLibrary/net_test.old8", code);
        var (interpreter, exception) = ExecuteCodeFile("./StandardLibrary/net_test.old8");

        Assert.Null(exception);
        var netLib = interpreter.Manager.GetValue(new LangId("Net"));
        Assert.NotNull(netLib);
        Assert.IsAssignableFrom<IModuleValueType>(netLib);
    }

    [Fact]
    public void CreateHttpClient_ShouldWorkCorrectly()
    {
        var code = @"
import Net

client <- Net.CreateHttpClient()
PrintLine(""HTTP client created"")
";
        CreateTempModuleFile("./StandardLibrary/net_httpclient_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/net_httpclient_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void HttpClient_SetTimeout_ShouldWorkCorrectly()
    {
        var code = @"
import Net

client <- Net.HttpClient()
Net.HttpSetTimeout(client, 30000)
PrintLine(""HTTP client timeout set to 30 seconds"")
";
        CreateTempModuleFile("./StandardLibrary/net_http_timeout_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/net_http_timeout_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void HttpClient_AddDefaultHeader_ShouldWorkCorrectly()
    {
        var code = @"
import Net

client <- Net.CreateHttpClient()
Net.HttpAddDefaultHeader(client, ""User-Agent"", ""Old8Lang/1.0"")
PrintLine(""Default header added to HTTP client"")
";
        CreateTempModuleFile("./StandardLibrary/net_http_header_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/net_http_header_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void CreateMqttClient_ShouldWorkCorrectly()
    {
        var code = @"
import Net

client <- Net.CreateMqttClient(""localhost"", 1883)
PrintLine(""MQTT client created"")
";
        CreateTempModuleFile("./StandardLibrary/net_mqtt_create_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/net_mqtt_create_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void CreateWebSocketClient_ShouldWorkCorrectly()
    {
        var code = @"
import Net

client <- Net.CreateWebSocketClient()
PrintLine(""WebSocket client created"")
";
        CreateTempModuleFile("./StandardLibrary/net_websocket_create_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/net_websocket_create_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void CreateSocketClient_ShouldWorkCorrectly()
    {
        var code = @"
import Net

client <- Net.CreateSocketClient()
PrintLine(""Socket client created"")
";
        CreateTempModuleFile("./StandardLibrary/net_socket_create_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/net_socket_create_test.old8");

        Assert.Null(exception);
    }
}
