using System.Net.WebSockets;

namespace Old8Lang.NetLib;

/// <summary>
/// WebSocket客户端类，用于WebSocket通信
/// </summary>
public class WebSocketClient : IAsyncDisposable
{
    private readonly ClientWebSocket Client;
    private readonly string Url;
    private Action<string>? TextMessageReceivedHandler;
    private Action<byte[]>? BinaryMessageReceivedHandler;
    private Action<string>? ConnectedHandler;
    private Action<string>? DisconnectedHandler;
    private Action<Exception>? ErrorHandler;
    private CancellationTokenSource? Cts;
    private Task? ReceiveTask;

    /// <summary>
    /// 获取客户端连接状态
    /// </summary>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public WebSocketClient(string url)
    {
        Url = url;
        Client = new ClientWebSocket();
    }

    /// <summary>
    /// 连接到WebSocket服务器
    /// </summary>
    public async Task ConnectAsync(Dictionary<string, string>? headers = null)
    {
        Cts = new CancellationTokenSource();

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
                    Client.Options.SetRequestHeader(header.Key, header.Value);
                }
            }
        }

        await Client.ConnectAsync(new Uri(Url), Cts.Token);
        IsConnected = true;
        ConnectedHandler?.Invoke("Connected to WebSocket server");

        // 启动接收循环
        ReceiveTask = ReceiveLoopAsync();
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
            while (Client.State == WebSocketState.Open && !Cts!.IsCancellationRequested)
            {
                var result = await Client.ReceiveAsync(segment, Cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    IsConnected = false;
                    DisconnectedHandler?.Invoke("WebSocket connection closed");
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
                            TextMessageReceivedHandler?.Invoke(message);
                        }
                        else if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            // 处理二进制消息
                            var binaryData = messageBytes.ToArray();
                            BinaryMessageReceivedHandler?.Invoke(binaryData);
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
            ErrorHandler?.Invoke(ex);
            IsConnected = false;
            DisconnectedHandler?.Invoke($"WebSocket error: {ex.Message}");
        }
    }

    /// <summary>
    /// 发送文本消息
    /// </summary>
    public async Task SendAsync(string message)
    {
        if (!IsConnected || Client.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket client is not connected");
        }

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(buffer);
        await Client.SendAsync(segment, WebSocketMessageType.Text, true, Cts!.Token);
    }

    /// <summary>
    /// 发送二进制消息
    /// </summary>
    public async Task SendBinaryAsync(byte[] data)
    {
        if (!IsConnected || Client.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket client is not connected");
        }

        if (data == null)
        {
            throw new ArgumentNullException(nameof(data), "二进制数据不能为空");
        }

        var segment = new ArraySegment<byte>(data);
        await Client.SendAsync(segment, WebSocketMessageType.Binary, true, Cts!.Token);
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (IsConnected)
        {
            await Cts!.CancelAsync();

            if (Client.State == WebSocketState.Open)
            {
                await Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }

            IsConnected = false;
            DisconnectedHandler?.Invoke("Disconnected from WebSocket server");
        }
    }

    /// <summary>
    /// 设置文本消息接收事件处理程序
    /// </summary>
    public void SetTextMessageReceivedHandler(Action<string> handler)
    {
        TextMessageReceivedHandler = handler;
    }

    /// <summary>
    /// 设置二进制消息接收事件处理程序
    /// </summary>
    public void SetBinaryMessageReceivedHandler(Action<byte[]> handler)
    {
        BinaryMessageReceivedHandler = handler;
    }

    /// <summary>
    /// 设置消息接收事件处理程序（兼容旧版本）
    /// </summary>
    public void SetMessageReceivedHandler(Action<string> handler)
    {
        TextMessageReceivedHandler = handler;
    }

    /// <summary>
    /// 设置连接事件处理程序
    /// </summary>
    public void SetConnectedHandler(Action<string> handler)
    {
        ConnectedHandler = handler;
    }

    /// <summary>
    /// 设置断开连接事件处理程序
    /// </summary>
    public void SetDisconnectedHandler(Action<string> handler)
    {
        DisconnectedHandler = handler;
    }

    /// <summary>
    /// 设置错误事件处理程序
    /// </summary>
    public void SetErrorHandler(Action<Exception> handler)
    {
        ErrorHandler = handler;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(Client);
        if (Cts != null) await CastAndDispose(Cts);
        if (ReceiveTask != null) await CastAndDispose(ReceiveTask);

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