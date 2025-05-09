using GameFrameSync;

namespace ChaosServer.Model;

public class RoomFrameSyncData
{
    /// <summary>
    /// 消息序列
    /// </summary>
    public int index;
    /// <summary>
    /// 消息数据
    /// </summary>
    public ResFrameSyncData resFrameSyncData;
    /// <summary>
    /// 发送的客户端ip
    /// </summary>
    public string clientIp;
    /// <summary>
    /// 发送时间
    /// </summary>
    public DateTime sendTime;
    /// <summary>
    /// 超时时间
    /// </summary>
    public int timeoutCount;
}