using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol; // 可以在 .NET 10 的 csproj 中隐式引用

namespace Old8Lang.NetLib;

/// <summary>
/// 基于 MQTTnet 的现代化 MQTT 客户端封装
/// </summary>
/// <remarks>
/// 专为 .NET 8/9/10 设计，使用 ILogger 和 异步事件流。
/// </remarks>
public class MqttClientWrapper : IDisposable, IAsyncDisposable
{
    private readonly IMqttClient Client;
    private readonly string Server;
    private readonly int Port;
    private MqttClientOptions? Options;

    // ==========================================
    // 标准 C# 事件 - 允许外部使用 += 订阅
    // ==========================================

    /// <summary>
    /// 当收到消息时触发 (Topic, Payload)
    /// </summary>
    public event Func<string, string, Task>? MessageReceivedAsync;

    /// <summary>
    /// 当连接成功时触发
    /// </summary>
    public event Func<Task>? ConnectedAsync;

    /// <summary>
    /// 当连接断开时触发
    /// </summary>
    public event Func<Task>? DisconnectedAsync;

    /// <summary>
    /// 获取当前连接状态
    /// </summary>
    private bool IsConnected => Client.IsConnected;

    /// <summary>
    /// 使用主构造函数风格初始化的构造函数
    /// </summary>
    /// <param name="server">服务器地址</param>
    /// <param name="port">端口</param>
    public MqttClientWrapper(string server, int port)
    {
        Server = server;
        Port = port;

        // 创建客户端实例
        var factory = new MqttFactory();
        Client = factory.CreateMqttClient();

        // 内部挂载事件处理
        ConfigureInternalHandlers();
    }

    private void ConfigureInternalHandlers()
    {
        Client.ConnectedAsync += async _ =>
        {
            if (ConnectedAsync != null)
            {
                await ConnectedAsync.Invoke();
            }
        };

        Client.DisconnectedAsync += async _ =>
        {
            if (DisconnectedAsync != null)
            {
                await DisconnectedAsync.Invoke();
            }
        };

        Client.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic;
            // 优化：使用 Span/Memory 避免不必要的分配，但在公共 API 转换回 string 以方便使用
            var payload = e.ApplicationMessage.PayloadSegment.Count > 0
                ? Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)
                : string.Empty;

            if (MessageReceivedAsync != null)
            {
                await MessageReceivedAsync.Invoke(topic, payload);
            }

            // 确认收到消息 (如果是 QoS 1/2 需要自动 ACK，MQTTnet 默认行为通常是 AutoAcknowledge)
            // e.AutoAcknowledge = true; 
        };
    }

    /// <summary>
    /// 连接到 MQTT 服务器
    /// </summary>
    /// <param name="clientId">客户端ID，留空则自动生成</param>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="timeoutSeconds">超时时间(秒)</param>
    /// <param name="ct">取消令牌</param>
    public async Task ConnectAsync(
        string? clientId = null,
        string? username = null,
        string? password = null,
        int timeoutSeconds = 30,
        CancellationToken ct = default)
    {
        if (IsConnected)
        {
            return;
        }

        var actualClientId = string.IsNullOrWhiteSpace(clientId) ? Guid.NewGuid().ToString() : clientId;

        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(Server, Port)
            .WithClientId(actualClientId)
            .WithTimeout(TimeSpan.FromSeconds(timeoutSeconds))
            // 保持连接 (KeepAlive) 心跳设置
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(15));

        if (!string.IsNullOrEmpty(username))
        {
            builder.WithCredentials(username, password);
        }

        Options = builder.Build();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        await Client.ConnectAsync(Options, timeoutCts.Token);
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (!IsConnected) return;

        try
        {
            await Client.DisconnectAsync(new MqttClientDisconnectOptionsBuilder()
                .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                .Build(), ct);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    /// <summary>
    /// 订阅主题
    /// </summary>
    public async Task SubscribeAsync(string topic, int qos = 0, CancellationToken ct = default)
    {
        if (!IsConnected) throw new InvalidOperationException("MQTT client is not connected");

        var topicFilter = new MqttTopicFilterBuilder()
            .WithTopic(topic)
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos)
            .Build();

        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(topicFilter)
            .Build();

        await Client.SubscribeAsync(subscribeOptions, ct);
    }

    /// <summary>
    /// 取消订阅
    /// </summary>
    public async Task UnsubscribeAsync(string topic, CancellationToken ct = default)
    {
        if (!IsConnected) throw new InvalidOperationException("MQTT client is not connected");

        await Client.UnsubscribeAsync(topic, ct);
    }

    /// <summary>
    /// 发布消息
    /// </summary>
    public async Task PublishAsync(string topic, string payload, int qos = 0, bool retain = false,
        CancellationToken ct = default)
    {
        if (!IsConnected) throw new InvalidOperationException("MQTT client is not connected");

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos)
            .WithRetainFlag(retain)
            .Build();

        await Client.PublishAsync(message, ct);
    }

    // ==========================================
    // 资源释放
    // ==========================================

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Client.Dispose();
        }
    }

    // 实现新的 IAsyncDisposable 接口 (.NET Core 3.0+ 标准)
    public async ValueTask DisposeAsync()
    {
        // 如果还连着，先优雅断开
        if (IsConnected)
        {
            await DisconnectAsync();
        }

        Client.Dispose();

        GC.SuppressFinalize(this);
    }
}