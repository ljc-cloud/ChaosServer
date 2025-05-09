using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net;
using ChaosBall.Utility;
using ChaosServer.Event;
using ChaosServer.Model;
using ChaosServer.Net;
using SocketProtocol;
using RoomVisibility = ChaosServer.Model.RoomVisibility;

namespace ChaosServer.Game;

public class RoomManager : BaseManager
{
    /// <summary>
    /// 所有的房间信息 
    /// </summary>
    // private readonly List<RoomInfo> _mRoomList = new();
    private readonly ConcurrentDictionary<string, RoomInfo> _mRoomDict = new();
    /// <summary>
    /// 房间码-该房间内所有的玩家
    /// </summary>
    private readonly ConcurrentDictionary<string, List<RoomPlayerInfo>> _mRoomCodePlayerDict = new();
    /// <summary>
    /// 房间码-该房间内所有的TCP客户端
    /// </summary>
    private readonly ConcurrentDictionary<string, List<TcpClient>> _mRoomCodeTcpClientDict = new();
    /// <summary>
    /// 房间码-UDP监听IP
    /// </summary>
    private readonly ConcurrentDictionary<string, List<string>> _mRoomCodeIpDict = new();
    
    private readonly ConcurrentDictionary<string, List<IPEndPoint>> _mRoomCodeToFrameSyncEpDict = new();
    
    /// <summary>
    /// 房间码生成种子
    /// </summary>
    private int _mRandomSeed = 0;
    
    /// <summary>
    /// 所有的房间信息
    /// </summary>
    public ReadOnlyCollection<RoomInfo> RoomList => _mRoomDict.Values.ToList().AsReadOnly();
    /// <summary>
    /// 房间码-该房间内所有的玩家
    /// </summary>
    public ConcurrentDictionary<string, List<RoomPlayerInfo>> RoomCodeToPlayerDict => _mRoomCodePlayerDict;
    /// <summary>
    /// 房间码-该房间内所有的TCP客户端
    /// </summary>
    public ConcurrentDictionary<string, List<TcpClient>> RoomCodeClientDict => _mRoomCodeTcpClientDict;
    /// <summary>
    /// 房间码-UDP监听IP
    /// </summary>
    public ConcurrentDictionary<string, List<string>> RoomCodeIpDict => _mRoomCodeIpDict;
    public ConcurrentDictionary<string, List<IPEndPoint>> RoomCodeToFrameSyncEpDict => _mRoomCodeToFrameSyncEpDict;

    public RoomManager()
    {
        // _mRoomDict["123456"] = new RoomInfo
        // {
        //     currentPlayers = 1,
        //     maxPlayer = 2,
        //     roomName = "Room1",
        //     roomState = RoomState.WaitReady,
        //     roomVisibility = RoomVisibility.Public
        // };
        //
        // _mRoomDict["654321"] = new RoomInfo
        // {
        //     currentPlayers = 1,
        //     maxPlayer = 2,
        //     roomName = "Room2",
        //     roomState = RoomState.WaitReady,
        //     roomVisibility = RoomVisibility.Private
        // };
    }
    
    #region RoomManage

