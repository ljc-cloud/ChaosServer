using ChaosBall.Utility;
using ChaosServer.Model;
using ChaosServer.Net;
using SocketProtocol;

namespace ChaosServer.Controller;

public class RoomController : BaseController
{

    public RoomController()
    {
        RequestCode = RequestCode.Room;
    }
    
    public MainPack CreateRoom(MainPack pack, TcpClient tcpClient = null)
    {
        CreateRoomPack createRoomPack = pack.CreateRoomPack;

        RoomInfo roomInfo = new RoomInfo
        {
            roomName = createRoomPack.RoomName,
            roomVisibility = Enum.Parse<SocketProtocol.RoomVisibility>(createRoomPack.RoomVisibility.ToString()),
            maxPlayer = createRoomPack.MaxPlayer,
        };

        ReturnMessage returnMessage;

        PlayerInfo playerInfo = ServerInterface.Instance.PlayerManager.GetOnlinePlayer(tcpClient);
        
        var returnData = ServerInterface.Instance.RoomManager.CreateRoom(roomInfo, playerInfo, tcpClient);
        if (returnData.success)
        {
            pack.ReturnCode = ReturnCode.Success;
            RoomInfoPack roomInfoPack = new RoomInfoPack
            {
                MaxPlayer = createRoomPack.MaxPlayer,
                RoomName = createRoomPack.RoomName,
                RoomVisibility = createRoomPack.RoomVisibility,
                RoomCode = returnData.data,
            };
            pack.RoomInfoPack = roomInfoPack;
            returnMessage = new ReturnMessage
            {
                SuccessMessage = returnData.successMessage
            };
            _mLogger.LogInfo($"Player {playerInfo.nickname} has create room [{roomInfo}]");
        }
        else
        {
            pack.ReturnCode = ReturnCode.Fail;
            returnMessage = new ReturnMessage
            {
                ErrorMessage = returnData.errorMessage
            };
        }
        pack.ReturnMessage = returnMessage;

        return pack;
    }

    public MainPack SearchRoom(MainPack pack, TcpClient tcpClient = null)
    {
        RoomInfoPack roomInfoPack = pack.RoomInfoPack;

        RoomInfo roomInfo = new RoomInfo
        {
            roomName = roomInfoPack.RoomName,
            roomVisibility = Enum.Parse<SocketProtocol.RoomVisibility>(roomInfoPack.RoomVisibility.ToString()),
        };
        
        List<RoomInfo> roomInfoList = ServerInterface.Instance.RoomManager.SearchRoom(roomInfo);

        List<RoomInfoPack> roomInfoPackList = roomInfoList.Select(item =>
        {
            RoomInfoPack rip = new RoomInfoPack
            {
                RoomCode = item.roomCode,
                CurrentPlayers = item.currentPlayers,
                MaxPlayer = item.maxPlayer,
                RoomVisibility = Enum.Parse<SocketProtocol.RoomVisibility>(item.roomVisibility.ToString()),
                RoomName = item.roomName,
            };
            return rip;
        }).ToList();

        pack.ReturnCode = ReturnCode.Success;
        pack.ResponseCode = ResponseCode.RoomResponse;
        SearchRoomResultPack searchRoomResultPack = new SearchRoomResultPack();
        searchRoomResultPack.RoomInfoList.AddRange(roomInfoPackList);
        pack.SearchRoomResultPack = searchRoomResultPack;

        // ReturnMessage returnMessage = new ReturnMessage { SuccessMessage = "搜索成功!"};
        // pack.ReturnMessage = returnMessage;
        
        return pack;
    }

