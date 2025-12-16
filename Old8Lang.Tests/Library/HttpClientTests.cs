namespace Old8Lang.Tests.Library;

public class HttpClientTests
{
    [Fact]
    public async Task HttpClient_ShouldBeAbleToSendGetRequest()
    {
        // 测试 HttpClient 发送 GET 请求
        using var client = new Old8Lang.NetLib.HttpClient();
        
        // 设置超时时间
        client.SetTimeout(5000);
        
        // 使用公共测试 API
        var response = await client.GetAsync("https://jsonplaceholder.typicode.com/posts/1");
        
        // 验证响应
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.NotEmpty(response.Content);
        Assert.NotNull(response.Headers);
        Assert.NotEmpty(response.Headers);
    }

    [Fact]
    public async Task HttpClient_ShouldBeAbleToSendPostRequest()
    {
        // 测试 HttpClient 发送 POST 请求
        using var client = new Old8Lang.NetLib.HttpClient();
        
        // 设置超时时间
        client.SetTimeout(5000);
        
        // 使用公共测试 API
        var content = "{\"title\":\"test\",\"body\":\"test body\",\"userId\":1}";
        var response = await client.PostAsync("https://jsonplaceholder.typicode.com/posts", content);
        
        // 验证响应
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(201, response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.NotEmpty(response.Content);
    }

    [Fact]
    public async Task HttpClient_ShouldHandleDefaultHeaders()
    {
        // 测试 HttpClient 默认请求头
        using var client = new Old8Lang.NetLib.HttpClient();
        
        // 添加默认请求头
        client.AddDefaultHeader("User-Agent", "Old8Lang/1.0");
        client.AddDefaultHeader("Accept", "application/json");
        
        // 设置超时时间
        client.SetTimeout(5000);
        
        // 使用公共测试 API
        var response = await client.GetAsync("https://jsonplaceholder.typicode.com/posts/1");
        
        // 验证响应
        Assert.True(response.IsSuccessStatusCode);
        
        // 移除请求头并验证
        client.RemoveDefaultHeader("User-Agent");
        response = await client.GetAsync("https://jsonplaceholder.typicode.com/posts/1");
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task HttpClient_ShouldHandleInvalidUrl()
    {
        // 测试 HttpClient 处理无效 URL
        using var client = new Old8Lang.NetLib.HttpClient();
        
        // 设置超时时间
        client.SetTimeout(1000);
        
        // 使用无效 URL，应该抛出异常
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
        {
            await client.GetAsync("invalid-url");
        });
    }

    [Fact]
    public async Task HttpClient_ShouldHandleTimeout()
    {
        // 测试 HttpClient 超时处理
        using var client = new Old8Lang.NetLib.HttpClient();
        
        // 设置非常短的超时时间
        client.SetTimeout(10);
        
        // 使用一个会超时的 URL
        await Assert.ThrowsAsync<TaskCanceledException>(async () => 
        {
            await client.GetAsync("https://httpbin.org/delay/1");
        });
    }
}