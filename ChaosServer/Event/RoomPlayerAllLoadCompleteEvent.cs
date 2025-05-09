namespace ChaosServer.Event;

public struct RoomPlayerAllLoadCompleteEvent : IEvent
{
    public string roomCode;
    public int firstPlayerId;
}