namespace Old8Lang.NetLib;

/// <summary>
/// WebAPI客户端类，用于简化WebAPI调用
/// </summary>
public class WebApiClient : IDisposable
{
    private readonly HttpWebClient _httpWebClient;
    private readonly string _baseUrl;
    private readonly Dictionary<string, string> _defaultHeaders;
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WebApiClient(string baseUrl)
    {
        _baseUrl = EnsureTrailingSlash(baseUrl);
        _httpWebClient = new HttpWebClient();
        _defaultHeaders = new Dictionary<string, string>
        {
            { "Accept", "application/json" },
            { "Content-Type", "application/json" }
        };

        // 添加默认请求头
        foreach (var header in _defaultHeaders)
        {
            _httpWebClient.AddDefaultHeader(header.Key, header.Value);
        }
    }

    /// <summary>
    /// 确保URL以斜杠结尾
    /// </summary>
    private string EnsureTrailingSlash(string url)
    {
        if (!url.EndsWith("/"))
        {
            return url + "/";
        }

        return url;
    }

    /// <summary>
    /// 添加默认请求头
    /// </summary>
    public void AddDefaultHeader(string name, string value)
    {
        _defaultHeaders[name] = value;
        _httpWebClient.AddDefaultHeader(name, value);
    }

    /// <summary>
    /// 移除默认请求头
    /// </summary>
    public void RemoveDefaultHeader(string name)
    {
        _defaultHeaders.Remove(name);
        _httpWebClient.RemoveDefaultHeader(name);
    }

    /// <summary>
    /// 设置超时时间（毫秒）
    /// </summary>
    public void SetTimeout(int milliseconds)
    {
        _httpWebClient.SetTimeout(milliseconds);
    }

    /// <summary>
    /// 构建完整的URL
    /// </summary>
    private string BuildUrl(string endpoint, Dictionary<string, object>? pathParams = null,
        Dictionary<string, object>? queryParams = null)
    {
        string url = _baseUrl + endpoint;

        // 替换路径参数
        if (pathParams is { Count: > 0 })
        {
            url = pathParams.Aggregate(url,
                (current, param) => current.Replace($"{{{param.Key}}}", param.Value.ToString()!));
        }

        // 添加查询参数
        if (queryParams is { Count: > 0 })
        {
            var queryString = string.Join("&",
                queryParams.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value.ToString()!)}"));
            url += "?" + queryString;
        }

        return url;
    }

    /// <summary>
    /// 发送GET请求
    /// </summary>
    public async Task<HttpResponse> GetAsync(string endpoint, Dictionary<string, object>? pathParams = null,
        Dictionary<string, object>? queryParams = null)
    {
        string url = BuildUrl(endpoint, pathParams, queryParams);
        return await _httpWebClient.GetAsync(url);
    }

    /// <summary>
    /// 发送POST请求
    /// </summary>
    public async Task<HttpResponse> PostAsync(string endpoint, object? requestBody = null,
        Dictionary<string, object>? pathParams = null, Dictionary<string, object>? queryParams = null)
    {
        string url = BuildUrl(endpoint, pathParams, queryParams);
        string content = requestBody != null ? System.Text.Json.JsonSerializer.Serialize(requestBody) : string.Empty;
        return await _httpWebClient.PostAsync(url, content);
    }

    /// <summary>
    /// 发送PUT请求
    /// </summary>
    public async Task<HttpResponse> PutAsync(string endpoint, object? requestBody = null,
        Dictionary<string, object>? pathParams = null, Dictionary<string, object>? queryParams = null)
    {
        string url = BuildUrl(endpoint, pathParams, queryParams);
        string content = requestBody != null ? System.Text.Json.JsonSerializer.Serialize(requestBody) : string.Empty;
        return await _httpWebClient.PutAsync(url, content);
    }

    /// <summary>
    /// 发送DELETE请求
    /// </summary>
    public async Task<HttpResponse> DeleteAsync(string endpoint, Dictionary<string, object>? pathParams = null,
        Dictionary<string, object>? queryParams = null)
    {
        string url = BuildUrl(endpoint, pathParams, queryParams);
        return await _httpWebClient.DeleteAsync(url);
    }

    /// <summary>
    /// 发送PATCH请求
    /// </summary>
    public async Task<HttpResponse> PatchAsync(string endpoint, object? requestBody = null,
        Dictionary<string, object>? pathParams = null, Dictionary<string, object>? queryParams = null)
    {
        string url = BuildUrl(endpoint, pathParams, queryParams);
        string content = requestBody != null ? System.Text.Json.JsonSerializer.Serialize(requestBody) : string.Empty;

        // 使用HttpClient的底层方法发送PATCH请求
        var httpMethod = new HttpMethod("PATCH");
        using var httpRequest = new HttpRequestMessage(httpMethod, url);
        if (!string.IsNullOrEmpty(content))
        {
            httpRequest.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
        }

        // 添加默认请求头
        foreach (var header in _defaultHeaders)
        {
            httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var response = await _httpWebClient.GetUnderlyingClient().SendAsync(httpRequest);

        return new HttpResponse
        {
            StatusCode = (int)response.StatusCode,
            Content = await response.Content.ReadAsStringAsync(),
            Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
            IsSuccessStatusCode = response.IsSuccessStatusCode
        };
    }

    /// <summary>
    /// 获取底层HttpClient实例，用于高级操作
    /// </summary>
    public HttpClient GetUnderlyingClient()
    {
        // 这里需要修改HttpClient类，添加一个方法来获取底层的System.Net.Http.HttpClient实例
        return _httpWebClient.GetUnderlyingClient();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpWebClient.Dispose();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 析构函数
    /// </summary>
    ~WebApiClient()
    {
        Dispose(false);
    }
}