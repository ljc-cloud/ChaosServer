namespace ChaosServer.Event;

public struct DestroyRoomEvent : IEvent
{
    public string roomCode;
}