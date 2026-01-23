using System.Net.WebSockets;

namespace Old8Lang.NetLib;

/// <summary>
/// WebSocket客户端类，用于WebSocket通信
/// </summary>
public class WebSocketClient : IAsyncDisposable
{
    private readonly ClientWebSocket _client;
    private readonly string _url;
    private Action<string>? _textMessageReceivedHandler;
    private Action<byte[]>? _binaryMessageReceivedHandler;
    private Action<string>? _connectedHandler;
    private Action<string>? _disconnectedHandler;
    private Action<Exception>? _errorHandler;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;

    /// <summary>
    /// 获取客户端连接状态
    /// </summary>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public WebSocketClient(string url)
    {
        _url = url;
        _client = new ClientWebSocket();
    }

    /// <summary>
    /// 连接到WebSocket服务器
    /// </summary>
    public async Task ConnectAsync(Dictionary<string, string>? headers = null)
    {
        _cts = new CancellationTokenSource();

        // 添加自定义头
        if (headers != null)
        {
            foreach (var header in headers)
            {
                // 只添加允许的自定义头（WebSocket协议限制）
                if (header.Key.StartsWith("Sec-WebSocket-", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.StartsWith("Authorization", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("Origin", StringComparison.OrdinalIgnoreCase))
                {
                    _client.Options.SetRequestHeader(header.Key, header.Value);
                }
            }
        }

        await _client.ConnectAsync(new Uri(_url), _cts.Token);
        IsConnected = true;
        _connectedHandler?.Invoke("Connected to WebSocket server");

        // 启动接收循环
        _receiveTask = ReceiveLoopAsync();
    }

    /// <summary>
    /// 接收消息循环
    /// </summary>
    private async Task ReceiveLoopAsync()
    {
        var buffer = new byte[1024 * 4];
        var segment = new ArraySegment<byte>(buffer);
        var messageBytes = new List<byte>();

        try
        {
            while (_client.State == WebSocketState.Open && !_cts!.IsCancellationRequested)
            {
                var result = await _client.ReceiveAsync(segment, _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    IsConnected = false;
                    _disconnectedHandler?.Invoke("WebSocket connection closed");
                    break;
                }

                if (result.Count > 0)
                {
                    // 收集消息数据
                    messageBytes.AddRange(segment.Take(result.Count));

                    // 如果是消息的最后一部分，处理消息
                    if (result.EndOfMessage)
                    {
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            // 处理文本消息
                            var message = System.Text.Encoding.UTF8.GetString(messageBytes.ToArray());
                            _textMessageReceivedHandler?.Invoke(message);
                        }
                        else if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            // 处理二进制消息
                            var binaryData = messageBytes.ToArray();
                            _binaryMessageReceivedHandler?.Invoke(binaryData);
                        }

                        // 重置消息缓冲区
                        messageBytes.Clear();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，不处理
        }
        catch (Exception ex)
        {
            _errorHandler?.Invoke(ex);
            IsConnected = false;
            _disconnectedHandler?.Invoke($"WebSocket error: {ex.Message}");
        }
    }

    /// <summary>
    /// 发送文本消息
    /// </summary>
    public async Task SendAsync(string message)
    {
        if (!IsConnected || _client.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket client is not connected");
        }

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(buffer);
        await _client.SendAsync(segment, WebSocketMessageType.Text, true, _cts!.Token);
    }

    /// <summary>
    /// 发送二进制消息
    /// </summary>
    public async Task SendBinaryAsync(byte[] data)
    {
        if (!IsConnected || _client.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket client is not connected");
        }

        if (data == null)
        {
            throw new ArgumentNullException(nameof(data), "二进制数据不能为空");
        }

        var segment = new ArraySegment<byte>(data);
        await _client.SendAsync(segment, WebSocketMessageType.Binary, true, _cts!.Token);
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (IsConnected)
        {
            await _cts!.CancelAsync();

            if (_client.State == WebSocketState.Open)
            {
                await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }

            IsConnected = false;
            _disconnectedHandler?.Invoke("Disconnected from WebSocket server");
        }
    }

    /// <summary>
    /// 设置文本消息接收事件处理程序
    /// </summary>
    public void SetTextMessageReceivedHandler(Action<string> handler)
    {
        _textMessageReceivedHandler = handler;
    }

    /// <summary>
    /// 设置二进制消息接收事件处理程序
    /// </summary>
    public void SetBinaryMessageReceivedHandler(Action<byte[]> handler)
    {
        _binaryMessageReceivedHandler = handler;
    }

    /// <summary>
    /// 设置消息接收事件处理程序（兼容旧版本）
    /// </summary>
    public void SetMessageReceivedHandler(Action<string> handler)
    {
        _textMessageReceivedHandler = handler;
    }

    /// <summary>
    /// 设置连接事件处理程序
    /// </summary>
    public void SetConnectedHandler(Action<string> handler)
    {
        _connectedHandler = handler;
    }

    /// <summary>
    /// 设置断开连接事件处理程序
    /// </summary>
    public void SetDisconnectedHandler(Action<string> handler)
    {
        _disconnectedHandler = handler;
    }

    /// <summary>
    /// 设置错误事件处理程序
    /// </summary>
    public void SetErrorHandler(Action<Exception> handler)
    {
        _errorHandler = handler;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(_client);
        if (_cts != null) await CastAndDispose(_cts);
        if (_receiveTask != null) await CastAndDispose(_receiveTask);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }
}