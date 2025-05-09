namespace ChaosServer.Event;

public struct ChangeRoomRoundEvent : IEvent
{
    public string roomCode;
    public int currentRound;
    public int operationPlayerId;
    public string message;
}