using System.Timers;
using ChaosServer.Event;
using ChaosServer.Model;
using SocketProtocol;
using Timer = System.Timers.Timer;

namespace ChaosServer.Game;

public class GameManager : BaseManager
{
    public enum GameState
    {
        NotStarted,
        WaitingStart,
        CountdownToStart,
        GamePlaying,
        GameOver,
    }

    private readonly Dictionary<string, GameState> _mRoomGameState = new();
    private readonly Dictionary<string, int> _mRoomCountdown = new();
    private readonly Dictionary<string, float> _mRoomCountdownTimer = new();
    private readonly Dictionary<string, int> _mRoomCurrentOperationPlayerIdDict = new();
    private readonly Dictionary<string, int> _mRoomFirstPlayerIdDict = new();
    private readonly Dictionary<string, Dictionary<int, PlayerInfo>> _mRoomPlayerInfoDict = new();

    // private readonly Dictionary<string, float> _mRoomSpawnRecipeTimer = new();
    private readonly float _mSpawnRecipeTimerMax = 1.5f;
    private readonly Dictionary<string, float> _mRoomSpawnRecipeTimerDict = new();
    private readonly Dictionary<string, List<int>> _mRoomRecipeDict = new();

    #region ChaosBall

    private const int MaxRound = 4;
    
    private int _mCurrentRound = 1;
    private readonly Dictionary<string, int> _mRoomCurrentRoundDict = new();
    
    /// <summary>
    /// 房间码-playerId-玩家计分板
    /// </summary>
    private readonly Dictionary<string, Dictionary<int, PlayerScoreBoard>> _mRoomCodeToPlayerScoreBoard = new();
    

    // public void InitGame(string roomCode, int[] playerIdArray)
    // {
    //     Dictionary<int, PlayerScoreBoard> playerScoreBoardDict = new();
    //     foreach (var playerId in playerIdArray)
    //     {
    //         playerScoreBoardDict[playerId] = new PlayerScoreBoard
    //         {
    //             score = new int[MaxRound],
    //             operationLeft = MaxRound,
    //         };
    //     }
    //
    //     _mRoomCodeToPlayerScoreBoard[roomCode] = playerScoreBoardDict;
    // }
    
    public PlayerScoreBoard FinishOperation(string roomCode, int playerId, int[] score)
    {
        if (_mRoomCodeToPlayerScoreBoard.TryGetValue(roomCode, out var playerScoreBoardDict))
        {
            if (playerScoreBoardDict.TryGetValue(playerId, out var playerScoreBoard))
            {
                playerScoreBoard.operationLeft--;
                playerScoreBoard.score = score;
                return playerScoreBoard;
            }
        }
        return null;
    }

    public void CheckOperation(string roomCode)
    {
        if (_mRoomCodeToPlayerScoreBoard.TryGetValue(roomCode, out var playerScoreBoardDict))
        {
            int[] operationLeftArray = playerScoreBoardDict.Select(item 
                => item.Value.operationLeft).ToArray();

            bool gameOver = operationLeftArray.All(item => item == 0);
            if (gameOver)
            {
                EventSystem.Instance.Publish(new GameOverEvent
                {
                    roomCode = roomCode,
                    playerScoreArrayDict = playerScoreBoardDict
                        .ToDictionary(item 
                            => item.Key, item=>item.Value.score),
                });
                return;
            }

            // _mLogger.LogInfo($"operationLeft: player1 {operationLeftArray[0]}, player2 {operationLeftArray[1]}");
            bool changeRound = operationLeftArray.Length == 1 || operationLeftArray[0] == operationLeftArray[1];

            if (changeRound)
            {
                _mRoomCurrentRoundDict[roomCode]++;
                int operationPlayerId = -1;
                string message = string.Empty;
                if (_mRoomCodeToPlayerScoreBoard.TryGetValue(roomCode, out var playerScoreBordDict))
                {
                    int maxScore = -1000;
                    foreach (var (playerId, scoreBoard) in playerScoreBordDict)
                    {
                        if (scoreBoard.score is { Length: > 0 })
                        {
                            int score = scoreBoard.score.Aggregate((pre, next) => pre + next);
                            if (score > maxScore)
                            {
                                maxScore = score;
                                operationPlayerId = playerId;
                            }
                            else if (score == maxScore)
                            {
                                operationPlayerId = -1;
                            }
                        }
                    }

                    string nickname = string.Empty;
                    if (operationPlayerId == -1)
                    {
                        var firstPlayerId = _mRoomFirstPlayerIdDict[roomCode];
                        
                        if (ServerInterface.Instance.RoomManager.RoomCodeToPlayerDict.TryGetValue(roomCode,
                                out var playerInfoList))
                        {
                            nickname = playerInfoList.Find(item => 
                                item.id == firstPlayerId)?.nickname ?? string.Empty;
                        }
                        _mRoomCurrentOperationPlayerIdDict[roomCode] = firstPlayerId;
                        message = $"比分相同, 由第一回合先手玩家{nickname}先手";
                    }
                    else
                    {
                        if (ServerInterface.Instance.RoomManager.RoomCodeToPlayerDict.TryGetValue(roomCode,
                                out var playerInfoList))
                        {
                            nickname = playerInfoList.Find(item => 
                                item.id == operationPlayerId)?.nickname ?? string.Empty;
                        }
                        _mRoomCurrentOperationPlayerIdDict[roomCode] = operationPlayerId;
                        message = $"由上一回合得分高玩家{nickname}先手";
                    }
                }
                EventSystem.Instance.Publish(new ChangeRoomRoundEvent
                {
                    roomCode = roomCode,
                    currentRound = _mRoomCurrentRoundDict[roomCode],
                    operationPlayerId = _mRoomCurrentOperationPlayerIdDict[roomCode],
                    message = message,
                });
            }
            else
            {
                List<RoomPlayerInfo> roomPlayerInfoList = ServerInterface.Instance.RoomManager.RoomCodeToPlayerDict[roomCode];
                int currentOperationPlayerId = -1;
                foreach (var roomPlayerInfo in roomPlayerInfoList)
                {
                    if (roomPlayerInfo.id != _mRoomCurrentOperationPlayerIdDict[roomCode])
                    {
                        currentOperationPlayerId = roomPlayerInfo.id;
                        break;
                    }
                }
                _mRoomCurrentOperationPlayerIdDict[roomCode] = currentOperationPlayerId;
                EventSystem.Instance.Publish(new ChangeRoomOperationEvent
                {
                    roomCode = roomCode,
                    currentOperationPlayerId = currentOperationPlayerId,
                    operationLeft = playerScoreBoardDict[currentOperationPlayerId].operationLeft,
                });
            }
        }
    }

