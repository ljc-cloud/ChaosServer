using SocketProtocol;

namespace ChaosServer.Controller {
    public abstract class BaseController
    {
        protected readonly Logger _mLogger;

        public BaseController()
        {
            _mLogger = new Logger(GetType());
        }
        
        public RequestCode RequestCode { get; protected set; }
    }
}
