using System.Net.Sockets;

namespace Old8Lang.NetLib;

/// <summary>
/// Socket客户端类，用于TCP Socket通信
/// </summary>
public class SocketClient : IDisposable
{
    private readonly TcpClient Client;
    private NetworkStream? Stream;
    private readonly string Host;
    private readonly int Port;

    /// <summary>
    /// 获取客户端连接状态
    /// </summary>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public SocketClient(string host, int port)
    {
        Host = host;
        Port = port;
        Client = new TcpClient();
    }

    /// <summary>
    /// 连接到服务器
    /// </summary>
    public async Task ConnectAsync()
    {
        await Client.ConnectAsync(Host, Port);
        Stream = Client.GetStream();
        IsConnected = true;
    }

    /// <summary>
    /// 发送数据
    /// </summary>
    public async Task SendAsync(string data)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Socket client is not connected");
        }

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data);
        await Stream?.WriteAsync(buffer, 0, buffer.Length)!;
    }

    /// <summary>
    /// 接收数据
    /// </summary>
    public async Task<string> ReceiveAsync(int bufferSize = 1024)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Socket client is not connected");
        }

        byte[] buffer = new byte[bufferSize];
        int bytesRead = await Stream?.ReadAsync(buffer, 0, buffer.Length)!;
        return System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        if (IsConnected)
        {
            Stream?.Close();
            Client.Close();
            IsConnected = false;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Disconnect();
        Client.Dispose();
    }
}