namespace ChaosServer.Event;

public struct ChangeRoomOperationEvent : IEvent
{
    public string roomCode;
    public int currentOperationPlayerId;
    public int operationLeft;
}