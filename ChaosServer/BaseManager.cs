
namespace ChaosServer;

public abstract class BaseManager
{

    protected readonly Logger _mLogger;
    
    public BaseManager()
    {
        _mLogger = new Logger(GetType());
    }
    
    public abstract void OnInit();
    public abstract void OnUpdate();
    public abstract void OnDestroy();
}