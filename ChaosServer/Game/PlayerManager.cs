using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using ChaosServer.Event;
using ChaosServer.Model;
using Net_TcpClient = ChaosServer.Net.TcpClient;
using TcpClient = ChaosServer.Net.TcpClient;

namespace ChaosServer.Game;

public class PlayerManager : BaseManager
{
    // 在线玩家集合
    private List<PlayerInfo> _mOnlinePlayerList = new();

    /// <summary>
    /// 客户端-玩家信息字典
    /// </summary>
    public Dictionary<Net_TcpClient, PlayerInfo> ClientPlayerDict { get; } = new();

    public Dictionary<int, IPEndPoint> PlayerIdToFrameSyncEpDict { get; } = new();

    public void AddOnlinePlayer(Net_TcpClient tcpClient, PlayerInfo playerInfo, int udpPort)
    {
        _mLogger.LogInfo($"clientId: {tcpClient.ClientId}, Player {playerInfo.nickname} online...");
        _mOnlinePlayerList.Add(playerInfo);
        ClientPlayerDict[tcpClient] = playerInfo;

        string clientIp = tcpClient.ClientIp;

        PlayerIdToFrameSyncEpDict[playerInfo.id] = new IPEndPoint(IPAddress.Parse(clientIp), udpPort);
        
        tcpClient.OnTcpClientClosed += HandleTcpClientOffline;
    }

    private void HandleTcpClientOffline(Net_TcpClient client)
    {
        if (ClientPlayerDict.TryGetValue(client, out PlayerInfo? playerInfo))
        {
            _mLogger.LogInfo($"clientId: {client.ClientId}, Player {playerInfo.nickname} offline...");
            EventSystem.Instance.Publish(new PlayerQuitGameEvent
            {
                playerId = playerInfo.id,
                clientId = client.ClientId
            });
            ClientPlayerDict.Remove(client);
            PlayerIdToFrameSyncEpDict.Remove(playerInfo.id);
        }
    }

    public PlayerInfo GetOnlinePlayer(Net_TcpClient tcpClient)
    {
        if (ClientPlayerDict.TryGetValue(tcpClient, out var playerInfo))
        {
            return playerInfo;
        }
        else
        {
            _mLogger.LogWarning($"不存在该Client {tcpClient.ClientId} 对应的Player");
            return null;
        }
    }

    public override void OnInit()
    {
        
    }

    public override void OnUpdate()
    {
        
    }

    public override void OnDestroy()
    {
        
    }
}