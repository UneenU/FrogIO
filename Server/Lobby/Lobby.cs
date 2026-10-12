using Core;

namespace Server;

public class Lobby
{
    public string LobbyId { get; }
    public List<LobbyMember> Members { get; } = new();
    public string LobbyName { get; }
    public int MaxPlayers { get; }
    public bool IsFull => Members.Count == MaxPlayers;
    public bool EveryoneIsReady => Members.All(member => member.IsReady);
    
    private Lobby(string lobbyName, string lobbyId, int maxPlayers)
    {
        LobbyId = lobbyId;
        LobbyName = lobbyName;
        MaxPlayers = maxPlayers;
    }

    public static Lobby NewLobby(string name, int maxPlayers)
    {
        if (maxPlayers <= 0) throw new ArgumentException(nameof(maxPlayers));
        var lobbyId = Guid.NewGuid().ToString();
        return new Lobby(name, lobbyId, maxPlayers);
    }

}