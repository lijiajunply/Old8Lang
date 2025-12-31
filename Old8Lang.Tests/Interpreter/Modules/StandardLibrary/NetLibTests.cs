using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.StandardLibrary;

/// <summary>
/// NetLib 库测试 - 测试网络功能（HTTP、WebAPI等）
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
        var code = @"
import Net

// 创建 HttpWebClient 实例
client <- Net.HttpWebClient()

// 设置超时时间为 5000 毫秒
client.SetTimeout(5000)

// 发送 GET 请求，Task 会自动等待完成
response <- client.GetAsync(""https://jsonplaceholder.typicode.com/posts/1"")

// 验证响应
PrintLine($""Status Code: {response.StatusCode}"")
PrintLine($""IsSuccessStatusCode: {response.IsSuccessStatusCode}"")
PrintLine($""Content Length: {len(response.Content)}"")

// 清理资源
client.Dispose()
";
        CreateTempModuleFile("./StandardLibrary/net_get_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/net_get_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void HttpWebClient_PostAsync_ShouldWorkCorrectly()
    {
        var code = @"
import Net
import Json

// 创建 HttpWebClient 实例
client <- Net.HttpWebClient()

// 设置超时时间
client.SetTimeout(5000)

// 准备 POST 数据
postData <- {
    ""title"": ""test post"",
    ""body"": ""test body content"",
    ""userId"": 1
}

// 将数据转换为 JSON 字符串
jsonContent <- Json.Serialize(postData)

// 发送 POST 请求，Task 会自动等待完成
response <- client.PostAsync(""https://jsonplaceholder.typicode.com/posts"", jsonContent, ""application/json"")

// 验证响应
PrintLine($""Status Code: {response.StatusCode}"")
PrintLine($""IsSuccessStatusCode: {response.IsSuccessStatusCode}"")
PrintLine($""Response Content: {response.Content}"")

// 清理资源
client.Dispose()
";
        CreateTempModuleFile("./StandardLibrary/net_post_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/net_post_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void WebApiClient_GetAsync_ShouldWorkCorrectly()
    {
        var code = @"
import Net

// 创建 WebApiClient 实例，设置基础 URL
client <- Net.WebApiClient(""https://jsonplaceholder.typicode.com/"")

// 设置超时时间
client.SetTimeout(5000)

// 发送 GET 请求，获取指定 ID 的 post，Task 会自动等待完成
response <- client.GetAsync(""posts/1"")

// 验证响应
PrintLine($""Status Code: {response.StatusCode}"")
PrintLine($""IsSuccessStatusCode: {response.IsSuccessStatusCode}"")
PrintLine($""Content Length: {len(response.Content)}"")

// 清理资源
client.Dispose()
";
        CreateTempModuleFile("./StandardLibrary/webapi_get_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/webapi_get_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void WebApiClient_PostAsync_ShouldWorkCorrectly()
    {
        var code = @"
import Net

// 创建 WebApiClient 实例
client <- Net.WebApiClient(""https://jsonplaceholder.typicode.com/"")

// 设置超时时间
client.SetTimeout(5000)

// 准备 POST 数据（使用字典）
postData <- {
    ""title"": ""WebAPI Test"",
    ""body"": ""This is a test from Old8Lang WebApiClient"",
    ""userId"": 1
}

// 发送 POST 请求，Task 会自动等待完成
response <- client.PostAsync(""posts"", postData)

// 验证响应
PrintLine($""Status Code: {response.StatusCode}"")
PrintLine($""IsSuccessStatusCode: {response.IsSuccessStatusCode}"")
PrintLine($""Response Content: {response.Content}"")

// 清理资源
client.Dispose()
";
        CreateTempModuleFile("./StandardLibrary/webapi_post_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/webapi_post_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void HttpWebClient_WithDefaultHeaders_ShouldWorkCorrectly()
    {
        var code = @"
import Net

// 创建 HttpWebClient 实例
client <- Net.HttpWebClient()

// 添加默认请求头
client.AddDefaultHeader(""User-Agent"", ""Old8Lang/1.0"")
client.AddDefaultHeader(""Accept"", ""application/json"")

// 设置超时时间
client.SetTimeout(5000)

// 发送 GET 请求，Task 会自动等待完成
response <- client.GetAsync(""https://jsonplaceholder.typicode.com/posts/1"")

PrintLine($""First request - Status: {response.StatusCode}"")

// 移除一个请求头
client.RemoveDefaultHeader(""User-Agent"")

// 再次发送请求，Task 会自动等待完成
response <- client.GetAsync(""https://jsonplaceholder.typicode.com/posts/2"")

PrintLine($""Second request - Status: {response.StatusCode}"")

// 清理资源
client.Dispose()
";
        CreateTempModuleFile("./StandardLibrary/net_headers_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/net_headers_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void HttpWebClient_PutAsync_ShouldWorkCorrectly()
    {
        var code = @"
import Net
import Json

// 创建 HttpWebClient 实例
client <- Net.HttpWebClient()
client.SetTimeout(5000)

// 准备更新数据
updateData <- {
    ""id"": 1,
    ""title"": ""updated title"",
    ""body"": ""updated body"",
    ""userId"": 1
}

jsonContent <- Json.Serialize(updateData)

// 发送 PUT 请求，Task 会自动等待完成
response <- client.PutAsync(""https://jsonplaceholder.typicode.com/posts/1"", jsonContent, ""application/json"")

PrintLine($""Status Code: {response.StatusCode}"")
PrintLine($""IsSuccessStatusCode: {response.IsSuccessStatusCode}"")

client.Dispose()
";
        CreateTempModuleFile("./StandardLibrary/net_put_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/net_put_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void HttpWebClient_DeleteAsync_ShouldWorkCorrectly()
    {
        var code = @"
import Net

// 创建 HttpWebClient 实例
client <- Net.HttpWebClient()
client.SetTimeout(5000)

// 发送 DELETE 请求，Task 会自动等待完成
response <- client.DeleteAsync(""https://jsonplaceholder.typicode.com/posts/1"")

PrintLine($""Status Code: {response.StatusCode}"")
PrintLine($""IsSuccessStatusCode: {response.IsSuccessStatusCode}"")

client.Dispose()
";
        CreateTempModuleFile("./StandardLibrary/net_delete_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/net_delete_test.old8");

        Assert.Null(exception);
    }
}
