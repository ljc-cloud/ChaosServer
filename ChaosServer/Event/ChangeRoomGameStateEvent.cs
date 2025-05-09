using ChaosServer.Game;

namespace ChaosServer.Event;

public struct ChangeRoomGameStateEvent : IEvent
{
    public string roomCode;
    public GameManager.GameState newState;
}