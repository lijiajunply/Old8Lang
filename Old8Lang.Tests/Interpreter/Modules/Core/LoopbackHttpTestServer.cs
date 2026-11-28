using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Old8Lang.Tests.Interpreter.Modules.Core;

/// <summary>
/// 进程内本地 HTTP 回环测试桩，用于替代外网依赖（如 jsonplaceholder.typicode.com），
/// 保证 NetLibTests 在离线与受限网络环境下稳定运行。
/// </summary>
public sealed class LoopbackHttpTestServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _acceptLoop;

    public string BaseUrl { get; }

    public LoopbackHttpTestServer()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        BaseUrl = $"http://127.0.0.1:{port}/";
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }

            _ = Task.Run(() => HandleClientAsync(client, ct), ct);
        }
    }

    private static async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        await using (var stream = client.GetStream())
        {
            try
            {
                var buffer = new byte[8192];
                var headerBuilder = new MemoryStream();
                var headerEndIndex = -1;

                while (headerEndIndex < 0)
                {
                    var read = await stream.ReadAsync(buffer, ct).ConfigureAwait(false);
                    if (read == 0) return;
                    await headerBuilder.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                    headerEndIndex = FindHeaderEnd(headerBuilder.GetBuffer(), (int)headerBuilder.Length);
                    if (headerBuilder.Length > 64 * 1024) return;
                }

                var rawBytes = headerBuilder.GetBuffer();
                var totalLength = (int)headerBuilder.Length;
                var headerText = Encoding.ASCII.GetString(rawBytes, 0, headerEndIndex);
                var lines = headerText.Split("\r\n");
                var requestLineParts = lines[0].Split(' ');
                var method = requestLineParts.Length > 0 ? requestLineParts[0] : "GET";
                var path = requestLineParts.Length > 1 ? requestLineParts[1] : "/";

                var contentLength = 0;
                for (var i = 1; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(lines[i].Substring("Content-Length:".Length).Trim(), out var parsed))
                    {
                        contentLength = parsed;
                    }
                }

                var bodyStart = headerEndIndex + 4;
                var bodyBytesReceived = totalLength - bodyStart;
                var bodyStream = new MemoryStream();
                if (bodyBytesReceived > 0)
                {
                    await bodyStream.WriteAsync(rawBytes.AsMemory(bodyStart, bodyBytesReceived), ct).ConfigureAwait(false);
                }

                while (bodyStream.Length < contentLength)
                {
                    var remaining = contentLength - (int)bodyStream.Length;
                    var read = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, remaining)), ct)
                        .ConfigureAwait(false);
                    if (read == 0) break;
                    await bodyStream.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                }

                var (statusLine, responseBody) = BuildResponse(method, path, Encoding.UTF8.GetString(bodyStream.ToArray()));
                var responseBodyBytes = Encoding.UTF8.GetBytes(responseBody);
                var responseHeaders =
                    $"HTTP/1.1 {statusLine}\r\n" +
                    "Content-Type: application/json; charset=utf-8\r\n" +
                    $"Content-Length: {responseBodyBytes.Length}\r\n" +
                    "Connection: close\r\n\r\n";
                var headerBytes = Encoding.ASCII.GetBytes(responseHeaders);
                await stream.WriteAsync(headerBytes, ct).ConfigureAwait(false);
                await stream.WriteAsync(responseBodyBytes, ct).ConfigureAwait(false);
                await stream.FlushAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                // 客户端断开连接或测试结束时忽略
            }
        }
    }

    private static (string statusLine, string body) BuildResponse(string method, string path, string requestBody)
    {
        return (method.ToUpperInvariant(), path) switch
        {
            ("GET", "/posts/1") => ("200 OK", """{"id":1,"userId":1,"title":"post 1","body":"body 1"}"""),
            ("GET", "/posts/2") => ("200 OK", """{"id":2,"userId":1,"title":"post 2","body":"body 2"}"""),
            ("POST", "/posts") => ("201 Created", string.IsNullOrWhiteSpace(requestBody) ? """{"id":101}""" : requestBody),
            ("PUT", "/posts/1") => ("200 OK", string.IsNullOrWhiteSpace(requestBody) ? """{"id":1}""" : requestBody),
            ("DELETE", "/posts/1") => ("200 OK", "{}"),
            _ => ("200 OK", """{"ok":true}""")
        };
    }

    private static int FindHeaderEnd(byte[] buffer, int length)
    {
        for (var i = 0; i <= length - 4; i++)
        {
            if (buffer[i] == '\r' && buffer[i + 1] == '\n' && buffer[i + 2] == '\r' && buffer[i + 3] == '\n')
            {
                return i;
            }
        }

        return -1;
    }

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _listener.Stop();
        }
        catch
        {
            // 忽略关闭异常
        }

        try
        {
            // Dispose 只能是同步的；测试桩已在上面 Cancel + Stop，
            // 接受循环会很快退出，这里同步等待只为确保端口资源释放完成。
#pragma warning disable VSTHRD002 // Dispose 无法异步，且该测试桩不运行在 UI 线程上
            _acceptLoop.Wait(TimeSpan.FromSeconds(1));
#pragma warning restore VSTHRD002
        }
        catch
        {
            // 忽略等待取消异常
        }

        _cts.Dispose();
    }
}
