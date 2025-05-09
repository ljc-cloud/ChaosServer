using System.Net;
using System.Net.Sockets;
using System.Timers;
using ChaosServer.Model;
using ChaosServer.Utility;
using GameFrameSync;
using Google.Protobuf;
using Timer = System.Timers.Timer;

namespace ChaosServer.Net;

public class UdpListener : IDisposable
{
    private readonly Logger _mLogger;
    /// <summary>
    /// UDP监听
    /// </summary>
    private readonly UdpClient _mUdpClient;
    
    private readonly RoomFrameSyncDataManager _mRoomFrameSyncDataManager = new();

    private readonly ObjectPool<ResFrameSyncData> _mResFrameSyncDataPool;
    private readonly ObjectPool<MessageHead> _mMessageHeadPool;
    
    private readonly Timer _mTimeoutTimer;
    private int _mCurrentDataSequence = 0;
    
    public event Action<ResFrameSyncData> OnFrameSyncDataReceived;

    public UdpListener(int port)
    {
        _mLogger = new Logger(GetType());
        
        uint IOC_IN = 0x80000000;
        uint IOC_VENDOR = 0x18000000;
        uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
        
        _mUdpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
        _mUdpClient.Client.IOControl((int)SIO_UDP_CONNRESET, [Convert.ToByte(false)], null);
        _mTimeoutTimer = new Timer
        {
            AutoReset = true,
            Interval = 1000,
            Enabled = true,
        };
        _mTimeoutTimer.Elapsed += DataPacketAckTimeoutCheck;
        
        _mResFrameSyncDataPool = new ObjectPool<ResFrameSyncData>(() => new ResFrameSyncData());
        _mMessageHeadPool = new ObjectPool<MessageHead>(() => new MessageHead());
        StartReceive();
    }

    private void DataPacketAckTimeoutCheck(object? sender, ElapsedEventArgs e)
    {
        #region Obsolete

        // lock (this)
        // {
        //     for (int i = 0; i < _mFrameSyncDataToSendTimeDict.Count; i++)
        //     {
        //         KeyValuePair<int,Dictionary<string, DateTime>> keyValuePair2 = _mFrameSyncDataToSendTimeDict.ElementAt(i);
        //         for (int j = 0; j < keyValuePair2.Value.Count; j++)
        //         {
        //             bool isTimeout = (DateTime.Now - keyValuePair2.Value.ElementAt(j).Value).TotalMilliseconds > 1000;
        //             if (isTimeout)
        //             {
        //                 int index = keyValuePair2.Key;
        //                 string clientIp = keyValuePair2.Value.ElementAt(j).Key;
        //                 if (!_mClientIpToTimeoutPacketCountDict.TryGetValue(clientIp, out var timeoutPacketCountDict))
        //                 {
        //                     continue;
        //                 }
        //
        //                 if (timeoutPacketCountDict.TryGetValue(index, out var timeoutCount))
        //                 {
        //                     if (timeoutCount > ServerConstant.MAX_TIMEOUT_COUNT)
        //                     {
        //                         return;
        //                     }
        //                     _mLogger.LogInfo($"clientIp: {clientIp}, index: {index}, timeoutCount: {timeoutCount}");
        //                     timeoutPacketCountDict[index]++;
        //                     if (_mCachedFrameSyncDataDict.TryGetValue(index, out var timeoutFrameSyncData))
        //                     {
        //                         SendTimeout(index, new IPEndPoint(IPAddress.Parse(clientIp), ServerConstant.CLIENT_UDP_LISTEN_PORT)
        //                             , timeoutFrameSyncData);
        //                     }
        //                 }
        //                
        //             }
        //         }
        //     }
        // }

        #endregion

        List<RoomFrameSyncData> roomFrameSyncDataList = _mRoomFrameSyncDataManager.GetAckTimeoutPacket();
        for (int i = 0; i < roomFrameSyncDataList.Count; i++)
        {
            if (roomFrameSyncDataList[i].timeoutCount > ServerConstant.MAX_TIMEOUT_COUNT)
            {
                continue;
            }
            int index = roomFrameSyncDataList[i].index;
            string clientIp = roomFrameSyncDataList[i].clientIp;
            ResFrameSyncData resFrameSyncData = roomFrameSyncDataList[i].resFrameSyncData;
            IPEndPoint clientIpEp = new IPEndPoint(IPAddress.Parse(clientIp), ServerConstant.CLIENT_UDP_LISTEN_PORT);
            SendTimeout(index, clientIpEp, resFrameSyncData);
            roomFrameSyncDataList[i].sendTime = DateTime.Now;
            roomFrameSyncDataList[i].timeoutCount++;
        }
        // 删除所有超时次数超过最大值的 房间同步数据
        roomFrameSyncDataList.RemoveAll(item => item.timeoutCount > ServerConstant.MAX_TIMEOUT_COUNT);
    }

    private void StartReceive()
    {
        try
        {
            _mUdpClient.BeginReceive(ReceiveCallback, null);
        }
        catch (SocketException e)
        {
            _mLogger.LogError("UDP SocketError:" + e);
        }
    }

