namespace ChaosServer;

public interface ServerConstant
{
    public const string APP_PATH = "E:\\code\\ChaosServer\\ChaosServer";
    public const int CLIENT_UDP_LISTEN_PORT = 10020;
    public const int LOGIC_FRAME_RATE = 20;
    
    public const int MAX_TIMEOUT_COUNT = 5;
    public const int GAME_FRAME_FRATE = 60;
    public const int UPDATE_INTERVAL_MS = 1000 / GAME_FRAME_FRATE;
    public const float Update_INTERVAL_S = UPDATE_INTERVAL_MS / 1000f;

    public const float RECIPE_SPAWN_TIME = 1.5f;
    public const int MAX_RECIPE_NUM = 4;
    public const int MAX_RECIPE_SPAWN_NUM = 4;
}