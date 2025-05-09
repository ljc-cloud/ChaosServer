using ChaosServer.Model;

namespace ChaosServer.Event;

public struct QuitRoomEvent : IEvent
{
    public string roomCode;
    public int playerId;
}