    /// <summary>
    /// 创建房间
    /// </summary>
    /// <param name="roomInfo">创建房间的信息</param>
    /// <param name="creator">创建的玩家</param>
    /// <param name="tcpClient">创建的客户端</param>
    /// <returns></returns>
    public ReturnData<string> CreateRoom(RoomInfo roomInfo, PlayerInfo creator, TcpClient tcpClient)
    {
        int roomCode = 12345;
        // roomInfo.roomCode = roomCode.ToString();
        lock (this)
        {
            Random rnd = new(_mRandomSeed++);
            roomCode = rnd.Next(10000, 99999);
            roomInfo.roomCode = CharsetUtil.DefaultToUTF8(roomCode.ToString());
        }
        
        roomInfo.currentPlayers = 1;
        
        ReturnData<string> returnData;
        lock (this)
        {
            if (_mRoomDict.ContainsKey(roomInfo.roomCode))
            {
                _mLogger.LogWarning($"已存在该房间码 {roomCode}");
                returnData = new ReturnData<string>
                {
                    success = false,
                    errorMessage = CharsetUtil.DefaultToUTF8("已存在该房间码")
                };
                return returnData;
            }
            
            bool containRoomName = false;
            foreach (var (_, existRoomInfo) in _mRoomDict)
            {
                if (roomInfo.roomName == existRoomInfo.roomName)
                {
                    containRoomName = true;
                    break;
                }
            }
            // containRoomName = _mRoomList.Find(item => item.roomName == roomInfo.roomName) != null;
            if (containRoomName)
            {
                _mLogger.LogInfo($"已存在该房间名 {roomInfo.roomName}");
                returnData = new ReturnData<string>
                {
                    success = false,
                    errorMessage = CharsetUtil.DefaultToUTF8("已存在该房间名")
                };
                return returnData;
            }
        }
        _mRoomDict[roomInfo.roomCode] = roomInfo;
        // _mRoomList.Add(roomInfo);
        RoomPlayerInfo roomPlayerInfo = new RoomPlayerInfo
        {
            id = creator.id,
            username = creator.username,
            nickname = creator.nickname,
            ready = false
        };
        bool addPlayer = _mRoomCodePlayerDict.TryAdd(roomInfo.roomCode, [roomPlayerInfo]);
        if (!addPlayer)
        {
            returnData = new ReturnData<string>
            {
                success = false,
                errorMessage = CharsetUtil.DefaultToUTF8("创建房间失败，addPlayer"),
            };
            return returnData;
        }
        
        bool addClient = _mRoomCodeTcpClientDict.TryAdd(roomInfo.roomCode, [tcpClient]);
        if (!addClient)
        {
            returnData = new ReturnData<string>
            {
                success = false,
                errorMessage = CharsetUtil.DefaultToUTF8("创建房间失败，addClient"),
            };
            return returnData;
        }

        _mRoomCodeIpDict.TryAdd(roomInfo.roomCode, []);

        _mRoomCodeIpDict[roomInfo.roomCode].Add(tcpClient.ClientIp);
        
        if (ServerInterface.Instance.PlayerManager.PlayerIdToFrameSyncEpDict.TryGetValue(creator.id, out var ep))
        {
            List<IPEndPoint> ipEndPointList = [ep];
            _mRoomCodeToFrameSyncEpDict[roomInfo.roomCode] = ipEndPointList;
            _mLogger.LogInfo($"Player {creator.nickname} create room, port: {ep.Port}");
        }
        
        returnData = new ReturnData<string>
        {
            success = true,
            data = roomInfo.roomCode,
            successMessage = CharsetUtil.DefaultToUTF8("创建房间成功")
        };

        roomInfo.roomState = RoomState.WaitReady;
        
        EventSystem.Instance.Publish(new CreateRoomEvent
        {
            roomInfo = roomInfo,
            creator = creator,
        });
        
        return returnData;
    }

