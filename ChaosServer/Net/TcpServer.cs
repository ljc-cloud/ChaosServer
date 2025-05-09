using System.Net;
using System.Net.Sockets;
using System.Timers;
using ChaosServer.Controller;
using SocketProtocol;
using Timer = System.Timers.Timer;

namespace ChaosServer.Net;

public class TcpServer : IDisposable
{
    public const int HEARTBEAT_TIMEINTERVAL = 5000;
    /// <summary>
    /// 服务端Socket
    /// </summary>
    private Socket _mSocket;
    /// <summary>
    /// 客户端连接集合
    /// </summary>
    private List<TcpClient> _mClientList;

    private Dictionary<TcpClient, DateTime> _mLastHeartbeatTime;

    private int _mCurrentConnectedClients;

    private int _mCurrentClientId;

    private readonly Logger _mLogger;
        
    public TcpServer(int port)
    {
        _mLogger = new Logger(GetType());
        
        _mClientList = new List<TcpClient>();
        _mLastHeartbeatTime = new Dictionary<TcpClient, DateTime>();
        _mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _mSocket.Bind(new IPEndPoint(IPAddress.Any, port));
        // 指定挂起连接队列的最大长度。
        // 当多个客户端同时尝试连接服务器时，服务器可能无法立即处理所有连接请求。
        // 这些未处理的连接会被放入一个队列,参数决定了这个队列的最大长度。
        // 如果队列已满，新的连接请求会被拒绝。
        _mSocket.Listen(100);
        _mLogger.LogInfo($"TCP server listening on port {port}...");
        StartAccept();

        Timer heartbeatTimer = new Timer(10000);
        heartbeatTimer.Elapsed += CheckHeartbeatTimeout;
        heartbeatTimer.AutoReset = true;
        heartbeatTimer.Enabled = true;
        heartbeatTimer.Start();
    }

    private void CheckHeartbeatTimeout(object? state, ElapsedEventArgs e)
    {
        foreach (var client in _mLastHeartbeatTime.Keys)
        {
            if (!client.Connected || (DateTime.Now - _mLastHeartbeatTime[client]).TotalSeconds > 20)
            {
                _mLogger.LogWarning($"Client {client.ClientId} time out, connection close");
                client.CloseSocket();
                _mLastHeartbeatTime.Remove(client);
                _mClientList.Remove(client);
            }
        }
    }

    /// <summary>
    /// 开始接收来自客户端的连接
    /// </summary>
    private void StartAccept() {
        _mSocket.BeginAccept(AcceptCallback, null);
    }

    /// <summary>
    /// 连接完成回调
    /// </summary>
    /// <param name="iar"></param>
    private void AcceptCallback(IAsyncResult iar) {
        Socket clientSocket = _mSocket.EndAccept(iar);
        IPEndPoint? clientIpEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
        
        Interlocked.Increment(ref _mCurrentConnectedClients);
        Interlocked.Increment(ref _mCurrentClientId);
        
        _mLogger.LogInfo($"Client {clientIpEndPoint.Address.ToString()}:{clientIpEndPoint.Port} success connect to tcp server!");
            
        TcpClient tcpClient = new TcpClient(clientSocket, this, _mCurrentClientId, clientIpEndPoint.Address.ToString());
        tcpClient.OnTcpClientClosed += HandleTcpClientClosed;
            
        _mLastHeartbeatTime[tcpClient] = DateTime.Now;
        _mClientList.Add(tcpClient);
            
        StartAccept();
    }

    private void HandleTcpClientClosed(TcpClient tcpClient)
    {
        _mClientList.Remove(tcpClient);
        if (_mLastHeartbeatTime.ContainsKey(tcpClient))
        {
            _mLastHeartbeatTime.Remove(tcpClient);
        }

        Interlocked.Decrement(ref _mCurrentConnectedClients);
        tcpClient.OnTcpClientClosed -= HandleTcpClientClosed;
        tcpClient = null;
    }

    public void UpdateHeartbeatTime(TcpClient tcpClient)
    {
        if (_mLastHeartbeatTime.ContainsKey(tcpClient))
        {
            _mLastHeartbeatTime[tcpClient] = DateTime.Now;
        }
    }

    public void Dispose()
    {
        _mSocket.Shutdown(SocketShutdown.Both);
        _mSocket.Close();
        _mSocket.Dispose();
    }
}