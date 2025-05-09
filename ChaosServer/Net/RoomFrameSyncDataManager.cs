using ChaosServer.Model;
using GameFrameSync;

namespace ChaosServer.Net;

public class RoomFrameSyncDataManager
{
    /// <summary>
    /// 房间码-RoomFrameSyncData 集合
    /// </summary>
    private readonly Dictionary<string, List<RoomFrameSyncData>> _mRoomFrameSyncDataDict = new();
    
    // private readonly ObjectPool<RoomFrameSyncData> _m

    public void AddRoomFrameSyncData(in string roomCode, in int index, in ResFrameSyncData resFrameSyncData
        , in List<string> clientIpList)
    {
        List<RoomFrameSyncData> addRoomFrameSyncDataList = [];
        foreach (var clientIp in clientIpList)
        {
            RoomFrameSyncData roomFrameSyncData = new RoomFrameSyncData
            {
                index = index,
                clientIp = clientIp,
                resFrameSyncData = resFrameSyncData,
                sendTime = DateTime.Now
            };
            addRoomFrameSyncDataList.Add(roomFrameSyncData);
        }
        if (_mRoomFrameSyncDataDict.TryGetValue(roomCode, out var roomFrameSyncDataList))
        {
            roomFrameSyncDataList.AddRange(addRoomFrameSyncDataList);
        }
        else
        { 
            roomFrameSyncDataList = new List<RoomFrameSyncData>();
            roomFrameSyncDataList.AddRange(addRoomFrameSyncDataList);
            _mRoomFrameSyncDataDict[roomCode] = roomFrameSyncDataList;
        }
    }

    public void RemoveRoomFrameSyncData(in string roomCode, int index)
    {
        if (_mRoomFrameSyncDataDict.TryGetValue(roomCode, out var roomFrameSyncDataList))
        {
            RoomFrameSyncData? roomFrameSyncData = roomFrameSyncDataList.Find(item => item.index == index);
            if (roomFrameSyncData != null) roomFrameSyncDataList.Remove(roomFrameSyncData);
        }
    }

    public void RemoveRoomFrameSyncData(in string roomCode, RoomFrameSyncData roomFrameSyncData)
    {
        if (_mRoomFrameSyncDataDict.TryGetValue(roomCode, out var roomFrameSyncDataList))
        {
            roomFrameSyncDataList.Remove(roomFrameSyncData);
        }
    }

    public List<RoomFrameSyncData> GetAckTimeoutPacket()
    {
        List<RoomFrameSyncData> result = new List<RoomFrameSyncData>();

        for (int i = 0; i < _mRoomFrameSyncDataDict.Count; i++)
        {
            KeyValuePair<string, List<RoomFrameSyncData>> kv = _mRoomFrameSyncDataDict.ElementAt(i);
            List<RoomFrameSyncData> roomFrameSyncDataList = kv.Value;

            for (int j = 0; j < roomFrameSyncDataList.Count; j++)
            {
                RoomFrameSyncData roomFrameSyncData = roomFrameSyncDataList[j];
                if ((DateTime.Now - roomFrameSyncData.sendTime).TotalMilliseconds > 1000)
                {
                    result.Add(roomFrameSyncData);
                }
            }
        }
        return result;
    }
}