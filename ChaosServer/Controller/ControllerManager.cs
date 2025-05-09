using SocketProtocol;
using System.Reflection;
using ChaosServer.Net;

namespace ChaosServer.Controller {
    public class ControllerManager {
        private readonly Dictionary<RequestCode, BaseController> _mControllerDict;

        private readonly Logger _mLogger;
        
        public ControllerManager()
        {
            _mLogger = new Logger(GetType());
            
            _mControllerDict = new Dictionary<RequestCode, BaseController>();
            UserController userController = new UserController();
            RoomController roomController = new RoomController();
            GameController gameController = new GameController();
            _mControllerDict.Add(RequestCode.User, userController);
            _mControllerDict.Add(RequestCode.Room, roomController);
            _mControllerDict.Add(RequestCode.Game, gameController);
        }

        public void HandleRequest(MainPack pack, TcpClient tcpClient) {
            if (_mControllerDict.TryGetValue(pack.RequestCode, out BaseController? controller)) {
                _mLogger.LogInfo($"接收到{pack.ActionCode}请求");
                string methodName = pack.ActionCode.ToString();
                MethodInfo? method = controller.GetType().GetMethod(methodName);
                if (method is null) {
                    _mLogger.LogWarning($"No such method named {methodName} in {controller.GetType().Name}");
                    return;
                }
                object[] parameters = [pack, tcpClient];
                object? ret = method.Invoke(controller, parameters);
                if (ret is not null) { 
                    MainPack mainPack = ret as MainPack;
                    tcpClient.Send(mainPack);
                }
            } else {
                _mLogger.LogWarning($"No such controller named {pack.RequestCode}Controller");
            }
        }
    }
}
