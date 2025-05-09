using ChaosServer.Model;
using ChaosServer.Net;
using Google.Protobuf.Collections;
using SocketProtocol;

namespace ChaosServer.Controller;

public class GameController : BaseController
{
    public GameController()
    {
        RequestCode = RequestCode.Game;
    }

    public void LoadGameSceneComplete(MainPack pack, TcpClient tcpClient = null)
    {
        int playerId = pack.PlayerInfoPack.Id;
        string roomCode = pack.RoomInfoPack.RoomCode;
        
        ServerInterface.Instance.RoomManager.SetRoomPlayerLoadComplete(roomCode, playerId);

        // pack.ReturnCode = ReturnCode.Success;
        
        // return pack;
    }

    public void FinishOperation(MainPack pack, TcpClient tcpClient = null)
    {
        // _mLogger.LogInfo("接收到完成操作请求");
        int playerId = pack.PlayerInfoPack.Id;
        string roomCode = pack.RoomInfoPack.RoomCode;

        RepeatedField<int> score = pack.FinishOperationPack.TotalScore;

        int[] scoreArray = score.ToArray();
        
        // PlayerScoreBoard? updatePlayerScoreBoard = ServerInterface.Instance.GameManager
        //     .UpdatePlayerScoreBoard(roomCode, playerId, scoreArray);
        PlayerScoreBoard updatePlayerScoreBoard = 
            ServerInterface.Instance.GameManager.FinishOperation(roomCode, playerId, scoreArray);

        PlayerInfoPack playerInfoPack = pack.PlayerInfoPack;
        PlayerScoreBoardPack playerScoreBoardPack = new PlayerScoreBoardPack();
        {
            playerScoreBoardPack.OperationLeft = updatePlayerScoreBoard.operationLeft;
            playerScoreBoardPack.ScoreArray.AddRange(score);
        }
        
        MainPack mainPack = new MainPack
        {
            RequestCode = pack.RequestCode,
            ActionCode = pack.ActionCode,
            PlayerInfoPack  = playerInfoPack,
            PlayerScoreBoardPack = playerScoreBoardPack,
            ReturnCode = ReturnCode.Success,
        };
        
        ServerInterface.Instance.RoomManager.BroadcastRoomClient(roomCode, mainPack);
        
        ServerInterface.Instance.GameManager.CheckOperation(roomCode);
        // return mainPack;
    }

    public void DeliverRecipe(MainPack pack, TcpClient tcpClient = null)
    {
        int deliverRecipeId = pack.DeliverRecipeId;
        string roomCode = pack.RoomInfoPack.RoomCode;
        List<int> updatedRecipeIdList = ServerInterface.Instance.GameManager.DeliverRecipe(roomCode, deliverRecipeId);

        MainPack mainPack = new MainPack
        {
            RequestCode = RequestCode.Game,
            ActionCode = ActionCode.UpdateRecipe,
            ReturnCode = ReturnCode.Success,
            RecipeIdArray = { updatedRecipeIdList }
        };

        ServerInterface.Instance.RoomManager.BroadcastRoomClient(roomCode, mainPack);
    }
}