using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using ChaosServer.Event;
using ChaosServer.Model;
using ChaosServer.Net;
using ChaosServer.Utility;
using GameFrameSync;

namespace ChaosServer.Game;

public class RoomFrameSyncManager : BaseManager
{
    private int _mFrameId = -1;
    private Dictionary<string, int> _mRoomCodeFrameId = new();
    private readonly int _mPerFrameTime;
    private Thread _mSyncThread;

    private readonly ConcurrentDictionary<string, ConcurrentBag<FrameInputData>> _mNextFrameInputData = new();

    // private readonly ManualResetEvent _mCollectFrameEvent = new(true);
    private readonly ConcurrentDictionary<string, ManualResetEvent> _mRoomCollectEvent = new();
    private readonly RoomManager _mRoomManager;
    // private readonly ConcurrentDictionary<string, List<IPEndPoint>> _mRoomClientIpEndPointDict;
    
    private readonly ObjectPool<ResFrameSyncData> _mResFrameSyncDataPool;
    private readonly ObjectPool<FrameInputData> _mFrameInputDataPool;

    public RoomFrameSyncManager(RoomManager roomManager, UdpListener udpListener)
    {
        _mRoomManager = roomManager;
        _mPerFrameTime = (int)(1f / ServerConstant.LOGIC_FRAME_RATE * 1000);
        _mLogger.LogInfo("GameSync PerFrameDeltaTime:" + _mPerFrameTime);
        DoUpdate();
        udpListener.OnFrameSyncDataReceived += ReceiveFrameSyncData;
        
        _mResFrameSyncDataPool = new ObjectPool<ResFrameSyncData>(() => new ResFrameSyncData());
        _mFrameInputDataPool = new ObjectPool<FrameInputData>(() => new FrameInputData());
        // _mRoomClientIpEndPointDict = new ConcurrentDictionary<string, List<IPEndPoint>>();
    }
    
    private void ReceiveFrameSyncData(ResFrameSyncData resFrameSyncData)
    {
        ReqFrameInputData reqFrameInputData = resFrameSyncData.ReqFrameInputData;

        FrameInputData frameInputData = _mFrameInputDataPool.Allocate();

        LoadFrameInputData(frameInputData, reqFrameInputData);

        string roomCode = resFrameSyncData.RoomCode;
        // _mLogger.LogInfo($"RoomCode:{roomCode}");
        if (_mNextFrameInputData.TryGetValue(roomCode, out var frameInputDataLit))
        {
            frameInputDataLit.Add(frameInputData);
        }
        else
        {
            frameInputDataLit = new ConcurrentBag<FrameInputData>();
            frameInputDataLit.Add(frameInputData);
            _mNextFrameInputData[roomCode] = frameInputDataLit;
            _mRoomCollectEvent[roomCode] = new ManualResetEvent(true);
        }

        // 安全访问字典
        if (_mRoomManager.RoomCodeClientDict.ContainsKey(roomCode) && _mNextFrameInputData.ContainsKey(roomCode))
        {
            // 获取这个房间的所有客户端数量
            int clientNum = _mRoomManager.RoomCodeClientDict[roomCode].Count;
            // 获取已经收集帧数据的数量
            int collectedNum = _mNextFrameInputData[roomCode].Count;
            // 收集完成
            // 发出信号，解除阻塞
            if(collectedNum >= clientNum)
                _mRoomCollectEvent[roomCode].Set();
        }
        
        // _mLogger.LogInfo($"frame:{frameInputData.FrameId}, playerId:{frameInputData.PlayerId} is input:{frameInputData.InputType}");
    }

    private void LoadFrameInputData(in FrameInputData frameInputData, in ReqFrameInputData reqFrameInputData)
    {
        // ChaosBall
        frameInputData.FrameId = reqFrameInputData.FrameId;
        frameInputData.InputType = reqFrameInputData.InputType;
        frameInputData.PlayerId = reqFrameInputData.PlayerId;
        frameInputData.Force = reqFrameInputData.Force;
        frameInputData.Position = reqFrameInputData.Position;
        frameInputData.ShootDirection = reqFrameInputData.ShootDirection;
        frameInputData.ArrowRotationZ = reqFrameInputData.ArrowRotationZ;
        frameInputData.Index = reqFrameInputData.Index;
        // KitchenChaos
        frameInputData.MoveVector = reqFrameInputData.MoveVector;
        frameInputData.InteractCounter = reqFrameInputData.InteractCounter;
    }