    public PlayerScoreBoard? UpdatePlayerScoreBoard(string roomCode, int playerId, int[] score)
    {
        if (_mRoomCodeToPlayerScoreBoard.TryGetValue(roomCode, out var playerScoreBoardDict))
        {
            if (playerScoreBoardDict.TryGetValue(playerId, out var playerScoreBoard))
            {
                playerScoreBoard.score = score;
                return playerScoreBoard;
            }
        }
        return null;
    }

    public int GetCurrentRound(string roomCode)
    {
        return _mRoomCurrentRoundDict.GetValueOrDefault(roomCode, -1);
    }

    public PlayerScoreBoard? GetPlayerScoreBoard(string roomCode, int playerId)
    {
        if (_mRoomCodeToPlayerScoreBoard.TryGetValue(roomCode, out var playerScoreBoardDict))
        {
            if (playerScoreBoardDict.TryGetValue(playerId, out var playerScoreBoard))
            {
                return playerScoreBoard;
            }
        }
        return null;
    }

    #endregion
    
    // public void SetRoomCurrentGameState(string roomCode, GameState gameState)
    // {
    //     _mRoomGameState[roomCode] = gameState;
    //     //: 发送游戏状态信息
    // }
    
    public override void OnInit()
    {
        EventSystem.Instance.Subscribe<CreateRoomEvent>(OnCreateRoom);
        EventSystem.Instance.Subscribe<JoinRoomEvent>(OnJoinRoom);
        EventSystem.Instance.Subscribe<QuitRoomEvent>(OnQuitRoom);
        EventSystem.Instance.Subscribe<DestroyRoomEvent>(OnDestroyRoom);
        EventSystem.Instance.Subscribe<ChangeRoomGameStateEvent>(OnChangeRoomGameState);
        EventSystem.Instance.Subscribe<RoomPlayerAllLoadCompleteEvent>(OnRoomPlayerAllLoadComplete);
    }

    private void OnRoomPlayerAllLoadComplete(RoomPlayerAllLoadCompleteEvent e)
    {
        _mRoomCurrentOperationPlayerIdDict[e.roomCode] = e.firstPlayerId;
        _mRoomFirstPlayerIdDict[e.roomCode] = e.firstPlayerId;
        _mRoomCurrentRoundDict[e.roomCode] = 1;
        _mRoomSpawnRecipeTimerDict[e.roomCode] = 0f;
        _mRoomRecipeDict[e.roomCode] = new List<int>();
    }

    private void OnChangeRoomGameState(ChangeRoomGameStateEvent e)
    {
        ChangeRoomGameState(e.roomCode, e.newState);
    }

