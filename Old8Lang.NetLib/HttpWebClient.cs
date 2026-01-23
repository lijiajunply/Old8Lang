using System.Net.Http.Headers;

namespace Old8Lang.NetLib;

/// <summary>
/// HTTP客户端类，用于发送HTTP请求
/// </summary>
public class HttpWebClient : IDisposable
{
    private readonly HttpClient _client;
    private readonly Dictionary<string, string> _defaultHeaders;

    /// <summary>
    /// 构造函数
    /// </summary>
    public HttpWebClient()
    {
        _client = new HttpClient();
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
    /// 下载文件到本地路径
    /// </summary>
    /// <param name="url">文件URL</param>
    /// <param name="localPath">本地保存路径</param>
    /// <returns>HTTP响应</returns>
    public async Task<HttpResponse> DownloadFileAsync(string url, string localPath)
    {
        if (string.IsNullOrEmpty(localPath))
        {
            throw new ArgumentNullException(nameof(localPath), "本地路径不能为空");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        // 添加默认请求头
        foreach (var header in _defaultHeaders)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // 发送请求
        var response = await _client.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 下载文件
            await using var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fileStream);
        }

        // 构建响应对象
        return new HttpResponse
        {
            StatusCode = (int)response.StatusCode,
            Content = response.IsSuccessStatusCode
                ? "File downloaded successfully"
                : await response.Content.ReadAsStringAsync(),
            Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
            IsSuccessStatusCode = response.IsSuccessStatusCode
        };
    }

    /// <summary>
    /// 上传单个文件
    /// </summary>
    /// <param name="url">目标URL</param>
    /// <param name="filePath">本地文件路径</param>
    /// <param name="parameterName">表单参数名，默认为"file"</param>
    /// <param name="fileName">上传后的文件名，默认为原文件名</param>
    /// <param name="additionalFields">额外的表单字段</param>
    /// <returns>HTTP响应</returns>
    public async Task<HttpResponse> UploadFileAsync(string url, string filePath, string parameterName = "file",
        string? fileName = null, Dictionary<string, string>? additionalFields = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"文件不存在: '{filePath}'", filePath);
        }

        // 使用MultipartFormDataContent上传文件
        using var content = new MultipartFormDataContent();
        // 添加额外的表单字段
        if (additionalFields != null)
        {
            foreach (var field in additionalFields)
            {
                content.Add(new StringContent(field.Value), field.Key);
            }
        }

        // 添加文件内容
        var fileContent = new StreamContent(File.OpenRead(filePath));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var uploadFileName = string.IsNullOrEmpty(fileName) ? Path.GetFileName(filePath) : fileName;
        content.Add(fileContent, parameterName, uploadFileName);

        return await SendRequestWithContentAsync(HttpMethod.Post, url, content);
    }

    /// <summary>
    /// 上传多个文件
    /// </summary>
    /// <param name="url">目标URL</param>
    /// <param name="filePaths">本地文件路径列表</param>
    /// <param name="parameterName">表单参数名，默认为"files"</param>
    /// <param name="additionalFields">额外的表单字段</param>
    /// <returns>HTTP响应</returns>
    public async Task<HttpResponse> UploadFilesAsync(string url, IEnumerable<string> filePaths,
        string parameterName = "files", Dictionary<string, string>? additionalFields = null)
    {
        var enumerable = filePaths as string[] ?? filePaths.ToArray();
        if (filePaths == null || enumerable.Length == 0)
        {
            throw new ArgumentException("文件路径列表不能为空", nameof(filePaths));
        }

        // 检查所有文件是否存在
        foreach (var filePath in enumerable)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"文件不存在: '{filePath}'", filePath);
            }
        }

        // 使用MultipartFormDataContent上传文件
        using var content = new MultipartFormDataContent();
        // 添加额外的表单字段
        if (additionalFields != null)
        {
            foreach (var field in additionalFields)
            {
                content.Add(new StringContent(field.Value), field.Key);
            }
        }

        // 添加多个文件
        foreach (var filePath in enumerable)
        {
            var fileContent = new StreamContent(File.OpenRead(filePath));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, parameterName, Path.GetFileName(filePath));
        }

        return await SendRequestWithContentAsync(HttpMethod.Post, url, content);
    }

    /// <summary>
    /// 发送带内容的请求
    /// </summary>
    private async Task<HttpResponse> SendRequestWithContentAsync(HttpMethod method, string url, HttpContent content)
    {
        using var request = new HttpRequestMessage(method, url);
        // 添加默认请求头
        foreach (var header in _defaultHeaders)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // 设置请求内容
        request.Content = content;

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

    /// <summary>
    /// 发送请求
    /// </summary>
    private async Task<HttpResponse> SendAsync(HttpMethod method, string url, string? content, string? contentType)
    {
        using HttpRequestMessage request = new HttpRequestMessage(method, url);
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

    /// <summary>
    /// 获取底层System.Net.Http.HttpClient实例，用于高级操作
    /// </summary>
    public HttpClient GetUnderlyingClient()
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