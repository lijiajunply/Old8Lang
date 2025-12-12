using System.Net.Sockets;

namespace Old8Lang.NetLib;

/// <summary>
/// Socket客户端类，用于TCP Socket通信
/// </summary>
public class SocketClient : IDisposable
{
    private TcpClient _client;
    private NetworkStream _stream;
    private readonly string _host;
    private readonly int _port;
    private bool _isConnected;

    /// <summary>
    /// 获取客户端连接状态
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SocketClient(string host, int port)
    {
        _host = host;
        _port = port;
        _client = new TcpClient();
    }

    /// <summary>
    /// 连接到服务器
    /// </summary>
    public async Task ConnectAsync()
    {
        await _client.ConnectAsync(_host, _port);
        _stream = _client.GetStream();
        _isConnected = true;
    }

    /// <summary>
    /// 发送数据
    /// </summary>
    public async Task SendAsync(string data)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Socket client is not connected");
        }

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data);
        await _stream.WriteAsync(buffer, 0, buffer.Length);
    }

    /// <summary>
    /// 接收数据
    /// </summary>
    public async Task<string> ReceiveAsync(int bufferSize = 1024)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Socket client is not connected");
        }

        byte[] buffer = new byte[bufferSize];
        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
        return System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        if (_isConnected)
        {
            _stream?.Close();
            _client?.Close();
            _isConnected = false;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Disconnect();
        _client?.Dispose();
    }
}