    /// <summary>
    /// 加入房间
    /// </summary>
    /// <param name="roomCode">房间码</param>
    /// <param name="playerInfo">需要加入房间的玩家</param>
    /// <param name="tcpClient">需要加入房间的客户端</param>
    /// <returns></returns>
    public ReturnData<List<RoomPlayerInfo>> JoinRoom(string roomCode, PlayerInfo playerInfo, TcpClient tcpClient)
    {
        ReturnData<List<RoomPlayerInfo>> returnData;
        if (_mRoomDict.TryGetValue(roomCode, out var roomInfo))
        {
            _mLogger.LogInfo($"Player {playerInfo.nickname} request join room {roomCode}:{roomInfo?.roomName}");
            if (roomInfo.roomVisibility is SocketProtocol.RoomVisibility.Private)
            {
                returnData = new ReturnData<List<RoomPlayerInfo>>
                {
                    success = false,
                    errorMessage = CharsetUtil.DefaultToUTF8("不能进入该房间(Private)")
                };
                _mLogger.LogWarning(
                    $"Player {playerInfo.nickname} cannot join room {roomCode}:{roomInfo.roomName}(Private)");
                return returnData;
            }

            lock (this)
            {
                if (roomInfo.currentPlayers >= roomInfo.maxPlayer)
                {
                    returnData = new ReturnData<List<RoomPlayerInfo>>
                    {
                        success = false,
                        errorMessage = CharsetUtil.DefaultToUTF8("房间玩家已满")
                    };
                    _mLogger.LogWarning(
                        $"Room {roomCode}:{roomInfo.roomName} has filled {roomInfo.currentPlayers}:{roomInfo.maxPlayer}");
                    return returnData;
                }
            }

            RoomPlayerInfo roomPlayerInfo = new RoomPlayerInfo
            {
                id = playerInfo.id,
                username = playerInfo.username,
                nickname = playerInfo.nickname,
                ready = false
            };

            if (_mRoomCodePlayerDict.TryGetValue(roomCode, out var roomPlayerInfoList))
            {
                roomPlayerInfoList.Add(roomPlayerInfo);
                if (_mRoomCodeTcpClientDict.TryGetValue(roomCode, out var tcpClientList))
                {
                    tcpClientList.Add(tcpClient);
                }

                // List<RoomPlayerInfo> roomPlayerInfoList = _mRoomCodePlayerDict[roomCode];
                if (_mRoomCodeIpDict.TryGetValue(roomCode, out var clientIpList))
                {
                    clientIpList.Add(tcpClient.ClientIp);
                }

                if (ServerInterface.Instance.PlayerManager.PlayerIdToFrameSyncEpDict.TryGetValue(playerInfo.id,
                        out var ep))
                {
                    _mLogger.LogInfo($"Player {playerInfo.nickname} join room, port: {ep.Port}");
                    _mRoomCodeToFrameSyncEpDict[roomInfo.roomCode].Add(ep);
                }

                returnData = new ReturnData<List<RoomPlayerInfo>>
                {
                    success = true,
                    data = roomPlayerInfoList,
                    successMessage = CharsetUtil.DefaultToUTF8("加入房间成功")
                };
                EventSystem.Instance.Publish(new JoinRoomEvent
                {
                    roomCode = roomCode,
                    playerInfo = roomPlayerInfo
                });

                _mLogger.LogInfo($"Player {playerInfo.nickname} join room {roomCode}:{roomInfo.roomName} success");
                roomInfo.currentPlayers++;
            }
            else
            {
                returnData = new ReturnData<List<RoomPlayerInfo>>
                {
                    success = false,
                    errorMessage = CharsetUtil.DefaultToUTF8("没有对应的房间码为:" + roomCode)
                };
            }
        }
        else
        {
            returnData = new ReturnData<List<RoomPlayerInfo>>
            {
                success = false,
                errorMessage = CharsetUtil.DefaultToUTF8($"不存在房间:{roomCode}")
            };
        }
        return returnData;
    }

    /// <summary>
    /// 获取所有的Public房间
    /// </summary>
    /// <returns></returns>
    public List<RoomInfo> GetPublicRoomList()
    {
        List<RoomInfo> publicRoomList = _mRoomDict.Where(item 
            => item.Value.roomVisibility is SocketProtocol.RoomVisibility.Public).Select(item => item.Value).ToList();
        // List<RoomInfo> publicRoomList = _mRoomList.Where(item => item.roomVisibility == RoomVisibility.Public).ToList();
        return publicRoomList;
    }

    /// <summary>
    /// 按照给定的房间信息搜索房间
    /// </summary>
    /// <param name="condition">房间信息</param>
    /// <returns></returns>
    public List<RoomInfo> SearchRoom(RoomInfo condition)
    {
        // _mLogger.LogInfo($"搜索房间 {condition}");
        IEnumerable<RoomInfo> roomInfoEnumerable = _mRoomDict.Select(item => item.Value);
        // IEnumerable<RoomInfo> roomInfoEnumerable = _mRoomList;
        if (!string.IsNullOrWhiteSpace(condition.roomName))
        {
            _mLogger.LogWarning("房间名为空...");
            roomInfoEnumerable = _mRoomDict.Where(item => item.Value.roomName.Contains(condition.roomName))
                .Select(item => item.Value);
            // roomInfoEnumerable = _mRoomList.Where(item => item.roomName.Contains(roomInfo.roomName));
        }

        if (!string.IsNullOrEmpty(condition.roomCode))
        {
            roomInfoEnumerable = _mRoomDict.Where(item => item.Value.roomCode == condition.roomCode)
                .Select(item => item.Value);
            // roomInfoEnumerable = _mRoomList.Where(item => item.roomCode == condition.roomCode);
        }

        if (condition.roomVisibility != SocketProtocol.RoomVisibility.None)
        {
            roomInfoEnumerable = roomInfoEnumerable.Where(item => item.roomVisibility == condition.roomVisibility);
        }
        
        // _mLogger.LogInfo("搜索结果：" + roomInfoEnumerable.Count());
        
        return roomInfoEnumerable.ToList();
    }

