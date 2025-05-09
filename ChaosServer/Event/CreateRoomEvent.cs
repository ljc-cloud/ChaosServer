using ChaosServer.Model;

namespace ChaosServer.Event;

public struct CreateRoomEvent : IEvent
{
    public RoomInfo roomInfo;
    public PlayerInfo creator;
}