    private void OnCreateRoom(CreateRoomEvent e)
    {
        string roomCode = e.roomInfo.roomCode;
        _mRoomGameState[roomCode] = GameState.NotStarted;
        _mRoomCountdown[roomCode] = 3;
        _mRoomCountdownTimer[roomCode] = 1f;

        _mRoomCodeToPlayerScoreBoard[roomCode] = new Dictionary<int, PlayerScoreBoard>
        {
            { e.creator.id, new PlayerScoreBoard
            {
                score = new int[MaxRound],
                operationLeft = MaxRound,
            }}
        };
    }
    private void OnJoinRoom(JoinRoomEvent e)
    {
        string roomCode = e.roomCode;
        int playerId = e.playerInfo.id;
        if (_mRoomCodeToPlayerScoreBoard.TryGetValue(roomCode, out var playerScoreBoardDict))
        {
            playerScoreBoardDict[playerId] = new PlayerScoreBoard
            {
                score = new int[MaxRound],
                operationLeft = MaxRound,
            };
        }
    }
    private void OnQuitRoom(QuitRoomEvent e)
    {
        string roomCode = e.roomCode;
        int playerId = e.playerId;
        if (_mRoomCodeToPlayerScoreBoard.TryGetValue(roomCode, out var playerScoreBoardDict))
        {
            playerScoreBoardDict.Remove(playerId);
        }
    }
    private void OnDestroyRoom(DestroyRoomEvent e)
    {
        string roomCode = e.roomCode;
        _mRoomGameState.Remove(roomCode);
        _mRoomCountdown.Remove(roomCode);
        _mRoomCountdownTimer.Remove(roomCode);
        _mRoomCodeToPlayerScoreBoard.Remove(roomCode);
    }
    
    public override void OnUpdate()
    {
        foreach (var item in _mRoomGameState)
        {
            switch (item.Value)
            {
                case GameState.NotStarted:
                    break;
                case GameState.WaitingStart:
                    break;
                case GameState.CountdownToStart:
                    // Countdown(item.Key);
                    break;
                case GameState.GamePlaying:
                    SpawnRecipe();
                    break;
                case GameState.GameOver:
                    break;
            }
        }
    }

    private void SpawnRecipe()
    {
        foreach (var item in _mRoomSpawnRecipeTimerDict)
        {
            float timer = item.Value;
            timer += ServerConstant.Update_INTERVAL_S;
            _mLogger.LogInfo("Time:" + timer);
            string roomCode = item.Key;
            _mRoomSpawnRecipeTimerDict[roomCode] = timer;
            if (timer < _mSpawnRecipeTimerMax) continue;
            timer = 0f;
            _mRoomSpawnRecipeTimerDict[roomCode] = timer;
            if (_mRoomRecipeDict.TryGetValue(roomCode, out var recipeIdList))
            {
                if (recipeIdList.Count >= ServerConstant.MAX_RECIPE_SPAWN_NUM) continue;
                Random rnd = new Random();
                int nextRecipeId = rnd.Next(1, ServerConstant.MAX_RECIPE_NUM + 1);
                recipeIdList.Add(nextRecipeId);

                MainPack mainPack = new MainPack
                {
                    RequestCode = RequestCode.Game,
                    ActionCode = ActionCode.UpdateRecipe,
                    ReturnCode = ReturnCode.Success,
                    RecipeIdArray = { recipeIdList }
                };
                
                ServerInterface.Instance.RoomManager.BroadcastRoomClient(roomCode, mainPack);
            }
        }
    }

    private void ChangeRoomGameState(string roomCode, GameState newState)
    {
        _mRoomGameState[roomCode] = newState;
        EventSystem.Instance.Publish(new RoomGameStateChangeEvent
        {
            roomCode = roomCode,
            gameState = newState
        });
        switch (newState)
        {
            case GameState.NotStarted:
                break;
            case GameState.CountdownToStart:
                Timer timer = new Timer
                {
                    Interval = 3000d,
                    // AutoReset = false,
                    // Enabled = true,
                };
                timer.Elapsed += (_, _) =>
                {
                    ChangeRoomGameState(roomCode, GameState.GamePlaying);
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
                break;
            case GameState.GamePlaying:
                break;
            case GameState.GameOver:
                _mRoomSpawnRecipeTimerDict.Remove(roomCode);
                _mRoomRecipeDict.Remove(roomCode);
                break;
        }
    }

    public override void OnDestroy()
    {
        EventSystem.Instance.Unsubscribe<CreateRoomEvent>(OnCreateRoom);
        EventSystem.Instance.Unsubscribe<JoinRoomEvent>(OnJoinRoom);
        EventSystem.Instance.Unsubscribe<QuitRoomEvent>(OnQuitRoom);
        EventSystem.Instance.Unsubscribe<DestroyRoomEvent>(OnDestroyRoom);
        EventSystem.Instance.Unsubscribe<ChangeRoomGameStateEvent>(OnChangeRoomGameState);
    }

    public List<int> DeliverRecipe(string roomCode, int deliverRecipeId)
    {
        if (_mRoomRecipeDict.TryGetValue(roomCode, out var recipeIdList))
        {
            // TODO: IndexOutOfRange
            recipeIdList.Remove(deliverRecipeId);
            return [..recipeIdList];
        }

        return null;
    }
}