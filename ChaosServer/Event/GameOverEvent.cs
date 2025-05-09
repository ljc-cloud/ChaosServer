using ChaosServer.Model;

namespace ChaosServer.Event;

public struct GameOverEvent : IEvent
{
    public string roomCode;
    public Dictionary<int, int[]> playerScoreArrayDict;
}