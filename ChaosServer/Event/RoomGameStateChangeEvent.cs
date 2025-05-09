using ChaosServer.Game;

namespace ChaosServer.Event;

public struct RoomGameStateChangeEvent : IEvent
{
    public string roomCode;
    public GameManager.GameState gameState;
}