    private void ReceiveCallback(IAsyncResult iar)
    {
        try
        {
            IPEndPoint? clientIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = _mUdpClient.EndReceive(iar, ref clientIpEndPoint);
            ResFrameSyncData resFrameSyncData = Deserialize(data);

            int messageHeadIndex = resFrameSyncData.MessageHead.Index;
            if (resFrameSyncData.MessageType is MessageType.Ack)
            {
                lock (this)
                {
                    #region Obsolete

                    // if (_mFrameSyncDataToSendTimeDict.TryGetValue(messageHeadIndex
                    //         , out var clientIpDateTimeDict))
                    // {
                    //     clientIpDateTimeDict.Remove(clientIpEndPoint.Address.ToString());
                    // }
                    //
                    // if (_mClientIpToTimeoutPacketCountDict.TryGetValue(clientIpEndPoint.Address.ToString()
                    //         , out var timeoutPacketCountDict))
                    // {
                    //     timeoutPacketCountDict.Remove(messageHeadIndex);
                    // }
                    // if (_mFrameSyncDataToTargetIpDict.TryGetValue(messageHeadIndex, out var clientIpList))
                    // {
                    //     clientIpList.Remove(resFrameSyncData.MessageHead.ClientIp);
                    // }
                    // _mCachedFrameSyncDataDict.TryRemove(messageHeadIndex, out _);
                    //
                    // if (_mCachedRoomFrameSyncData.TryGetValue(resFrameSyncData.RoomCode, out var roomFrameSyncDataList))
                    // {
                    //     // roomFrameSyncDataList.Remove()
                    // }

                    #endregion

                    _mRoomFrameSyncDataManager.RemoveRoomFrameSyncData(resFrameSyncData.RoomCode
                        , resFrameSyncData.MessageHead.Index);
                }
            }
            else
            {
                OnFrameSyncDataReceived?.Invoke(resFrameSyncData);
                SendAck(messageHeadIndex, clientIpEndPoint);
            }

            StartReceive();
        }
        catch (SocketException e)
        {
            _mLogger.LogError("UDP SocketError:" + e);
        }
        catch (Exception e)
        {
            _mLogger.LogError("Exception:" + e);
        }
    }
    public void Send(in List<IPEndPoint> clientIpEndPointList, in ResFrameSyncData resFrameSyncData)
    {
        MessageHead messageHead = _mMessageHeadPool.Allocate();
        messageHead.Index = _mCurrentDataSequence;

        resFrameSyncData.MessageType = MessageType.FrameSync;
        resFrameSyncData.MessageHead = messageHead;

        try
        {
            byte[] data = Serialize(resFrameSyncData);

            List<string> clientIpList = clientIpEndPointList.Select(item => item.Address.ToString()).ToList();

            lock (this)
            {
                #region Obsolete
                
                // _mCachedFrameSyncDataDict.TryAdd(_mCurrentDataSequence, resFrameSyncData);
                //
                // Dictionary<string,DateTime> ipDateTime = clientIpList.Select(item
                //     => new KeyValuePair<string, DateTime>(item, DateTime.Now)).ToDictionary();
                // _mFrameSyncDataToSendTimeDict.TryAdd(resFrameSyncData.MessageHead.Index, ipDateTime);
                // _mFrameSyncDataToTargetIpDict[resFrameSyncData.MessageHead.Index] = clientIpList;

                #endregion
                
                _mRoomFrameSyncDataManager.AddRoomFrameSyncData(resFrameSyncData.RoomCode, resFrameSyncData.MessageHead.Index,
                    resFrameSyncData, clientIpList);
            }

            _mCurrentDataSequence++;

            foreach (var clientIpEndPoint in clientIpEndPointList)
            {
                _mUdpClient.Send(data, data.Length, clientIpEndPoint);
            }
        }
        catch (SocketException e)
        {
            _mLogger.LogError("UDP SocketError:" + e);
        }
        catch (Exception e)
        {
            _mLogger.LogError("Exception:" + e);
        }
        finally
        {
            _mMessageHeadPool.Release(messageHead);
        }
    }

    private void SendAck(in int index, in IPEndPoint clientIpEndPoint)
    {
        MessageHead messageHead = _mMessageHeadPool.Allocate();
        messageHead.Index = index;

        ResFrameSyncData resFrameSyncData = _mResFrameSyncDataPool.Allocate();
        resFrameSyncData.MessageHead = messageHead;
        resFrameSyncData.MessageType = MessageType.Ack;

        try
        {
            byte[] data = Serialize(resFrameSyncData);

            _mUdpClient.Send(data, data.Length, clientIpEndPoint);
        }
        catch (SocketException e)
        {
            _mLogger.LogError("UDP SocketError:" + e);
        }
        catch (Exception e)
        {
            _mLogger.LogError("Exception:" + e);
        }
        finally
        {
            _mResFrameSyncDataPool.Release(resFrameSyncData);
            _mMessageHeadPool.Release(messageHead);
        }
    }

    private void SendTimeout(in int index, in IPEndPoint clientIpEndPoint, in ResFrameSyncData resFrameSyncData)
    {
        // _mLogger.LogError($"pack {index} has timeout ");
        MessageHead messageHead = _mMessageHeadPool.Allocate();
        messageHead.Index = index;
        resFrameSyncData.MessageHead = messageHead;
        
        try
        {
            byte[] data = Serialize(resFrameSyncData);

            _mUdpClient.Send(data, data.Length, clientIpEndPoint);
        }
        catch (SocketException e)
        {
            _mLogger.LogError("UDP SocketError:" + e);
        }
        catch (Exception e)
        {
            _mLogger.LogError("Exception:" + e);
        }
        finally
        {
            _mMessageHeadPool.Release(messageHead);
        }
    }
    
    public ResFrameSyncData Deserialize(in byte[] data)
    {
        return ResFrameSyncData.Parser.ParseFrom(data, 0, data.Length);
    }

    public byte[] Serialize(in ResFrameSyncData pack)
    {
        byte[] data = pack.ToByteArray();
        return data;
    }

    public void Dispose()
    {
        _mUdpClient.Dispose();
        _mTimeoutTimer.Dispose();
    }
}