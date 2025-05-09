using ChaosServer.Event;
using ChaosServer.Game;
using ChaosServer.Net;

namespace ChaosServer;

public class ServerInterface
{
    private readonly Logger _mLogger;
    
    private Thread _mUpdateThread;
    private bool _mApplicationQuit;

    private readonly CancellationTokenSource _mStopToken = new();
    
    private ServerInterface()
    {
        _mLogger = new Logger(GetType());
        
        _mLogger.LogInfo("Server Initializing......");
        
        TcpServer = new TcpServer(8080);
        RoomManager = new RoomManager();
        PlayerManager = new PlayerManager();
        UdpListener = new UdpListener(10040);
        RoomFrameSyncManager = new RoomFrameSyncManager(RoomManager, UdpListener);
        GameManager = new GameManager();
        RoomManager.OnInit();
        PlayerManager.OnInit();
        RoomFrameSyncManager.OnInit();
        GameManager.OnInit();

        _mUpdateThread = new Thread(Update);
        _mUpdateThread.Start();
        
        AppDomain.CurrentDomain.ProcessExit += ServerQuit;
        AppDomain.CurrentDomain.UnhandledException += HandleUnHandleException;
    }

    public static ServerInterface Instance { get; private set; }
    
    public TcpServer TcpServer { get; private set; }
    public UdpListener UdpListener { get; private set; }
    public RoomManager RoomManager { get; private set; }
    public GameManager GameManager { get; private set; }
    public RoomFrameSyncManager RoomFrameSyncManager { get; private set; }
    public PlayerManager PlayerManager { get; private set; }

    public static event Action OnServerQuit;
    
    public static void Start()
    {
        Instance = new();
    }

    private void Update()
    {
        while (!_mStopToken.IsCancellationRequested)
        {
            if (_mStopToken.IsCancellationRequested) break;
            Thread.Sleep(TimeSpan.FromMilliseconds(ServerConstant.UPDATE_INTERVAL_MS));
            RoomManager.OnUpdate();
            PlayerManager.OnUpdate();
            RoomFrameSyncManager.OnUpdate();
            GameManager.OnUpdate();
        }
    }

    private void ServerQuit(object sender, EventArgs e)
    {
        // _mApplicationQuit = true;
        _mStopToken.Cancel();
        _mLogger.LogInfo("Server Quit");
        RoomManager.OnDestroy();
        PlayerManager.OnDestroy();
        RoomFrameSyncManager.OnDestroy();
        GameManager.OnDestroy();
        TcpServer.Dispose();
        UdpListener.Dispose();
    }
    
    private void HandleUnHandleException(object sender, UnhandledExceptionEventArgs e)
    {
        _mLogger.LogCritical($"UnHandle exception: {e.ExceptionObject}");
    }
}