using ChaosServer.Model;

namespace ChaosServer.Event;

public struct JoinRoomEvent : IEvent
{
    public string roomCode;
    public PlayerInfo playerInfo;
}