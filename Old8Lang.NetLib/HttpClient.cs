namespace Old8Lang.NetLib;

/// <summary>
/// HTTP客户端类，用于发送HTTP请求
/// </summary>
public class HttpClient : IDisposable
{
    private System.Net.Http.HttpClient _client;
    private Dictionary<string, string> _defaultHeaders;

    /// <summary>
    /// 构造函数
    /// </summary>
    public HttpClient()
    {
        _client = new System.Net.Http.HttpClient();
        _defaultHeaders = new Dictionary<string, string>();
    }

    /// <summary>
    /// 添加默认请求头
    /// </summary>
    public void AddDefaultHeader(string name, string value)
    {
        _defaultHeaders[name] = value;
    }

    /// <summary>
    /// 移除默认请求头
    /// </summary>
    public void RemoveDefaultHeader(string name)
    {
        _defaultHeaders.Remove(name);
    }

    /// <summary>
    /// 设置超时时间（毫秒）
    /// </summary>
    public void SetTimeout(int milliseconds)
    {
        _client.Timeout = TimeSpan.FromMilliseconds(milliseconds);
    }

    /// <summary>
    /// 发送GET请求
    /// </summary>
    public async Task<HttpResponse> GetAsync(string url)
    {
        return await SendAsync(HttpMethod.Get, url, null, null);
    }

    /// <summary>
    /// 发送POST请求
    /// </summary>
    public async Task<HttpResponse> PostAsync(string url, string content, string contentType = "application/json")
    {
        return await SendAsync(HttpMethod.Post, url, content, contentType);
    }

    /// <summary>
    /// 发送PUT请求
    /// </summary>
    public async Task<HttpResponse> PutAsync(string url, string content, string contentType = "application/json")
    {
        return await SendAsync(HttpMethod.Put, url, content, contentType);
    }

    /// <summary>
    /// 发送DELETE请求
    /// </summary>
    public async Task<HttpResponse> DeleteAsync(string url)
    {
        return await SendAsync(HttpMethod.Delete, url, null, null);
    }

    /// <summary>
    /// 发送请求
    /// </summary>
    private async Task<HttpResponse> SendAsync(HttpMethod method, string url, string? content, string? contentType)
    {
        using (HttpRequestMessage request = new HttpRequestMessage(method, url))
        {
            // 添加默认请求头
            foreach (var header in _defaultHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // 添加请求内容
            if (!string.IsNullOrEmpty(content))
            {
                request.Content = new StringContent(content, System.Text.Encoding.UTF8, contentType);
            }

            // 发送请求
            var response = await _client.SendAsync(request);

            // 构建响应对象
            return new HttpResponse
            {
                StatusCode = (int)response.StatusCode,
                Content = await response.Content.ReadAsStringAsync(),
                Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                IsSuccessStatusCode = response.IsSuccessStatusCode
            };
        }
    }

    /// <summary>
    /// 获取底层System.Net.Http.HttpClient实例，用于高级操作
    /// </summary>
    public System.Net.Http.HttpClient GetUnderlyingClient()
    {
        return _client;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _client.Dispose();
    }
}

/// <summary>
/// HTTP响应类，用于封装HTTP响应结果
/// </summary>
public class HttpResponse
{
    /// <summary>
    /// 状态码
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// 响应内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 响应头
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccessStatusCode { get; set; }

    /// <summary>
    /// 将响应内容转换为字符串
    /// </summary>
    public override string ToString()
    {
        return Content;
    }
}