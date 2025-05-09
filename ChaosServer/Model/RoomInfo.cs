namespace ChaosServer.Model;

public enum RoomVisibility
{
    None,
    Public,
    Private,
}

public enum RoomState
{
    WaitReady,
    AllReady,
    WaitingStart,
    Started,
}
public class RoomInfo
{
    public string roomCode;
    public string roomName;
    public int maxPlayer;
    public int currentPlayers;
    public SocketProtocol.RoomVisibility roomVisibility;
    public RoomState roomState;

    public override string ToString()
    {
        return $"roomCode: {roomCode}, roomName: {roomName}, maxPlayer: {maxPlayer}, currentPlayer: {currentPlayers}" +
               $", roomVisibility: {roomVisibility}, roomState: {roomState}";
    }
}