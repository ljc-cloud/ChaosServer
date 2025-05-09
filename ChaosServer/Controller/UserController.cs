using SocketProtocol;
using System.Runtime.Intrinsics;
using ChaosServer.Dao;
using ChaosServer.Model;
using ChaosServer.Net;

namespace ChaosServer.Controller;

public class UserController : BaseController {
        
    /// <summary>
    /// 用户查询服务
    /// </summary>
    private UserService _mUserService;
    public UserController()
    {
        RequestCode = RequestCode.User;
        _mUserService = new UserService();
    }

    public MainPack SignIn(MainPack pack, TcpClient tcpClient)
    {
        _mLogger.LogInfo("接收到登录请求");
        ReturnData<PlayerInfo> returnData = _mUserService.SignIn(pack.SignInPack);
        ReturnMessage returnMessage;
        if (returnData.success)
        {
            PlayerInfoPack playerInfoPack = new PlayerInfoPack
            {
                Id = returnData.data.id,
                Nickname = returnData.data.nickname,
                Username = returnData.data.username,
            };
            pack.PlayerInfoPack = playerInfoPack;
            returnMessage = new ReturnMessage
            {
                SuccessMessage = returnData.successMessage
            };
            pack.ReturnCode = ReturnCode.Success;
            pack.ResponseCode = ResponseCode.UserResponse;

            PlayerInfo playerInfo = new PlayerInfo
            {
                id = returnData.data.id,
                nickname = returnData.data.nickname,
                username = returnData.data.username,
            };

            int udpListenPort = pack.ClientPack.UdpListenPort;

            ServerInterface.Instance.PlayerManager.AddOnlinePlayer(tcpClient, playerInfo, udpListenPort);
        }
        else
        {
            returnMessage = new ReturnMessage
            {
                ErrorMessage = returnData.errorMessage
            };
            pack.ReturnCode = ReturnCode.Fail;
        }
        pack.ReturnMessage = returnMessage;

        return pack;
    }

    public MainPack SignUp(MainPack pack, TcpClient tcpClient)
    {
        ReturnData<PlayerInfo> returnData = _mUserService.SignUp(pack.SignUpPack);
        if (returnData.success)
        {
            pack.ReturnCode = ReturnCode.Success;
            PlayerInfoPack playerInfoPack = new PlayerInfoPack
            {
                Id = returnData.data.id,
                Nickname = returnData.data.nickname,
                Username = returnData.data.username
            };
            pack.PlayerInfoPack = playerInfoPack;
            ReturnMessage returnMessage = new ReturnMessage
            {
                SuccessMessage = returnData.successMessage
            };
            pack.ReturnMessage = returnMessage;
            pack.ReturnCode = ReturnCode.Success;
        }
        else
        {
            ReturnMessage returnMessage = new ReturnMessage
            {
                ErrorMessage = returnData.errorMessage
            };
            pack.ReturnMessage = returnMessage;
            pack.ReturnCode = ReturnCode.Fail;
        }

        return pack;
    }

}