    /// <summary>
    /// 设置该玩家准备状态
    /// </summary>
    /// <param name="roomCode">房间码</param>
    /// <param name="playerId">玩家id</param>
    /// <param name="ready">准备状态</param>
    public ReturnData<bool> SetRoomPlayerReady(string roomCode, int playerId, bool ready)
    {
        ReturnData<bool> returnData;
        // List<RoomPlayerInfo> roomPlayerInfos = _mRoomCodePlayerDict[roomCode];
        List<RoomPlayerInfo> roomPlayerInfoList = _mRoomCodePlayerDict[roomCode];;
        RoomPlayerInfo? roomPlayerInfo = roomPlayerInfoList.Find(item => item.id == playerId);
        if (roomPlayerInfo != null)
        {
            returnData = new ReturnData<bool>
            {
                success = true,
                successMessage = CharsetUtil.DefaultToUTF8("")
            };
            roomPlayerInfo.ready = ready;
        }
        else
        {
            returnData = new ReturnData<bool>
            {
                success = false,
                errorMessage = CharsetUtil.DefaultToUTF8("准备失败")
            };
            _mLogger.LogWarning($"该房间{roomCode}不存在玩家id：" + playerId);
        }
        
        return returnData;
    }
    
    /// <summary>
    /// 玩家退出房间
    /// </summary>
    /// <param name="roomCode">房间码</param>
    /// <param name="clientId">客户端id</param>
    /// <param name="roomPlayerId">玩家id</param>
    /// <returns></returns>
    public bool RoomPlayerQuitRoom(string roomCode, int clientId, int roomPlayerId)
    {
        if (_mRoomCodeTcpClientDict.TryGetValue(roomCode, out var roomClientList))
        {
            TcpClient? client = roomClientList.Find(item => item.ClientId == clientId);
            if (client != null)
            {
                _mRoomCodeIpDict[roomCode].Remove(client.ClientIp);
                roomClientList.Remove(client);
            }
        }
        else
        {
            _mLogger.LogWarning($"不存在房间{roomCode}");
            return false;
        }

        if (_mRoomCodeToFrameSyncEpDict.TryGetValue(roomCode, out var clientEpList))
        {
            if (ServerInterface.Instance.PlayerManager.PlayerIdToFrameSyncEpDict.TryGetValue(roomPlayerId, out var clientEp))
            {
                _mLogger.LogInfo($"Player {roomPlayerId} quit room, port: {clientEp.Port}");
                clientEpList.Remove(clientEp);
            }
        }

        if (_mRoomCodePlayerDict.TryGetValue(roomCode, out var roomPlayerInfoList))
        {
            int index = roomPlayerInfoList.FindIndex(item => item.id == roomPlayerId);
            if (index != -1)
            {
                roomPlayerInfoList.RemoveAt(index);
            }
        }
        else
        {
            _mLogger.LogWarning($"不存在房间{roomCode}");
            return false;
        }

        if (_mRoomDict.TryGetValue(roomCode, out var roomInfo))
        {
            roomInfo.currentPlayers--;
            EventSystem.Instance.Publish(new QuitRoomEvent
            {
                playerId = roomPlayerId,
                roomCode = roomCode,
            });

            if (roomInfo.currentPlayers == 0)
            {
                _mRoomDict.Remove(roomCode, out _);
                _mRoomCodeTcpClientDict.TryRemove(roomCode, out _);
                _mRoomCodePlayerDict.TryRemove(roomCode, out _);
                _mRoomCodeToFrameSyncEpDict.TryRemove(roomCode, out _);
                _mRoomCodeIpDict.TryRemove(roomCode, out _);
                EventSystem.Instance.Publish(new DestroyRoomEvent { roomCode = roomCode });
            }

            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取房间内所有的客户端
    /// </summary>
    /// <param name="roomCode">房间码</param>
    /// <returns></returns>
    public List<TcpClient> GetRoomClientList(string roomCode)
    {
        if (_mRoomCodeTcpClientDict.TryGetValue(roomCode, out var clientList))
        {
            return clientList;
        }
        else
        {
            _mLogger.LogWarning($"没有找到{roomCode}房间码房间");
            return null;
        }
    }

    /// <summary>
    /// 给房间里所有的客户端广播消息
    /// </summary>
    /// <param name="roomCode">房间码</param>
    /// <param name="pack">消息</param>
    public void BroadcastRoomClient(string roomCode, MainPack pack)
    {
        List<TcpClient> roomClientList = GetRoomClientList(roomCode);
        foreach (TcpClient tcpClient in roomClientList)
        {
            tcpClient.Send(pack);
        }
        // GetRoomClientList(roomCode).ForEach(eachClient => eachClient.Send(pack));
        // List<TcpClient> roomClientList = GetRoomClientList(roomCode);
        // if (roomClientList != null)
        // {
        //     roomClientList.ForEach(eachClient =>
        //     {
        //         eachClient.Send(pack);
        //     });
        // }
    }

    /// <summary>
    /// 给房间的其他客户端广播消息
    /// </summary>
    /// <param name="roomCode">房间码</param>
    /// <param name="selfTcpClient">当前client</param>
    /// <param name="pack">消息</param>
    public void BroadcastOtherRoomClient(string roomCode, TcpClient selfTcpClient, MainPack pack)
    {
        List<TcpClient> roomClientList = GetRoomClientList(roomCode);
        if (roomClientList != null)
        {
            roomClientList.ForEach(eachClient =>
            {
                if (eachClient != selfTcpClient)
                {
                    eachClient.Send(pack);
                }
            });
        }
    }

    public void SetRoomPlayerLoadComplete(string roomCode, int playerId)
    {
        List<RoomPlayerInfo> roomPlayerInfoList = _mRoomCodePlayerDict[roomCode];
        // if (roomPlayerInfoList == null) return;
        RoomPlayerInfo? roomPlayerInfo = roomPlayerInfoList.Find(item => item.id == playerId);
        if (roomPlayerInfo != null)
        {
            _mLogger.LogInfo($"Room:{roomCode}, player:{playerId} load complete");
            roomPlayerInfo.loadComplete = true;
        }
    }

    #endregion

    public RoomInfo GetRoomInfoFromCode(string roomCode)
    {
        // return _mRoomDict.GetValueOrDefault(roomCode);
        return _mRoomDict[roomCode];
        // return _mRoomList.Find(item => item.roomCode == roomCode);
    }
    
    public override void OnInit()
    {
        EventSystem.Instance.Subscribe<RoomGameStateChangeEvent>(OnRoomGameStateChanged);
        EventSystem.Instance.Subscribe<ChangeRoomRoundEvent>(OnRoomChangeRound);
        EventSystem.Instance.Subscribe<ChangeRoomOperationEvent>(OnChangeRoomOperation);
        EventSystem.Instance.Subscribe<PlayerQuitGameEvent>(OnPlayerQuitGame);
        EventSystem.Instance.Subscribe<GameOverEvent>(OnGameOver);
    }

    private void OnGameOver(GameOverEvent e)
    {
        Dictionary<int,int[]> playerScoreBoardDict = e.playerScoreArrayDict;

        GameOverPack gameOverPack = new GameOverPack();
        
        foreach (var (playerId, scoreArray) in playerScoreBoardDict)
        {
            PlayerScorePack playerScorePack = new PlayerScorePack
            {
                PlayerId = playerId,
                ScoreArray = { scoreArray }
            };
            gameOverPack.PlayerScorePack.Add(playerScorePack);
        }
        
        MainPack mainPack = new MainPack
        {
            RequestCode = RequestCode.Game,
            ActionCode = ActionCode.SetGameOver,
            ReturnCode = ReturnCode.Success,
            GameOverPack = gameOverPack,
        };
        BroadcastRoomClient(e.roomCode, mainPack);
    }

    private void OnPlayerQuitGame(PlayerQuitGameEvent e)
    {
        int quitPlayerId = e.playerId;
        string quitRoomCode = string.Empty;
        foreach (var (roomCode, roomPlayerInfoList) in _mRoomCodePlayerDict)
        {
            bool roomExist = roomPlayerInfoList.Exists(item => item.id == quitPlayerId);
            if (!roomExist) continue;
            quitRoomCode = roomCode;
        }
        RoomPlayerQuitRoom(quitRoomCode, e.clientId, quitPlayerId);
    }

    private void OnChangeRoomOperation(ChangeRoomOperationEvent e)
    {
        ChangeOperationPack changeOperationPack = new ChangeOperationPack
        {
            CurrentOperationPlayerId = e.currentOperationPlayerId,
            OperationLeft = e.operationLeft
        };
        MainPack mainPack = new MainPack
        {
            RequestCode = RequestCode.Game,
            ActionCode = ActionCode.ChangeOperation,
            ReturnCode = ReturnCode.Success,
            ChangeOperationPack = changeOperationPack
        };
        BroadcastRoomClient(e.roomCode, mainPack);
    }

    private void OnRoomChangeRound(ChangeRoomRoundEvent e)
    {
        RoundPack roundPack = new RoundPack
        {
            CurrentRound = e.currentRound,
            OperationPlayerId = e.operationPlayerId,
            Message = e.message
        };
        MainPack mainPack = new MainPack
        {
            RequestCode = RequestCode.Game,
            ActionCode = ActionCode.ChangeRound,
            ReturnCode = ReturnCode.Success,
            RoundPack = roundPack
        };
        BroadcastRoomClient(e.roomCode, mainPack);
    }

    private void OnRoomGameStateChanged(RoomGameStateChangeEvent e)
    {
        MainPack mainPack = new MainPack
        {
            RequestCode = RequestCode.Game,
            ActionCode = ActionCode.ChangeGameState,
            ReturnCode = ReturnCode.Success,
            CurrentGameState = Enum.Parse<SocketProtocol.GameState>(e.gameState.ToString())
        };
        _mLogger.LogInfo($"房间{e.roomCode} 更改游戏状态：{mainPack.CurrentGameState}");
        BroadcastRoomClient(e.roomCode, mainPack);
    }

    public override void OnUpdate()
    {
        // if (true) return;
        CheckRoomAllReady();
        CheckRoomAllLoadComplete();
    }

    private void CheckRoomAllLoadComplete()
    {
        foreach (var (_, roomInfo) in _mRoomDict)
        {
            if (roomInfo.roomState != RoomState.WaitingStart) continue;
            string roomCode = roomInfo.roomCode;
            List<RoomPlayerInfo> roomPlayerInfoList = _mRoomCodePlayerDict[roomCode];
            bool allLoadComplete = roomPlayerInfoList.All(item => item.loadComplete);
            // _mLogger.LogInfo($"All load complete: {allLoadComplete}");
            if (allLoadComplete)
            {
                // roomInfo.roomState = RoomState.Started;
                try
                {
                    Random rdm = new Random();
                    int next = rdm.Next(0, roomPlayerInfoList.Count);
                    _mLogger.LogInfo($"roomPlayerInfoList Length: {roomPlayerInfoList.Count}, next: {next}");
                    int firstPlayerId = roomPlayerInfoList[next].id;
                    StartGameResultPack startGameResultPack = new StartGameResultPack { FirstPlayerId = firstPlayerId };
                    MainPack mainPack = new MainPack
                    {
                        RequestCode = RequestCode.Game,
                        ActionCode = ActionCode.LoadGameSceneComplete,
                        ReturnCode = ReturnCode.Success, 
                        StartGameResultPack = startGameResultPack,
                    };
                    BroadcastRoomClient(roomCode, mainPack);
                    EventSystem.Instance.Publish(new ChangeRoomGameStateEvent
                    {
                        roomCode = roomCode,
                        newState = GameManager.GameState.CountdownToStart,
                    });
                    _mLogger.LogInfo($"Room {roomCode}:{roomInfo.roomName} all load complete");
                    
                    EventSystem.Instance.Publish(new RoomPlayerAllLoadCompleteEvent
                    {
                        roomCode = roomCode,
                        firstPlayerId = firstPlayerId
                    });
                    roomInfo.roomState = RoomState.Started;
                }
                catch (Exception e)
                {
                    _mLogger.LogError("UnknownException:" + e);
                }
            }
        }
    }

    private void CheckRoomAllReady()
    {
        foreach (var (_, roomInfo) in _mRoomDict)
        {
            if (roomInfo.roomState != RoomState.WaitReady) continue;
            string roomCode = roomInfo.roomCode;
            List<RoomPlayerInfo> roomPlayerInfoList = _mRoomCodePlayerDict[roomCode];
            bool allReady = roomPlayerInfoList.All(item => item.ready);
            if (allReady)
            {
                MainPack mainPack = new MainPack
                {
                    RequestCode = RequestCode.Room,
                    ActionCode = ActionCode.ReadyStartGame,
                    ReturnCode = ReturnCode.Success,
                    // CurrentGameState = GameState.WaitingStart
                };
                BroadcastRoomClient(roomCode, mainPack);
                EventSystem.Instance.Publish(new ChangeRoomGameStateEvent
                {
                    roomCode = roomCode,
                    newState = GameManager.GameState.WaitingStart,
                });
                roomInfo.roomState = RoomState.WaitingStart;
            }
        }
    }

    public override void OnDestroy()
    {
        EventSystem.Instance.Unsubscribe<RoomGameStateChangeEvent>(OnRoomGameStateChanged);
        EventSystem.Instance.Unsubscribe<ChangeRoomRoundEvent>(OnRoomChangeRound);
    }
}