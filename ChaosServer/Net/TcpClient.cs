using System.Net.Sockets;
using ChaosBall.Utility;
using ChaosServer.Controller;
using SocketProtocol;

namespace ChaosServer.Net;

public class TcpClient : IDisposable
{

    private readonly Logger _mLogger;
    
    /// <summary>
    /// 客户端Socket
    /// </summary>
    private Socket _mClientSocket;
    /// <summary>
    /// 服务端引用
    /// </summary>
    private TcpServer _mTcpServer;
    /// <summary>
    /// 消息封装
    /// </summary>
    private Message _mMessage;
    /// <summary>
    /// 客户端ID
    /// </summary>
    private int _mClientId;
    /// <summary>
    /// 控制器管理
    /// </summary>
    private ControllerManager _mControllerManager;
        
    public event Action<TcpClient> OnTcpClientClosed;
    public bool Connected => _mClientSocket.Connected;
    public int ClientId => _mClientId;
    public string ClientIp { get; private set; }

    private int _mCurrentUdpPort;

    public TcpClient(Socket clientSocket, TcpServer tcpServer, int clientId, string clientIp)
    {
        _mLogger = new Logger(GetType());
        
        _mClientSocket = clientSocket;
        _mTcpServer = tcpServer;
        _mMessage = new Message();
        _mClientId = clientId;
        ClientIp = clientIp;
        _mControllerManager = new ControllerManager();

        // _mCurrentUdpPort = ServerConstant.CLIENT_UDP_LISTEN_PORT;
            
        AssignClient();
        StartReceive();
    }

    private void AssignClient()
    {
        ClientPack clientPack = new ClientPack
        {   
            ClientId = _mClientId,
            UdpListenPort = ServerConstant.CLIENT_UDP_LISTEN_PORT,
        };
        _mCurrentUdpPort++;
        MainPack mainPack = new MainPack
        {
            ActionCode = ActionCode.AssignClient,
            ClientPack = clientPack,
        };
        Send(mainPack);
    }

    private void StartReceive() {
        try
        {
            _mClientSocket.BeginReceive(_mMessage.Buffer, _mMessage.MessageLen
                , _mMessage.RemainSize, SocketFlags.None, ReceiveCallback, null);
        }
        catch (SocketException e)
        {
            CloseSocket();
            _mLogger.LogError("接收消息失败，远端关闭了连接..." + e.Message);
        }
    }

    private void ReceiveCallback(IAsyncResult iar)
    {
        try
        {
            if (_mClientSocket is null || !_mClientSocket.Connected)
            {
                return;
            }

            int len = _mClientSocket.EndReceive(iar);
            if (len == 0)
            {
                CloseSocket();
                _mLogger.LogError("连接已被对端关闭！");
                return;
            }

            _mMessage.ReadBuffer(len, HandleRequest);
            StartReceive();
        }
        catch (SocketException e)
        {
            if (e.ErrorCode == 10054)
            {
                // 连接被对端重置
                _mLogger.LogError("连接被对端重置！");
            }
            else
            {
                _mLogger.LogError($"Socket异常:{e.Message}");
            }

            CloseSocket();
        }
        catch (Exception e)
        {
            _mLogger.LogError("Exception:" + e.Message);
        }
    }

    private void HandleRequest(MainPack pack)
    {
        if (pack.RequestCode is RequestCode.HeartBeat && pack.Heartbeat is { Triggered: true, Type: "PING" })
        {
            _mTcpServer.UpdateHeartbeatTime(this);
            SendHeartbeat();
            return;
        }

        try
        {
            _mControllerManager.HandleRequest(pack, this);
        }
        catch (Exception e)
        {
            _mLogger.LogWarning(e.ToString());
        }
    }

    private void SendHeartbeat()
    {
        HeartbeatPack pack = new HeartbeatPack
        {
            Triggered = true,
            Type = CharsetUtil.DefaultToUTF8("PONG"),
            Timestamp = DateTime.Now.Ticks
        };
        MainPack mainPack = new MainPack
        {
            ResponseCode = ResponseCode.HeartBeatResponse,
            Heartbeat = pack
        };
        Send(mainPack);
    }

    public void Send(MainPack pack) {
        // if (_mClientSocket.Connected)
        {
            try
            {
                byte[] data = Message.GetPackData(pack);
                _mClientSocket.Send(data);
            }
            catch (SocketException e)
            {
                _mLogger.LogError("SocketError:" + e);
            }
            catch (Exception e)
            {
                _mLogger.LogError("Exception:" + e);
            }
        }
        // else
        // {
        //     _mLogger.LogError("连接已关闭，无法发送消息...");
        // }
    }
        
    public void CloseSocket()
    {
        _mClientSocket.Shutdown(SocketShutdown.Both);
        _mClientSocket.Close();
        OnTcpClientClosed?.Invoke(this);
    }

    public void Dispose()
    {
        _mClientSocket.Shutdown(SocketShutdown.Both);
        _mClientSocket.Close();
        _mClientSocket.Dispose();
    }
}