    public MainPack JoinRoom(MainPack pack, TcpClient tcpClient = null)
    {
        PlayerInfo playerInfo = ServerInterface.Instance.PlayerManager.GetOnlinePlayer(tcpClient);
        string roomCode = pack.RoomInfoPack.RoomCode;
        
        ReturnData<List<RoomPlayerInfo>> returnData = ServerInterface.Instance.RoomManager.JoinRoom(roomCode, playerInfo, tcpClient);
        ReturnMessage returnMessage;
        if (returnData.success)
        {
            returnMessage = new ReturnMessage
            {
                SuccessMessage = returnData.successMessage,
            };
            List<RoomPlayerInfo> roomPlayerInfoList = returnData.data;
            List<RoomPlayerInfoPack> roomPlayerInfoPackList =
                roomPlayerInfoList.Select(item => new RoomPlayerInfoPack
                {
                    Id = item.id,
                    Nickname = item.nickname,
                    Username = item.username,
                    Ready = item.ready
                }).ToList();
            JoinRoomResultPack joinRoomResultPack = new JoinRoomResultPack
            {
                RoomPlayerInfoList = { roomPlayerInfoPackList }
            };
            pack.JoinRoomResultPack = joinRoomResultPack;
            
            pack.ReturnCode = ReturnCode.Success;
            
            // 给同一房间的其他玩家广播该玩家的信息，进行同步
            RoomPlayerInfoPack roomPlayerInfoPack = new RoomPlayerInfoPack
            {
                Id = playerInfo.id,
                Nickname = playerInfo.nickname,
                Username = playerInfo.username,
                Ready = false
            };
            RoomInfo? roomInfo = ServerInterface.Instance.RoomManager.GetRoomInfoFromCode(roomCode);
            RoomInfoPack roomInfoPack = new RoomInfoPack { RoomCode = roomCode };
            if (roomInfo != null)
            {
                roomInfoPack.RoomName = roomInfo.roomName;
                roomInfoPack.RoomVisibility = Enum.Parse<SocketProtocol.RoomVisibility>(roomInfo.roomVisibility.ToString());
                roomInfoPack.MaxPlayer = roomInfo.maxPlayer;
                roomInfoPack.CurrentPlayers = roomInfo.currentPlayers;
                pack.RoomInfoPack = roomInfoPack;
            }
            ClientPack clientPack = new ClientPack { ClientId = tcpClient.ClientId };
            MainPack mainPack = new MainPack
            {
                RequestCode = RequestCode.Room,
                ActionCode = ActionCode.JoinRoom,
                ReturnCode = ReturnCode.Success,
                RoomPlayerInfoPack = roomPlayerInfoPack,
                RoomInfoPack = roomInfoPack,
                ClientPack = clientPack,
            };
            ServerInterface.Instance.RoomManager.BroadcastOtherRoomClient(roomCode, tcpClient, mainPack);
            
        }
        else
        {
            pack.ReturnCode = ReturnCode.Fail;
            returnMessage = new ReturnMessage
            {
                ErrorMessage = returnData.errorMessage,
            };
        }
        pack.ReturnMessage = returnMessage;
        return pack;
    }
    
    public MainPack PlayerReady(MainPack pack, TcpClient tcpClient = null)
    {
        PlayerInfo playerInfo = ServerInterface.Instance.PlayerManager.GetOnlinePlayer(tcpClient);

        string roomCode = pack.RoomInfoPack.RoomCode;

        bool ready = pack.RoomPlayerReadyPack.Ready;

        ReturnData<bool> returnData = ServerInterface.Instance.RoomManager.SetRoomPlayerReady(roomCode, playerInfo.id, ready);

        ReturnMessage returnMessage;
        if (returnData.success)
        {
            pack.ReturnCode = ReturnCode.Success;
            returnMessage = new ReturnMessage { SuccessMessage = returnData.successMessage };
            PlayerInfoPack playerInfoPack = new PlayerInfoPack{Id = playerInfo.id};
            pack.PlayerInfoPack = playerInfoPack;

            RoomPlayerReadyPack playerReadyPack = new RoomPlayerReadyPack { Ready = ready };
            ClientPack clientPack = new ClientPack { ClientId = tcpClient.ClientId };
            MainPack mainPack = new MainPack
            {
                RequestCode = RequestCode.Room,
                ActionCode = ActionCode.PlayerReady,
                ReturnCode = ReturnCode.Success,
                PlayerInfoPack = playerInfoPack,
                RoomPlayerReadyPack = playerReadyPack,
                ClientPack = clientPack,
            };
            ServerInterface.Instance.RoomManager.BroadcastOtherRoomClient(roomCode, tcpClient, mainPack);
        }
        else
        {
            pack.ReturnCode = ReturnCode.Fail;
            returnMessage = new ReturnMessage { ErrorMessage = returnData.errorMessage };
        }
        pack.ReturnMessage = returnMessage;
        
        return pack;
    }

    public MainPack QuitRoom(MainPack pack, TcpClient tcpClient = null)
    {
        int clientId = tcpClient.ClientId;
        // TODO: NPE
        PlayerInfo onlinePlayer = ServerInterface.Instance.PlayerManager.GetOnlinePlayer(tcpClient);
        
        if (onlinePlayer == null)
        {
            pack.ReturnCode = ReturnCode.Success;
            pack.ReturnMessage = new ReturnMessage
            {
                SuccessMessage = CharsetUtil.DefaultToUTF8("玩家已经退出房间")
            };
            return pack;
        }
        
        int playerId = onlinePlayer.id;
        PlayerInfoPack playerInfoPack = new PlayerInfoPack { Id = playerId };
        pack.PlayerInfoPack = playerInfoPack;
        string roomCode = pack.RoomInfoPack.RoomCode;

        var result = ServerInterface.Instance.RoomManager.RoomPlayerQuitRoom(roomCode, clientId, playerId);
        if (result)
        {
            pack.ReturnCode = ReturnCode.Success;

            // PlayerInfoPack playerInfoPack = new PlayerInfoPack { Id = playerId };
            ClientPack clientPack = new ClientPack { ClientId = tcpClient.ClientId };
            MainPack mainPack = new MainPack
            {
                RequestCode = RequestCode.Room,
                ActionCode = ActionCode.QuitRoom,
                ReturnCode = ReturnCode.Success,
                PlayerInfoPack = playerInfoPack,
                ClientPack = clientPack,
            };
            ServerInterface.Instance.RoomManager.BroadcastOtherRoomClient(roomCode, tcpClient, mainPack);
        }
        else
        {
            pack.ReturnCode = ReturnCode.Fail;
        }
        return pack;
    }
}