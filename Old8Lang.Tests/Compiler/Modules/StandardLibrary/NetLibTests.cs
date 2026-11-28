using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Modules.StandardLibrary;

/// <summary>
/// NetLib 库测试 - 测试网络功能（HTTP、WebAPI等），使用本地回环桩消除外网依赖
/// </summary>
[Collection("Sequential")]
public class NetLibTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_Net_ShouldWorkCorrectly()
    {
        var code = @"
import Net

PrintLine(""Net library imported"")
";
        CreateTempModuleFile("./StandardLibrary/net_import_test.old8", code);
        var (interpreter, exception) = ExecuteCodeFile("./StandardLibrary/net_import_test.old8");

        Assert.Null(exception);
        var netLib = interpreter.Manager.GetValue(new LangId("Net"));
        Assert.NotNull(netLib);
        Assert.IsAssignableFrom<IModuleValueType>(netLib);
    }

    [Fact]
    public void HttpWebClient_GetAsync_ShouldWorkCorrectly()
    {
        using var server = new LoopbackHttpTestServer();
        var code = $@"
import Net

client <- Net.HttpWebClient()
client.SetTimeout(5000)
response <- client.GetAsync(""{server.BaseUrl}posts/1"")

if !response.IsSuccessStatusCode {{
    throw ""GET failed: "" + response.StatusCode.ToStr()
}}
PrintLine($""Status Code: {{response.StatusCode}}"")
PrintLine($""Content Length: {{len(response.Content)}}"")
client.Dispose()
";
        CreateTempModuleFile("./StandardLibrary/net_get_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/net_get_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void HttpWebClient_PostAsync_ShouldWorkCorrectly()
    {
        using var server = new LoopbackHttpTestServer();
        var code = $@"
import Net

client <- Net.HttpWebClient()
client.SetTimeout(5000)

postData <- {{
    ""title"": ""test post"",
    ""body"": ""test body content"",
    ""userId"": 1
}}
jsonContent <- JsonSerialize(postData)
response <- client.PostAsync(""{server.BaseUrl}posts"", jsonContent, ""application/json"")

if !response.IsSuccessStatusCode {{
    throw ""POST failed: "" + response.StatusCode.ToStr()
}}
PrintLine($""Status Code: {{response.StatusCode}}"")
PrintLine($""Response Content: {{response.Content}}"")
client.Dispose()
";
        CreateTempModuleFile("./StandardLibrary/net_post_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/net_post_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void WebApiClient_GetAsync_ShouldWorkCorrectly()
    {
        using var server = new LoopbackHttpTestServer();
        var code = $@"
import Net

client <- Net.WebApiClient(""{server.BaseUrl}"")
client.SetTimeout(5000)
response <- client.GetAsync(""posts/1"")

if !response.IsSuccessStatusCode {{
    throw ""WebApi GET failed: "" + response.StatusCode.ToStr()
}}
PrintLine($""Status Code: {{response.StatusCode}}"")
PrintLine($""Content Length: {{len(response.Content)}}"")
client.Dispose()
";
        CreateTempModuleFile("./StandardLibrary/webapi_get_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/webapi_get_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void WebApiClient_PostAsync_ShouldWorkCorrectly()
    {
        using var server = new LoopbackHttpTestServer();
        var code = $@"
import Net

client <- Net.WebApiClient(""{server.BaseUrl}"")
client.SetTimeout(5000)

postData <- {{
    ""title"": ""WebAPI Test"",
    ""body"": ""This is a test from Old8Lang WebApiClient"",
    ""userId"": 1
}}
response <- client.PostAsync(""posts"", postData)

if !response.IsSuccessStatusCode {{
    throw ""WebApi POST failed: "" + response.StatusCode.ToStr()
}}
PrintLine($""Status Code: {{response.StatusCode}}"")
PrintLine($""Response Content: {{response.Content}}"")
client.Dispose()
";
        CreateTempModuleFile("./StandardLibrary/webapi_post_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/webapi_post_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void HttpWebClient_WithDefaultHeaders_ShouldWorkCorrectly()
    {
        using var server = new LoopbackHttpTestServer();
        var code = $@"
import Net

client <- Net.HttpWebClient()
client.AddDefaultHeader(""User-Agent"", ""Old8Lang/1.0"")
client.AddDefaultHeader(""Accept"", ""application/json"")
client.SetTimeout(5000)

response <- client.GetAsync(""{server.BaseUrl}posts/1"")
if !response.IsSuccessStatusCode {{
    throw ""First GET failed""
}}
PrintLine($""First request - Status: {{response.StatusCode}}"")

client.RemoveDefaultHeader(""User-Agent"")
response <- client.GetAsync(""{server.BaseUrl}posts/2"")
if !response.IsSuccessStatusCode {{
    throw ""Second GET failed""
}}
PrintLine($""Second request - Status: {{response.StatusCode}}"")
client.Dispose()
";
        CreateTempModuleFile("./StandardLibrary/net_headers_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/net_headers_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void HttpWebClient_PutAsync_ShouldWorkCorrectly()
    {
        using var server = new LoopbackHttpTestServer();
        var code = $@"
import Net

client <- Net.HttpWebClient()
client.SetTimeout(5000)

updateData <- {{
    ""id"": 1,
    ""title"": ""updated title"",
    ""body"": ""updated body"",
    ""userId"": 1
}}
jsonContent <- JsonSerialize(updateData)
response <- client.PutAsync(""{server.BaseUrl}posts/1"", jsonContent, ""application/json"")

if !response.IsSuccessStatusCode {{
    throw ""PUT failed: "" + response.StatusCode.ToStr()
}}
PrintLine($""Status Code: {{response.StatusCode}}"")
client.Dispose()
";
        CreateTempModuleFile("./StandardLibrary/net_put_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/net_put_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void HttpWebClient_DeleteAsync_ShouldWorkCorrectly()
    {
        using var server = new LoopbackHttpTestServer();
        var code = $@"
import Net

client <- Net.HttpWebClient()
client.SetTimeout(5000)
response <- client.DeleteAsync(""{server.BaseUrl}posts/1"")

if !response.IsSuccessStatusCode {{
    throw ""DELETE failed: "" + response.StatusCode.ToStr()
}}
PrintLine($""Status Code: {{response.StatusCode}}"")
client.Dispose()
";
        CreateTempModuleFile("./StandardLibrary/net_delete_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/net_delete_test.old8");

        Assert.Null(exception);
    }
}
