namespace ChaosServer.Event;

public struct PlayerQuitGameEvent : IEvent
{
    public int playerId;
    public int clientId;
}