    private void UpdateGameFrameSync()
    {
        ReadOnlyCollection<RoomInfo> roomList = _mRoomManager.RoomList;
        roomList.AsParallel().ForAll(roomInfo =>
        {
            if (roomInfo.roomState is not RoomState.Started) return;

            ResFrameSyncData resFrameSyncData = _mResFrameSyncDataPool.Allocate();

            string roomCode = roomInfo.roomCode;
            if (_mRoomCodeFrameId.TryGetValue(roomCode, out var roomCodeFrameId))
            {
                resFrameSyncData.FrameId = roomCodeFrameId + 1;
                _mRoomCodeFrameId[roomCode] = resFrameSyncData.FrameId;
            }
            else
            {
                _mRoomCodeFrameId[roomCode] = -1;
            }

            // 等待所有客户端帧数据收集完成
            if (_mRoomCollectEvent.TryGetValue(roomCode, out var roomCollectEvent))
            {
                roomCollectEvent.WaitOne(TimeSpan.FromMilliseconds(_mPerFrameTime * 2));
            }
            // _mRoomCollectEvent[roomCode].WaitOne(TimeSpan.FromMilliseconds(_mPerFrameTime * 3));
            if (_mNextFrameInputData.TryGetValue(roomCode, out var frameInputDataList))
            {
                resFrameSyncData.PlayersFrameInputData.AddRange(frameInputDataList);
            }

            BroadcastRoomFrameSyncData(roomCode, resFrameSyncData);
            if (_mRoomCollectEvent.TryGetValue(roomCode, out var collectEvent))
            {
                collectEvent.Reset();
            }
        });
    }

    private void BroadcastRoomFrameSyncData(in string roomCode, in ResFrameSyncData resFrameSyncData)
    {
        if (_mRoomManager.RoomCodeToFrameSyncEpDict.TryGetValue(roomCode, out var clientEndPointList))
        {
            if (clientEndPointList.Count > 0)
            {
                ServerInterface.Instance.UdpListener.Send(clientEndPointList, resFrameSyncData);
                ResetResFrameSyncData(resFrameSyncData);
                _mResFrameSyncDataPool.Release(resFrameSyncData);
                ClearCachedFrameInputData(roomCode);
            }
        }
    }

    private static void ResetResFrameSyncData(in ResFrameSyncData resFrameSyncData)
    {
        resFrameSyncData.PlayersFrameInputData.Clear();
        resFrameSyncData.ReqFrameInputData = null;
        resFrameSyncData.RoomCode = string.Empty;
    }

    private void ClearCachedFrameInputData(in string roomCode)
    {
        if (_mNextFrameInputData.TryGetValue(roomCode, out var frameInputDataList))
        {
            foreach (var frameInputData in frameInputDataList)
            {
                _mFrameInputDataPool.Release(frameInputData);
            }
            frameInputDataList.Clear();
        }
    }
    
    private void DoUpdate()
    {
        _mSyncThread = new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(_mPerFrameTime);
                UpdateGameFrameSync();
            }
        });
        _mSyncThread.Start();
    }

    public override void OnInit()
    {
        EventSystem.Instance.Subscribe<DestroyRoomEvent>(OnDestroyRoom);
    }

    private void OnDestroyRoom(DestroyRoomEvent e)
    {
        string roomCode = e.roomCode;
        _mNextFrameInputData.TryRemove(roomCode, out _);
        _mRoomCollectEvent.TryRemove(roomCode, out _);
        // lock (this)
        // {
        //     _mRoomClientIpEndPointDict.TryRemove(roomCode, out _);
        // }
    }

    public override void OnUpdate()
    {
    }

    public override void OnDestroy()
    {
        EventSystem.Instance.Unsubscribe<DestroyRoomEvent>(OnDestroyRoom);
    }
}