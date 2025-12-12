using System.Net.WebSockets;

namespace Old8Lang.NetLib;

/// <summary>
/// WebSocket客户端类，用于WebSocket通信
/// </summary>
public class WebSocketClient : IDisposable
{
    private ClientWebSocket _client;
    private readonly string _url;
    private bool _isConnected;
    private Action<string>? _messageReceivedHandler;
    private Action<string>? _connectedHandler;
    private Action<string>? _disconnectedHandler;
    private Action<Exception>? _errorHandler;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;

    /// <summary>
    /// 获取客户端连接状态
    /// </summary>
    public bool IsConnected => _isConnected;

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
        _isConnected = true;
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

        try
        {
            while (_client.State == WebSocketState.Open && !_cts!.IsCancellationRequested)
            {
                var result = await _client.ReceiveAsync(segment, _cts.Token);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    _isConnected = false;
                    _disconnectedHandler?.Invoke("WebSocket connection closed");
                    break;
                }

                if (result.Count > 0)
                {
                    var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _messageReceivedHandler?.Invoke(message);
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
            _isConnected = false;
            _disconnectedHandler?.Invoke($"WebSocket error: {ex.Message}");
        }
    }

    /// <summary>
    /// 发送文本消息
    /// </summary>
    public async Task SendAsync(string message)
    {
        if (!_isConnected || _client.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket client is not connected");
        }

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(buffer);
        await _client.SendAsync(segment, WebSocketMessageType.Text, true, _cts!.Token);
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_isConnected)
        {
            _cts?.Cancel();
            
            if (_client.State == WebSocketState.Open)
            {
                await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            
            _isConnected = false;
            _disconnectedHandler?.Invoke("Disconnected from WebSocket server");
        }
    }

    /// <summary>
    /// 设置消息接收事件处理程序
    /// </summary>
    public void SetMessageReceivedHandler(Action<string> handler)
    {
        _messageReceivedHandler = handler;
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
    public async void Dispose()
    {
        await DisconnectAsync();
        _cts?.Dispose();
        _client?.Dispose();
    }
}